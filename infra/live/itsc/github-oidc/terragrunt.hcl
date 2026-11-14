# Rol de GitHub Actions de la institución: CI (ECR, Beanstalk) y encendido a demanda.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/github-oidc"
}

dependency "oidc_provider" {
  config_path = "../../shared/github-oidc-provider"

  mock_outputs = {
    arn = "arn:aws:iam::000000000000:oidc-provider/token.actions.githubusercontent.com"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "registry" {
  config_path = "../../shared/registry"

  mock_outputs = {
    repository_arns = ["arn:aws:ecr:us-east-2:000000000000:repository/mock"]
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "app" {
  config_path = "../app"

  mock_outputs = {
    autoscaling_group_arn = "arn:aws:autoscaling:us-east-2:000000000000:autoScalingGroup:00000000-0000-0000-0000-000000000000:autoScalingGroupName/mock-asg"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "database" {
  config_path = "../database"

  mock_outputs = {
    arn = "arn:aws:rds:us-east-2:000000000000:db:mock-db"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "schedule" {
  config_path = "../schedule"

  mock_outputs = {
    schedule_group_name = "mock"
    scheduler_role_arn  = "arn:aws:iam::000000000000:role/mock-scheduler"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

inputs = {
  name                  = include.root.locals.name
  repository            = local.scope.github_repository
  oidc_provider_arn     = dependency.oidc_provider.outputs.arn
  ecr_repository_arns   = dependency.registry.outputs.repository_arns
  autoscaling_group_arn = dependency.app.outputs.autoscaling_group_arn
  db_instance_arn       = dependency.database.outputs.arn
  schedule_group_name   = dependency.schedule.outputs.schedule_group_name
  scheduler_role_arn    = dependency.schedule.outputs.scheduler_role_arn
}
