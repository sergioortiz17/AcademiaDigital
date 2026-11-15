# Repositorios ECR de las imágenes. Las mismas imágenes sirven a todas las instituciones.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/registry"
}

inputs = {
  prefix = include.root.locals.project
}
