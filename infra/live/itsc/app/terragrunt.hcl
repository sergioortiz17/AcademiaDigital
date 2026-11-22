# Elastic Beanstalk: aplicación, entorno con ALB y HTTPS, y registro A del dominio.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/app"
}

dependency "network" {
  config_path = "../network"

  mock_outputs = {
    vpc_id            = "vpc-00000000000000000"
    public_subnet_ids = ["subnet-00000000000000000", "subnet-11111111111111111"]
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "monitoring" {
  config_path = "../monitoring"

  mock_outputs = {
    loki_push_url               = "http://10.20.0.10:3100/loki/api/v1/push"
    prometheus_remote_write_url = "http://10.20.0.10:9090/api/v1/write"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "app_security" {
  config_path = "../app-security"

  mock_outputs = {
    instance_security_group_id = "sg-00000000000000000"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "dns_zone" {
  config_path = "../dns-zone"

  mock_outputs = {
    zone_id     = "Z0000000000000000000"
    domain_name = "itsc.example.org"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

# Certificado comodín *.dryvn.org (cubre itsc, la landing, el panel y cada institución).
# Aplicar esta unidad recién cuando el certificado esté ISSUED (ver wildcard-certificate).
dependency "certificate" {
  config_path = "../wildcard-certificate"

  mock_outputs = {
    certificate_arn = "arn:aws:acm:us-east-2:000000000000:certificate/00000000-0000-0000-0000-000000000000"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "email" {
  config_path = "../email"

  mock_outputs = {
    identity_arn = "arn:aws:ses:us-east-2:000000000000:identity/itsc.example.org"
    domain_name  = "itsc.example.org"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "storage" {
  config_path = "../storage"

  mock_outputs = {
    bucket_name = "mock-files"
    bucket_arn  = "arn:aws:s3:::mock-files"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "student_documents" {
  config_path = "../student-documents"

  mock_outputs = {
    bucket_name = "mock-student-docs"
    bucket_arn  = "arn:aws:s3:::mock-student-docs"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "secrets" {
  config_path = "../secrets"

  mock_outputs = {
    parameter_arns = {
      BACKEND_DB_CONNECTION = "arn:aws:ssm:us-east-2:000000000000:parameter/mock/backend"
      FINANCE_DB_CONNECTION = "arn:aws:ssm:us-east-2:000000000000:parameter/mock/finance"
      JWT_SECRET_KEY        = "arn:aws:ssm:us-east-2:000000000000:parameter/mock/jwt"
    }
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

inputs = {
  name                         = include.root.locals.name
  vpc_id                       = dependency.network.outputs.vpc_id
  instance_subnet_ids          = dependency.network.outputs.public_subnet_ids
  load_balancer_subnet_ids     = dependency.network.outputs.public_subnet_ids
  instance_security_group_id   = dependency.app_security.outputs.instance_security_group_id
  instance_type                = local.scope.instance_type
  instance_architecture        = local.scope.instance_architecture
  instance_count               = local.scope.instance_count
  certificate_arn              = dependency.certificate.outputs.certificate_arn
  dns_zone_id                  = dependency.dns_zone.outputs.zone_id
  domain_name                  = dependency.dns_zone.outputs.domain_name
  files_bucket_arn             = dependency.storage.outputs.bucket_arn
  student_documents_bucket_arn = dependency.student_documents.outputs.bucket_arn
  ses_identity_arn             = dependency.email.outputs.identity_arn
  secret_parameter_arns        = dependency.secrets.outputs.parameter_arns

  # UTC: sábado 10:30 en Córdoba, dentro del horario de encendido.
  managed_updates_start_time = "Sat:13:30"

  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ALLOW_TIME_TRAVEL      = "false"
    APP_PUBLIC_URL         = "https://${local.scope.domain_name}"
    APP_AWS_REGION         = include.root.locals.aws_region
    FILES_BUCKET           = dependency.storage.outputs.bucket_name
    MAIL_FROM              = "no-reply@${local.scope.domain_name}"

    # Observabilidad (agente Alloy del compose y backend).
    LOKI_PUSH_URL                       = dependency.monitoring.outputs.loki_push_url
    PROMETHEUS_REMOTE_WRITE_URL         = dependency.monitoring.outputs.prometheus_remote_write_url
    OBS_ENV                             = "${local.scope.client}-${local.scope.environment}"
    ReverseProxy__TrustForwardedHeaders = "true"  # ALB → nginx → backend; el backend no publica puertos
    Observability__MinimumLevel         = "Error" # solo errores + auditoría de accesos (logins, logouts)
    Swagger__Enabled                    = "false" # no publicar Swagger/Scalar en producción

    # Archivos (acuerdos, certificados, recibos) en S3 y no en el disco de cada instancia.
    # El prefijo (backend/ o finance/) lo define cada servicio en deploy/eb/docker-compose.yml.
    Storage__Provider   = "S3"
    Storage__S3__Bucket = dependency.storage.outputs.bucket_name
    Storage__S3__Region = include.root.locals.aws_region

    # Requisitos de ingreso: bucket propio, una carpeta por alumno (DNI_Apellido_Nombre/REQUISITO/).
    Storage__StudentDocuments__Bucket = dependency.student_documents.outputs.bucket_name

    # Correo saliente (recuperación de contraseña) por Amazon SES con el rol de la instancia.
    # Remitente: MAIL_FROM. Con la cuenta SES en sandbox solo llega a direcciones verificadas.
    Email__Provider = "Ses"

    # Registro público: los emails de este dominio se registran como Profesor, pendientes de
    # alta en "Aspirantes". Se aplica al arrancar solo si la institución todavía no tiene uno.
    Tenancy__DefaultTeacherEmailDomain = "itscordoba.edu.ar"

    # Aviso por correo de cada pedido del formulario "Pide tu plataforma" (landing).
    # Con SES en sandbox tiene que ser una dirección verificada.
    Platform__NotifyEmail = "sergioortiz170992@gmail.com"
  }
}
