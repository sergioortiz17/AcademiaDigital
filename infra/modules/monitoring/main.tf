# Monitoreo autogestionado: una EC2 arm64 con Grafana, Loki y Prometheus en Docker.
#
#   Beanstalk (Alloy) ── logs ────► Loki :3100 ──► S3 (chunks e índice, retención por compactor)
#                     └─ métricas ─► Prometheus :9090 (remote write, TSDB en el disco)
#   Grafana :3000 (solo localhost) ◄── Session Manager (port forwarding) ── operador
#
# - Sin puertos abiertos a internet: 3100 y 9090 solo aceptan al security group de la app;
#   Grafana escucha en 127.0.0.1 y se accede con Session Manager.
# - Sin claves: Loki usa S3 y Grafana lee CloudWatch con el rol de la instancia.
# - La configuración vive en S3 (prefijo config/) y la instancia la sincroniza en cada
#   arranque: cambiar un archivo = terraform apply + reiniciar el servicio (ver README).
# - Si la instancia se reemplaza, los logs siguen en S3; se pierden las métricas de
#   Prometheus y los cambios hechos a mano en Grafana (los dashboards se cargan de config/).

data "aws_region" "current" {}
data "aws_caller_identity" "current" {}

data "aws_subnet" "this" {
  id = var.subnet_id
}

data "aws_ssm_parameter" "al2023_arm64" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-arm64"
}

locals {
  region     = data.aws_region.current.region
  private_ip = cidrhost(data.aws_subnet.this.cidr_block, var.private_ip_hostnum)
  config_dir = "${path.module}/files"

  # Archivos de configuración que se suben a S3 (config/...). Los .tftpl se renderizan.
  rendered_files = {
    "docker-compose.yml" = templatefile("${local.config_dir}/docker-compose.yml.tftpl", {
      images                    = var.images
      prometheus_retention_days = var.prometheus_retention_days
    })
    "loki.yaml" = templatefile("${local.config_dir}/loki.yaml.tftpl", {
      region         = local.region
      bucket         = aws_s3_bucket.this.bucket
      retention_days = var.loki_retention_days
    })
    "grafana/provisioning/datasources/datasources.yaml" = templatefile("${local.config_dir}/grafana/provisioning/datasources/datasources.yaml.tftpl", {
      region = local.region
    })
  }

  static_files = {
    for f in fileset(local.config_dir, "**") : f => "${local.config_dir}/${f}"
    if !endswith(f, ".tftpl")
  }
}

# ── Bucket: datos de Loki y configuración ────────────────────────────────────

resource "aws_s3_bucket" "this" {
  bucket = "${var.name}-monitoring-${data.aws_caller_identity.current.account_id}"
}

resource "aws_s3_bucket_ownership_controls" "this" {
  bucket = aws_s3_bucket.this.id
  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_public_access_block" "this" {
  bucket                  = aws_s3_bucket.this.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "this" {
  bucket = aws_s3_bucket.this.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_policy" "this" {
  bucket = aws_s3_bucket.this.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "DenyInsecureTransport"
      Effect    = "Deny"
      Principal = "*"
      Action    = "s3:*"
      Resource  = [aws_s3_bucket.this.arn, "${aws_s3_bucket.this.arn}/*"]
      Condition = { Bool = { "aws:SecureTransport" = "false" } }
    }]
  })

  depends_on = [aws_s3_bucket_public_access_block.this]
}

# Solo limpia subidas multiparte abandonadas. La retención de los logs la hace el
# compactor de Loki (borrar objetos por fuera dejaría el índice apuntando a la nada).
resource "aws_s3_bucket_lifecycle_configuration" "this" {
  bucket = aws_s3_bucket.this.id
  rule {
    id     = "abort-incomplete-uploads"
    status = "Enabled"
    filter {}
    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }
}

resource "aws_s3_object" "rendered" {
  for_each = local.rendered_files

  bucket       = aws_s3_bucket.this.id
  key          = "config/${each.key}"
  content      = each.value
  etag         = md5(each.value)
  content_type = "text/plain"
}

resource "aws_s3_object" "static" {
  for_each = local.static_files

  bucket       = aws_s3_bucket.this.id
  key          = "config/${each.key}"
  source       = each.value
  etag         = filemd5(each.value)
  content_type = endswith(each.key, ".json") ? "application/json" : "text/plain"
}

# ── Contraseña del admin de Grafana (SSM; la instancia la lee al arrancar) ────

resource "random_password" "grafana_admin" {
  length  = 32
  special = false
}

resource "aws_ssm_parameter" "grafana_admin" {
  name        = "/${var.name}/monitoring/grafana-admin-password"
  description = "Contraseña del usuario admin de Grafana"
  type        = "SecureString"
  value       = random_password.grafana_admin.result
}

# ── Red ──────────────────────────────────────────────────────────────────────

resource "aws_security_group" "this" {
  name        = "${var.name}-monitoring"
  description = "Monitoreo: logs y metricas solo desde las instancias de la app; sin acceso desde internet"
  vpc_id      = var.vpc_id

  tags = { Name = "${var.name}-monitoring" }
}

resource "aws_vpc_security_group_ingress_rule" "loki" {
  security_group_id            = aws_security_group.this.id
  description                  = "Loki push desde las instancias de Beanstalk"
  referenced_security_group_id = var.app_security_group_id
  ip_protocol                  = "tcp"
  from_port                    = 3100
  to_port                      = 3100
}

resource "aws_vpc_security_group_ingress_rule" "prometheus" {
  security_group_id            = aws_security_group.this.id
  description                  = "Prometheus remote write desde las instancias de Beanstalk"
  referenced_security_group_id = var.app_security_group_id
  ip_protocol                  = "tcp"
  from_port                    = 9090
  to_port                      = 9090
}

resource "aws_vpc_security_group_egress_rule" "all" {
  security_group_id = aws_security_group.this.id
  description       = "Salida: imagenes, S3, SSM y CloudWatch"
  ip_protocol       = "-1"
  cidr_ipv4         = "0.0.0.0/0"
}

# ── Instancia ────────────────────────────────────────────────────────────────

resource "aws_instance" "this" {
  ami                         = data.aws_ssm_parameter.al2023_arm64.value
  instance_type               = var.instance_type
  subnet_id                   = var.subnet_id
  private_ip                  = local.private_ip
  associate_public_ip_address = true # solo para salir a internet; el SG no admite entrada desde afuera
  vpc_security_group_ids      = [aws_security_group.this.id]
  iam_instance_profile        = aws_iam_instance_profile.this.name

  user_data = templatefile("${path.module}/user_data.sh.tftpl", {
    region                 = local.region
    bucket                 = aws_s3_bucket.this.bucket
    grafana_password_param = aws_ssm_parameter.grafana_admin.name
    docker_compose_version = var.docker_compose_version
  })

  metadata_options {
    http_tokens                 = "required" # IMDSv2
    http_put_response_hop_limit = 2          # los contenedores (Loki, Grafana) usan el rol de la instancia
  }

  root_block_device {
    volume_type           = "gp3"
    volume_size           = var.root_volume_size_gb
    encrypted             = true
    delete_on_termination = true
  }

  tags = { Name = "${var.name}-monitoring" }

  lifecycle {
    # Una AMI nueva no debe reemplazar la instancia sola; se actualiza a propósito.
    ignore_changes = [ami]
  }

  depends_on = [aws_s3_object.rendered, aws_s3_object.static]
}
