# Connection strings y secreto JWT en SSM Parameter Store.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/secrets"
}

dependency "database" {
  config_path = "../database"

  mock_outputs = {
    address         = "mock.rds.amazonaws.com"
    port            = 5432
    db_name         = "academiadigital"
    master_username = "mock"
    master_password = "mock"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

inputs = {
  prefix          = include.root.locals.name
  db_address      = dependency.database.outputs.address
  db_port         = dependency.database.outputs.port
  db_username     = dependency.database.outputs.master_username
  db_password     = dependency.database.outputs.master_password
  backend_db_name = dependency.database.outputs.db_name
}
