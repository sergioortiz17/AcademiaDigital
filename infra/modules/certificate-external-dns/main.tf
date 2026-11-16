# Certificado ACM validado por DNS en una zona que NO está en Route 53 (dryvn.org vive en
# Cloudflare). Terraform crea el certificado y muestra los registros de validación; hay que
# cargarlos a mano en Cloudflare. No hay aws_acm_certificate_validation: el apply no se queda
# esperando. El certificado pasa a ISSUED unos minutos después de publicar el registro.

resource "aws_acm_certificate" "this" {
  domain_name               = var.domain_name
  subject_alternative_names = var.subject_alternative_names
  validation_method         = "DNS"

  lifecycle {
    create_before_destroy = true
  }
}
