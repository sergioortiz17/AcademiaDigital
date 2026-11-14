variable "prefix" {
  description = "Prefijo de los repositorios (<prefix>/<servicio>)."
  type        = string
}

variable "repositories" {
  description = "Servicios con imagen propia."
  type        = list(string)
  default     = ["backend", "finance", "frontend"]
}

variable "keep_last_images" {
  description = "Imágenes que se conservan por repositorio (permiten volver a versiones anteriores)."
  type        = number
  default     = 15
}
