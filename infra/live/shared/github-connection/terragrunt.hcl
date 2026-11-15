# Conexión de la cuenta con GitHub. Una sola por cuenta: la comparten los pipelines de
# todas las instituciones. Después del apply hay que completarla en la consola (ver módulo).
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/github-connection"
}

inputs = {
  name = "${include.root.locals.name}-github"
}
