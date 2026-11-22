# Bucket privado para los documentos que suben los alumnos en "Requisitos de ingreso"
# (analítico, DNI, partida, etc.). Va separado del bucket general para que Secretaría pueda
# descargarlos todos juntos sin mezclarlos con certificados ni recibos.
#
# Estructura: <DNI>_<Apellido>_<Nombre>/<REQUISITO>/<fecha>_<archivo>
# (otras instituciones, debajo de tenants/<slug>/).

data "aws_caller_identity" "current" {}

resource "aws_s3_bucket" "documents" {
  bucket = "${var.prefix}-student-docs-${data.aws_caller_identity.current.account_id}"
}

resource "aws_s3_bucket_ownership_controls" "documents" {
  bucket = aws_s3_bucket.documents.id

  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_public_access_block" "documents" {
  bucket = aws_s3_bucket.documents.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Versionado: si un alumno reemplaza un archivo, la versión anterior se conserva un tiempo.
resource "aws_s3_bucket_versioning" "documents" {
  bucket = aws_s3_bucket.documents.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "documents" {
  bucket = aws_s3_bucket.documents.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_policy" "documents" {
  bucket = aws_s3_bucket.documents.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "DenyInsecureTransport"
      Effect    = "Deny"
      Principal = "*"
      Action    = "s3:*"
      Resource  = [aws_s3_bucket.documents.arn, "${aws_s3_bucket.documents.arn}/*"]
      Condition = { Bool = { "aws:SecureTransport" = "false" } }
    }]
  })

  depends_on = [aws_s3_bucket_public_access_block.documents]
}

resource "aws_s3_bucket_lifecycle_configuration" "documents" {
  bucket = aws_s3_bucket.documents.id

  # Los documentos vigentes no vencen; solo se limpian versiones reemplazadas y subidas a medias.
  rule {
    id     = "expire-noncurrent-versions"
    status = "Enabled"

    filter {}

    noncurrent_version_expiration {
      noncurrent_days = var.noncurrent_version_days
    }

    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }
}

# Política de solo lectura para quien descarga los documentos (Secretaría / administración).
# Se asocia a mano al usuario o grupo de IAM que corresponda.
resource "aws_iam_policy" "read" {
  name        = "${var.prefix}-student-docs-read"
  description = "Listar y descargar los documentos de requisitos de ingreso de los alumnos."

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "ListDocuments"
        Effect   = "Allow"
        Action   = ["s3:ListBucket", "s3:ListBucketVersions"]
        Resource = aws_s3_bucket.documents.arn
      },
      {
        Sid      = "DownloadDocuments"
        Effect   = "Allow"
        Action   = ["s3:GetObject", "s3:GetObjectVersion"]
        Resource = "${aws_s3_bucket.documents.arn}/*"
      }
    ]
  })
}
