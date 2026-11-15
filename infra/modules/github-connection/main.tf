# Conexión de AWS con GitHub (CodeConnections). La usa CodePipeline para leer el repositorio
# sin guardar credenciales en GitHub ni en AWS: la autorización la da la app
# "AWS Connector for GitHub" instalada en la cuenta de GitHub.
#
# Terraform la crea en estado PENDING. Hay que completarla UNA vez desde la consola
# (Developer Tools → Settings → Connections → Update pending connection); ese paso abre
# GitHub para instalar/autorizar la app y no se puede automatizar.

resource "aws_codeconnections_connection" "github" {
  name          = var.name
  provider_type = "GitHub"
}
