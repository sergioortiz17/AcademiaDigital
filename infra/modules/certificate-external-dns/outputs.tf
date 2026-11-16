output "certificate_arn" {
  description = "ARN del certificado. Usarlo en el ALB recién cuando esté ISSUED."
  value       = aws_acm_certificate.this.arn
}

output "validation_records" {
  description = "Registros CNAME a cargar en el DNS externo (Cloudflare), sin proxy."
  value = distinct([
    for dvo in aws_acm_certificate.this.domain_validation_options : {
      name  = dvo.resource_record_name
      type  = dvo.resource_record_type
      value = dvo.resource_record_value
    }
  ])
}
