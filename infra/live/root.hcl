# Configuración común a todas las unidades: backend S3, provider y versiones.
# Cada unidad lo incluye con:  include "root" { path = find_in_parent_folders("root.hcl") }
#
# Jerarquía:
#   live/account.hcl          cuenta y región de AWS
#   live/<alcance>/scope.hcl  "shared" (recursos de toda la cuenta) o una institución
#   live/<alcance>/<unidad>/  un estado de Terraform por unidad

locals {
  account = read_terragrunt_config(find_in_parent_folders("account.hcl")).locals
  scope   = read_terragrunt_config(find_in_parent_folders("scope.hcl")).locals

  project    = "academiadigital"
  aws_region = local.account.aws_region

  # Control de cuenta: se ejecuta al leer la configuración, antes de crear el bucket del
  # estado o correr Terraform. Si las credenciales activas son de otra cuenta (p. ej. de
  # un cliente), Terragrunt aborta sin tocar nada. El bucket y allowed_account_ids usan
  # este valor verificado, así que no hay forma de saltarse el control.
  account_id = run_cmd("--terragrunt-quiet", "${get_parent_terragrunt_dir()}/../scripts/assert-account.sh", local.account.aws_account_id)

  # Prefijo de nombres de recursos: "academiadigital" en shared,
  # "academiadigital-<institución>-<ambiente>" en cada institución.
  name = local.scope.scope == "shared" ? local.project : "${local.project}-${local.scope.client}-${local.scope.environment}"
}

terraform_version_constraint  = ">= 1.10.0"
terragrunt_version_constraint = ">= 1.0.0"

# El bucket se crea la primera vez con `--backend-bootstrap` (o `terragrunt backend bootstrap`):
# Terragrunt le aplica versionado, cifrado, TLS obligatorio y bloqueo de acceso público.
remote_state {
  backend = "s3"

  generate = {
    path      = "backend.tf"
    if_exists = "overwrite_terragrunt"
  }

  config = {
    bucket       = "${local.project}-tfstate-${local.account_id}"
    key          = "${path_relative_to_include()}/terraform.tfstate"
    region       = local.aws_region
    encrypt      = true
    use_lockfile = true
  }
}

generate "provider" {
  path      = "provider.tf"
  if_exists = "overwrite_terragrunt"
  contents  = <<-EOF
    terraform {
      required_version = ">= 1.10.0"

      required_providers {
        aws = {
          source  = "hashicorp/aws"
          version = "~> 6.0"
        }
        random = {
          source  = "hashicorp/random"
          version = "~> 3.6"
        }
      }
    }

    provider "aws" {
      region = "${local.aws_region}"

      # Corta cualquier plan o apply si el perfil activo apunta a otra cuenta.
      allowed_account_ids = ["${local.account_id}"]

      default_tags {
        tags = {
          Project     = "${local.project}"
          Scope       = "${local.scope.scope}"
          Client      = "${try(local.scope.client, "shared")}"
          Environment = "${try(local.scope.environment, "shared")}"
          ManagedBy   = "terragrunt"
          Unit        = "${path_relative_to_include()}"
        }
      }
    }
  EOF
}
