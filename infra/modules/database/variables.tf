variable "name" {
  description = "Identificador de la instancia RDS y prefijo de sus recursos."
  type        = string
}

variable "vpc_id" {
  type = string
}

variable "subnet_ids" {
  description = "Subnets privadas (al menos 2 zonas de disponibilidad)."
  type        = list(string)
}

variable "allowed_security_group_ids" {
  description = "Security groups que pueden conectarse al puerto 5432."
  type        = list(string)
}

variable "engine_version" {
  description = "Versión mayor de PostgreSQL. RDS elige la menor más reciente y la actualiza sola."
  type        = string
  default     = "16"
}

variable "instance_class" {
  type    = string
  default = "db.t4g.micro"
}

variable "allocated_storage_gb" {
  type    = number
  default = 20
}

variable "max_allocated_storage_gb" {
  description = "Tope del autoescalado de almacenamiento."
  type        = number
  default     = 50
}

variable "multi_az" {
  description = "Réplica en espera en otra zona con failover automático."
  type        = bool
  default     = true
}

variable "db_name" {
  description = "Base inicial (la del monolito)."
  type        = string
  default     = "academiadigital"
}

variable "master_username" {
  type    = string
  default = "academiadigital_admin"
}

variable "backup_retention_days" {
  type    = number
  default = 7
}

variable "backup_window" {
  description = "Ventana diaria de backup en UTC. Debe caer dentro del horario de encendido."
  type        = string
}

variable "maintenance_window" {
  description = "Ventana semanal de mantenimiento en UTC. Debe caer dentro del horario de encendido."
  type        = string
}

variable "deletion_protection" {
  type    = bool
  default = true
}
