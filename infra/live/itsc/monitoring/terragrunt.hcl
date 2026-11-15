# Monitoreo autogestionado: EC2 con Grafana, Loki (datos en S3) y Prometheus.
# Sin puertos abiertos a internet; Grafana se abre con Session Manager (ver DESPLIEGUE-ITSC.md).
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/monitoring"
}

dependency "network" {
  config_path = "../network"

  mock_outputs = {
    vpc_id            = "vpc-00000000000000000"
    public_subnet_ids = ["subnet-00000000000000000", "subnet-11111111111111111"]
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "app_security" {
  config_path = "../app-security"

  mock_outputs = {
    instance_security_group_id = "sg-00000000000000000"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

inputs = {
  name                  = include.root.locals.name
  vpc_id                = dependency.network.outputs.vpc_id
  subnet_id             = dependency.network.outputs.public_subnet_ids[0]
  app_security_group_id = dependency.app_security.outputs.instance_security_group_id
  schedule_enabled      = local.scope.power_schedule_enabled
}
