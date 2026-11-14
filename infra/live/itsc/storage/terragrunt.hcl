# Bucket privado de archivos de la app.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/storage"
}

inputs = {
  prefix = include.root.locals.name
}
