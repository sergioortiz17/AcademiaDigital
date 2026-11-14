variable "domain_name" {
  description = "Dominio desde el que se envían los correos."
  type        = string
}

variable "zone_id" {
  description = "Hosted zone donde se cargan los registros DKIM, SPF y DMARC."
  type        = string
}

variable "mail_from_subdomain" {
  description = "Subdominio MAIL FROM (rebotes y SPF)."
  type        = string
  default     = "mail"
}

variable "dmarc_policy" {
  description = "Política DMARC. Empezar en none, revisar reportes y endurecer a quarantine."
  type        = string
  default     = "none"

  validation {
    condition     = contains(["none", "quarantine", "reject"], var.dmarc_policy)
    error_message = "Debe ser none, quarantine o reject."
  }
}
