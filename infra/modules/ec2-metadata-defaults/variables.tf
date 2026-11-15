variable "http_put_response_hop_limit" {
  description = "2 para que los contenedores Docker alcancen IMDSv2 (recomendado por AWS para entornos con contenedores)."
  type        = number
  default     = 2
}
