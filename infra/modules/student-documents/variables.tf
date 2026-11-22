variable "prefix" {
  type = string
}

variable "noncurrent_version_days" {
  description = "Días que se conserva una versión reemplazada o borrada de un documento."
  type        = number
  default     = 365
}
