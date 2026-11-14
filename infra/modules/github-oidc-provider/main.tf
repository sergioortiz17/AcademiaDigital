# Proveedor OIDC de GitHub Actions. Solo puede haber uno por cuenta: lo comparten los
# roles de todas las instituciones. Evita guardar claves de acceso en GitHub.

resource "aws_iam_openid_connect_provider" "github" {
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
}
