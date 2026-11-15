# Valores por defecto de metadatos (IMDS) para las instancias EC2 NUEVAS de la cuenta en
# esta región, incluidas las que crea Elastic Beanstalk.
#
# - IMDSv2 obligatorio (token): cierra el vector SSRF de IMDSv1.
# - Hop limit 2: los contenedores Docker están a un salto de red de la instancia; con el
#   valor 1 no reciben el token de IMDSv2 y el SDK de AWS no obtiene las credenciales del
#   rol (S3 de archivos, SES, SSM). Beanstalk no expone esta opción, por eso se fija acá.
#
# Solo afecta a instancias lanzadas DESPUÉS del apply; las existentes no cambian.
# Referencia: https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/configuring-IMDS-new-instances.html

resource "aws_ec2_instance_metadata_defaults" "this" {
  http_endpoint               = "enabled"
  http_tokens                 = "required"
  http_put_response_hop_limit = var.http_put_response_hop_limit
}
