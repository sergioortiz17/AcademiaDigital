variable "prefix" {
  description = "Prefijo de la ruta de los parámetros (/<prefix>/...)."
  type        = string
}

variable "db_address" {
  type = string
}

variable "db_port" {
  type = number
}

variable "db_username" {
  type = string
}

variable "db_password" {
  type      = string
  sensitive = true
}

variable "backend_db_name" {
  type = string
}

variable "finance_db_name" {
  description = "Base propia de Finance; el servicio la crea al arrancar si no existe."
  type        = string
  default     = "academiadigital_finance"
}
