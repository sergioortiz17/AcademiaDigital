# Certificado comodín *.dryvn.org para el ALB: cubre itsc.dryvn.org, academiadigital.dryvn.org
# (landing), admin.dryvn.org (panel) y el subdominio de cada institución nueva.
# dryvn.org está en Cloudflare: después del apply, cargar en Cloudflare el CNAME de
# `terragrunt output validation_records` (sin proxy) y esperar a que el certificado esté ISSUED.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
  # itsc.dryvn.org → dryvn.org
  base_domain = join(".", slice(split(".", local.scope.domain_name), 1, length(split(".", local.scope.domain_name))))
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/certificate-external-dns"
}

inputs = {
  domain_name = "*.${local.base_domain}"
}
