# Proveedor OIDC de GitHub Actions: uno por cuenta.
include "root" {
  path = find_in_parent_folders("root.hcl")
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/github-oidc-provider"
}
