# Hosted zone del subdominio delegado desde Cloudflare.
# Es la primera unidad que se aplica: sus NS hay que cargarlos en Cloudflare antes
# de que el certificado (unidad certificate) pueda validarse.

resource "aws_route53_zone" "this" {
  name    = var.domain_name
  comment = "Delegada desde el DNS del dominio padre"
}
