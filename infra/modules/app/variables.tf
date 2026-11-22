variable "name" {
  description = "Nombre de la aplicación y del entorno de Beanstalk."
  type        = string
}

variable "vpc_id" {
  type = string
}

variable "instance_security_group_id" {
  description = "Security group de las instancias (unidad app-security)."
  type        = string
}

variable "instance_subnet_ids" {
  type = list(string)
}

variable "load_balancer_subnet_ids" {
  type = list(string)
}

variable "instance_type" {
  type    = string
  default = "t4g.small"
}

variable "instance_architecture" {
  description = "arm64 para t4g (Graviton). Las imágenes de ECR tienen que construirse para esta arquitectura."
  type        = string
  default     = "arm64"

  validation {
    condition     = contains(["arm64", "x86_64"], var.instance_architecture)
    error_message = "Debe ser arm64 o x86_64."
  }
}

variable "instance_count" {
  description = "Instancias en horario de encendido (2 = una por zona de disponibilidad)."
  type        = number
  default     = 2
}

variable "root_volume_size_gb" {
  type    = number
  default = 20
}

variable "certificate_arn" {
  description = "Certificado ACM validado para el listener HTTPS."
  type        = string
}

variable "health_check_path" {
  description = "Ruta del health check del ALB. Pasar a /health cuando la API lo exponga."
  type        = string
  default     = "/"
}

variable "managed_updates_start_time" {
  description = "Inicio de la ventana semanal de parches, en UTC con formato Day:HH:MM."
  type        = string
}

variable "log_retention_days" {
  type    = number
  default = 30
}

variable "environment_variables" {
  description = "Variables de entorno no sensibles."
  type        = map(string)
  default     = {}
}

variable "secret_parameter_arns" {
  description = "Variables de entorno que Beanstalk lee de SSM Parameter Store (nombre => ARN)."
  type        = map(string)
  default     = {}
}

variable "files_bucket_arn" {
  type = string
}

variable "student_documents_bucket_arn" {
  description = "Bucket de los documentos que suben los alumnos (requisitos de ingreso)."
  type        = string
}

variable "ses_identity_arn" {
  type = string
}

variable "dns_zone_id" {
  description = "Hosted zone donde se crea el registro A del dominio."
  type        = string
}

variable "domain_name" {
  type = string
}
