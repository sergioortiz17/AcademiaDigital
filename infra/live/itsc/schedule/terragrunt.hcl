# Horario de servicio: lunes a viernes 7-22, sábado 7-13, domingo apagado (hora de Córdoba).
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/schedule"
}

dependency "app" {
  config_path = "../app"

  mock_outputs = {
    autoscaling_group_name = "mock-asg"
    autoscaling_group_arn  = "arn:aws:autoscaling:us-east-2:000000000000:autoScalingGroup:00000000-0000-0000-0000-000000000000:autoScalingGroupName/mock-asg"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "database" {
  config_path = "../database"

  mock_outputs = {
    identifier = "mock-db"
    arn        = "arn:aws:rds:us-east-2:000000000000:db:mock-db"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

inputs = {
  name                   = include.root.locals.name
  enabled                = local.scope.power_schedule_enabled
  autoscaling_group_name = dependency.app.outputs.autoscaling_group_name
  autoscaling_group_arn  = dependency.app.outputs.autoscaling_group_arn
  instance_count         = local.scope.instance_count
  db_instance_identifier = dependency.database.outputs.identifier
  db_instance_arn        = dependency.database.outputs.arn
}
