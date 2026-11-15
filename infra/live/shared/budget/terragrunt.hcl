# Presupuesto mensual de la cuenta con avisos por correo.
include "root" {
  path   = find_in_parent_folders("root.hcl")
  expose = true
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/budget"
}

inputs = {
  name = include.root.locals.name
  # Con el horario de servicio del ITSC se estiman ~51 USD/mes.
  # Avisos: gasto real al 80 % (80 USD) y al 100 %, y pronóstico del mes por encima de 100 USD.
  limit_usd    = 100
  alert_emails = ["sergioortiz170992@gmail.com"]
}
