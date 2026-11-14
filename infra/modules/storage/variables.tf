variable "prefix" {
  type = string
}

variable "noncurrent_version_days" {
  description = "Días que se conserva una versión reemplazada o borrada de un archivo."
  type        = number
  default     = 30
}
