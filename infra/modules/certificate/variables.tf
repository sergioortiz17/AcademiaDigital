variable "domain_name" {
  type = string
}

variable "zone_id" {
  description = "Hosted zone donde se cargan los registros de validación."
  type        = string
}
