# Bucket privado de los documentos que suben los alumnos (requisitos de ingreso).
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

locals {
  scope = include.root.locals.scope
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/student-documents"
}

inputs = {
  prefix = include.root.locals.name
}
