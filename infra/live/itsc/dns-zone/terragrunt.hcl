# Hosted zone del subdominio. Primera unidad a aplicar: sus NS van a Cloudflare.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/dns-zone"
}

inputs = {
  domain_name = local.scope.domain_name
}
