data "aws_partition" "current" {}

locals {
  managed_policy_prefix = "arn:${data.aws_partition.current.partition}:iam::aws:policy"
}

# ── Rol de servicio de Beanstalk (health mejorado y actualizaciones gestionadas) ──────
resource "aws_iam_role" "service" {
  name = "${var.name}-eb-service"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "elasticbeanstalk.amazonaws.com" }
      Action    = "sts:AssumeRole"
      Condition = { StringEquals = { "sts:ExternalId" = "elasticbeanstalk" } }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "service" {
  for_each = toset([
    "${local.managed_policy_prefix}/service-role/AWSElasticBeanstalkEnhancedHealth",
    "${local.managed_policy_prefix}/AWSElasticBeanstalkManagedUpdatesCustomerRolePolicy",
  ])

  role       = aws_iam_role.service.name
  policy_arn = each.value
}

# ── Rol de las instancias ────────────────────────────────────────────────────────────
resource "aws_iam_role" "instance" {
  name = "${var.name}-eb-instance"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })
}

resource "aws_iam_role_policy_attachment" "instance" {
  for_each = toset([
    "${local.managed_policy_prefix}/AWSElasticBeanstalkWebTier",
    "${local.managed_policy_prefix}/AmazonEC2ContainerRegistryReadOnly",
    # Acceso por Session Manager en lugar de abrir SSH.
    "${local.managed_policy_prefix}/AmazonSSMManagedInstanceCore",
  ])

  role       = aws_iam_role.instance.name
  policy_arn = each.value
}

resource "aws_iam_role_policy" "instance_app" {
  name = "app-access"
  role = aws_iam_role.instance.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "FilesBucketObjects"
        Effect   = "Allow"
        Action   = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
        Resource = "${var.files_bucket_arn}/*"
      },
      {
        # Documentos de los alumnos: la app sube y lee; no borra (un reemplazo queda versionado).
        Sid      = "StudentDocumentsObjects"
        Effect   = "Allow"
        Action   = ["s3:GetObject", "s3:PutObject"]
        Resource = "${var.student_documents_bucket_arn}/*"
      },
      {
        Sid      = "FilesBucketList"
        Effect   = "Allow"
        Action   = ["s3:ListBucket"]
        Resource = var.files_bucket_arn
      },
      {
        # SES autoriza contra la identidad del remitente y, con la cuenta en sandbox, también
        # contra la del destinatario (cada email verificado es una identidad). Por eso se
        # permite identity/* de esta cuenta y región, no solo el dominio remitente.
        Sid      = "SendEmail"
        Effect   = "Allow"
        Action   = ["ses:SendEmail", "ses:SendRawEmail"]
        Resource = "${join(":", slice(split(":", var.ses_identity_arn), 0, 5))}:identity/*"
      },
      {
        # Beanstalk lee los secretos con el rol de la instancia al arrancarla.
        Sid      = "ReadEnvironmentSecrets"
        Effect   = "Allow"
        Action   = ["ssm:GetParameter", "ssm:GetParameters"]
        Resource = values(var.secret_parameter_arns)
      },
    ]
  })
}

resource "aws_iam_instance_profile" "instance" {
  name = "${var.name}-eb-instance"
  role = aws_iam_role.instance.name
}
