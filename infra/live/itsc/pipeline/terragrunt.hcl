# CI/CD de la institución: GitHub → CodePipeline → CodeBuild arm64 → ECR → Beanstalk.
# No depende de la unidad app: los nombres de la aplicación y el entorno de Beanstalk son
# el mismo prefijo (include.root.locals.name). La etapa Deploy se activa con
# deploy_enabled = true recién cuando el entorno exista (pasos de database, secrets y app).
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/pipeline"
}

dependency "github_connection" {
  config_path = "../../shared/github-connection"

  mock_outputs = {
    arn = "arn:aws:codeconnections:us-east-2:000000000000:connection/00000000-0000-0000-0000-000000000000"
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

dependency "registry" {
  config_path = "../../shared/registry"

  mock_outputs = {
    repository_arns = ["arn:aws:ecr:us-east-2:000000000000:repository/academiadigital/backend"]
    repository_urls = { backend = "000000000000.dkr.ecr.us-east-2.amazonaws.com/academiadigital/backend" }
  }
  mock_outputs_allowed_terraform_commands = ["init", "validate", "plan"]
}

inputs = {
  name                = include.root.locals.name
  connection_arn      = dependency.github_connection.outputs.arn
  repository          = local.scope.github_repository
  branch              = "main"
  ecr_registry        = split("/", values(dependency.registry.outputs.repository_urls)[0])[0]
  ecr_prefix          = include.root.locals.project
  ecr_repository_arns = dependency.registry.outputs.repository_arns

  # Etapa Deploy activa: el entorno de Beanstalk ya existe (unidad app, 15/11/2026).
  deploy_enabled = true
}
