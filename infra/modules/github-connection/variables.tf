variable "name" {
  description = "Nombre de la conexión (máximo 32 caracteres)."
  type        = string

  validation {
    condition     = length(var.name) <= 32
    error_message = "CodeConnections admite nombres de hasta 32 caracteres."
  }
}
