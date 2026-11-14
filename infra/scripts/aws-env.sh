# Entorno AWS aislado para operar la infraestructura de AcademiaDigital.
# Se activa SOLO en la terminal donde se carga:
#
#   source infra/scripts/aws-env.sh
#   aws login                 # abre el navegador: elegir la sesión de la cuenta de destino
#   ad-whoami                 # confirma la cuenta antes de cualquier plan
#   ad-tf-creds               # exporta las credenciales temporales para Terraform/Terragrunt
#
# Para salir: cerrar la pestaña, o  ad-off
# No lee ni modifica ~/.aws: los perfiles de clientes y Leapp quedan intactos.
# No guarda claves de acceso: `aws login` usa credenciales temporales de la sesión de la consola.

AD_DIR="$HOME/.aws-academiadigital"
mkdir -p "$AD_DIR" && chmod 700 "$AD_DIR"

# `aws login` necesita AWS CLI 2.32 o superior. Si el del sistema es más viejo, se usa uno
# instalado aparte (AD_CLI_BIN) sin tocar el del sistema.
AD_CLI_BIN="${AD_CLI_BIN:-$HOME/.aws-charla/cli/bin}"

if [ -z "${AD_ON:-}" ]; then
  export AD_OLD_PATH="$PATH"
  export AD_OLD_RPROMPT="${RPROMPT:-}"
fi

export AWS_CONFIG_FILE="$AD_DIR/config"
export AWS_SHARED_CREDENTIALS_FILE="$AD_DIR/credentials"
export AWS_LOGIN_CACHE_DIRECTORY="$AD_DIR/login-cache"
export AWS_PROFILE=academiadigital
export AWS_REGION=us-east-2
unset AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_SESSION_TOKEN AWS_DEFAULT_PROFILE
if [ -x "$AD_CLI_BIN/aws" ]; then
  case ":$PATH:" in *":$AD_CLI_BIN:"*) ;; *) export PATH="$AD_CLI_BIN:$PATH" ;; esac
fi
export AWS_PAGER=""
export AD_ON=1

if [ -n "${ZSH_VERSION:-}" ]; then
  RPROMPT="%F{cyan}[aws: academiadigital · us-east-2]%f"
elif [ -n "${BASH_VERSION:-}" ] && [ -n "${PS1:-}" ] && [ -z "${AD_OLD_PS1+x}" ]; then
  export AD_OLD_PS1="$PS1"; PS1="[aws: academiadigital] $PS1"
fi

ad-whoami() {
  aws sts get-caller-identity --query '{Cuenta:Account,Identidad:Arn}' --output table
}

# Terraform y Terragrunt leen las variables AWS_* del entorno. Las credenciales duran lo
# que la sesión de la consola; si vencen, repetir `aws login` y `ad-tf-creds`.
ad-tf-creds() {
  eval "$(aws configure export-credentials --profile academiadigital --format env)" &&
    echo "Credenciales temporales exportadas para Terraform/Terragrunt en esta terminal."
}

ad-off() {
  unset AWS_CONFIG_FILE AWS_SHARED_CREDENTIALS_FILE AWS_LOGIN_CACHE_DIRECTORY AWS_PROFILE AWS_REGION AWS_PAGER AD_ON
  unset AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_SESSION_TOKEN AWS_CREDENTIAL_EXPIRATION
  export PATH="$AD_OLD_PATH"
  if [ -n "${ZSH_VERSION:-}" ]; then RPROMPT="$AD_OLD_RPROMPT"; fi
  if [ -n "${AD_OLD_PS1+x}" ]; then PS1="$AD_OLD_PS1"; unset AD_OLD_PS1; fi
  unset AD_OLD_PATH AD_OLD_RPROMPT
  unset -f ad-whoami ad-tf-creds ad-off
  echo "Entorno academiadigital desactivado."
}

if ! aws login help >/dev/null 2>&1; then
  echo "Aviso: este AWS CLI no tiene 'aws login' (requiere 2.32+). Definir AD_CLI_BIN con un CLI más nuevo." >&2
fi
