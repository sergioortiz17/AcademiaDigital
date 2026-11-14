#!/usr/bin/env bash
# Aborta si las credenciales activas no pertenecen a la cuenta esperada.
# Lo invoca root.hcl al leer la configuración de cada unidad, antes de que Terragrunt
# cree el bucket del estado o ejecute Terraform. Así un perfil de otra cuenta (por
# ejemplo, de un cliente) no puede crear ni modificar nada.
#
#   uso: assert-account.sh <account_id_esperado>

set -euo pipefail

expected="${1:?Falta el ID de la cuenta esperada}"

if ! actual="$(aws sts get-caller-identity --query Account --output text 2>/dev/null)"; then
  echo "ERROR: no hay credenciales de AWS válidas. Cargar infra/scripts/aws-env.sh y ejecutar 'aws login'." >&2
  exit 1
fi

if [ "$actual" != "$expected" ]; then
  echo "ERROR: la cuenta activa es $actual, pero esta infraestructura es de la cuenta $expected." >&2
  echo "       Revisar AWS_PROFILE / variables AWS_*: no se ejecutó nada." >&2
  exit 1
fi

# run_cmd devuelve la salida estándar: se imprime el ID para usarlo como valor.
printf '%s' "$actual"
