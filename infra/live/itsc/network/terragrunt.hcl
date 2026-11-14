# VPC propia: subnets públicas (ALB e instancias) y privadas (RDS), sin NAT.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/network"
}

inputs = {
  name = include.root.locals.name
}
