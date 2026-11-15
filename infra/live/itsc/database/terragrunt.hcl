# RDS PostgreSQL en subnets privadas, accesible solo desde las instancias.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/database"
}

dependency "network" {
  config_path = "../network"

  mock_outputs = {
    vpc_id             = "vpc-00000000000000000"
    private_subnet_ids = ["subnet-00000000000000000", "subnet-11111111111111111"]
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
  name                       = include.root.locals.name
  vpc_id                     = dependency.network.outputs.vpc_id
  subnet_ids                 = dependency.network.outputs.private_subnet_ids
  allowed_security_group_ids = [dependency.app_security.outputs.instance_security_group_id]
  instance_class             = local.scope.db_instance_class
  multi_az                   = local.scope.db_multi_az

  # UTC, dentro del horario de encendido (lunes a sábado desde las 10:00 UTC = 07:00 en Córdoba).
  backup_window      = "11:00-11:30"
  maintenance_window = "sat:13:00-sat:13:30"
}
