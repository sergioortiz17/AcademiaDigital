# Certificado ACM para CloudFront (frontend). Es el único recurso del proyecto en us-east-1
# (ver region.hcl). Los registros de validación van a la misma zona de Route 53.
#
# Además sirve de prueba: si la SCP de la organización niega ACM en us-east-1, el apply
# falla con AccessDenied sin crear nada y se descarta CloudFront en favor de Cloudflare Pages.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/certificate"
}

dependency "dns_zone" {
  config_path = "../dns-zone"

  mock_outputs = {
    zone_id     = "Z0000000000000000000"
    domain_name = "itsc.example.org"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

inputs = {
  domain_name = dependency.dns_zone.outputs.domain_name
  zone_id     = dependency.dns_zone.outputs.zone_id
}
