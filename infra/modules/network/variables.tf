variable "name" {
  description = "Prefijo de nombres de la red."
  type        = string
}

variable "cidr_block" {
  description = "Rango de la VPC. Cada subnet toma un /24 dentro de este rango."
  type        = string
  default     = "10.20.0.0/16"
}

variable "az_count" {
  description = "Cantidad de zonas de disponibilidad. RDS Multi-AZ y el ALB necesitan al menos 2."
  type        = number
  default     = 2

  validation {
    condition     = var.az_count >= 2
    error_message = "Se necesitan al menos 2 zonas de disponibilidad."
  }
}
