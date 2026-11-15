# IMDSv2 obligatorio y hop limit 2 para toda instancia EC2 nueva de la cuenta en la región.
# Necesario antes de crear Beanstalk: sin hop limit 2 los contenedores no obtienen las
# credenciales del rol (S3, SES, SSM). Es un ajuste de cuenta, por eso vive en shared/.
include "root" {
  path = find_in_parent_folders("root.hcl")
}

terraform {
  source = "${get_parent_terragrunt_dir("root")}/../modules/ec2-metadata-defaults"
}
