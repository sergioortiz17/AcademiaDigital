variable "domain_name" {
  description = "Dominio principal del certificado (ej. *.dryvn.org)."
  type        = string
}

variable "subject_alternative_names" {
  description = "Nombres adicionales del certificado."
  type        = list(string)
  default     = []
}
