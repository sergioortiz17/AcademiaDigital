# Identidad SES y registros DKIM, MAIL FROM, SPF y DMARC.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/email"
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
