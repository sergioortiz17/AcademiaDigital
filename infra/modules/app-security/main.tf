# Security group de las instancias de la app, en su propia unidad para cortar el ciclo
# database -> app -> secrets -> database: RDS autoriza este SG y la app lo usa.
# Beanstalk agrega por su cuenta la regla de ingreso desde el ALB a sus instancias.

resource "aws_security_group" "instances" {
  name        = "${var.name}-instances"
  description = "Instancias de la app; sin ingreso directo desde internet"
  vpc_id      = var.vpc_id

  tags = { Name = "${var.name}-instances" }
}

# Salida a internet: descarga de imágenes de ECR, SES, SSM, actualizaciones.
resource "aws_vpc_security_group_egress_rule" "all" {
  security_group_id = aws_security_group.instances.id
  ip_protocol       = "-1"
  cidr_ipv4         = "0.0.0.0/0"
  description       = "Salida a internet"
}
