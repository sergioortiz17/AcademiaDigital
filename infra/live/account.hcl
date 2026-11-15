# Cuenta de AWS de destino. Completar el ID antes del primer plan: el provider usa
# allowed_account_ids, así que con un perfil de otra cuenta Terraform se niega a operar.
locals {
  aws_account_id = "740227232235"
  aws_region     = "us-east-2"
}
