# Secretos de la aplicación en SSM Parameter Store (SecureString).
# Beanstalk los inyecta como variables de entorno al arrancar cada instancia; ningún
# valor queda en el repositorio. Los valores sí quedan en el estado de Terraform, que
# vive en un bucket privado, cifrado y solo accesible por TLS (ver infra/bootstrap).

resource "random_password" "jwt" {
  length  = 64
  special = false
}

locals {
  # RDS PostgreSQL 15+ exige SSL por defecto (rds.force_ssl).
  ssl = "SSL Mode=Require;Trust Server Certificate=true"

  connection_strings = {
    backend = "Host=${var.db_address};Port=${var.db_port};Database=${var.backend_db_name};Username=${var.db_username};Password=${var.db_password};${local.ssl}"
    finance = "Host=${var.db_address};Port=${var.db_port};Database=${var.finance_db_name};Username=${var.db_username};Password=${var.db_password};${local.ssl}"
  }
}

resource "aws_ssm_parameter" "backend_db" {
  name        = "/${var.prefix}/backend/db-connection"
  description = "Connection string del monolito"
  type        = "SecureString"
  value       = local.connection_strings.backend
}

resource "aws_ssm_parameter" "finance_db" {
  name        = "/${var.prefix}/finance/db-connection"
  description = "Connection string del microservicio Finance"
  type        = "SecureString"
  value       = local.connection_strings.finance
}

resource "aws_ssm_parameter" "jwt" {
  name        = "/${var.prefix}/backend/jwt-secret"
  description = "Clave de firma de los JWT"
  type        = "SecureString"
  value       = random_password.jwt.result
}
