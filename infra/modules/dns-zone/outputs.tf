output "zone_id" {
  value = aws_route53_zone.this.zone_id
}

output "domain_name" {
  value = aws_route53_zone.this.name
}

output "name_servers" {
  description = "Los 4 NS que hay que cargar en Cloudflare para delegar el subdominio."
  value       = aws_route53_zone.this.name_servers
}
