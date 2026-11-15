variable "name" {
  type = string
}

variable "limit_usd" {
  description = "Límite mensual en USD."
  type        = number
}

variable "actual_threshold_percent" {
  description = "Porcentaje del límite que dispara el aviso por gasto real."
  type        = number
  default     = 80
}

variable "alert_emails" {
  description = "Destinatarios de los avisos."
  type        = list(string)

  validation {
    condition     = length(var.alert_emails) > 0
    error_message = "Se necesita al menos un correo para los avisos de presupuesto."
  }
}
