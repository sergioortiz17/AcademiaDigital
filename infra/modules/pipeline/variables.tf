variable "name" {
  description = "Prefijo de los recursos y nombre de la aplicación y el entorno de Beanstalk."
  type        = string
}

variable "connection_arn" {
  description = "Conexión con GitHub (unidad shared/github-connection), ya en estado AVAILABLE."
  type        = string
}

variable "repository" {
  description = "Repositorio de GitHub en formato owner/repo."
  type        = string
}

variable "branch" {
  description = "Rama que dispara el pipeline."
  type        = string
  default     = "main"
}

variable "ecr_registry" {
  description = "Host del registro ECR (<cuenta>.dkr.ecr.<región>.amazonaws.com)."
  type        = string
}

variable "ecr_prefix" {
  description = "Prefijo de los repositorios ECR (academiadigital → academiadigital/backend, ...)."
  type        = string
}

variable "ecr_repository_arns" {
  description = "Repositorios ECR donde CodeBuild puede subir imágenes."
  type        = list(string)
}

variable "build_compute_type" {
  description = "Tamaño de la máquina de build. BUILD_GENERAL1_SMALL con ARM = arm1.small (entra en el Free Tier de CodeBuild)."
  type        = string
  default     = "BUILD_GENERAL1_SMALL"
}

variable "build_image" {
  description = "Imagen de CodeBuild para ARM (Amazon Linux 2023, incluye Docker)."
  type        = string
  default     = "aws/codebuild/amazonlinux-aarch64-standard:3.0"
}

variable "build_timeout_minutes" {
  type    = number
  default = 45
}

variable "deploy_enabled" {
  description = "Agrega la etapa de despliegue en Beanstalk. Activar recién cuando exista el entorno (unidad app)."
  type        = bool
  default     = false
}

variable "log_retention_days" {
  type    = number
  default = 30
}
