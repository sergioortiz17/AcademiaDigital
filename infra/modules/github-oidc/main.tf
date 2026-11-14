# Rol de GitHub Actions para una institución. El proveedor OIDC es único por cuenta y
# vive en la unidad shared/github-oidc-provider.
# El rol solo lo pueden asumir workflows del repositorio y las ramas indicadas.
# Permisos: subir imágenes a ECR, desplegar en Beanstalk y encender/apagar a demanda.

data "aws_caller_identity" "current" {}

resource "aws_iam_role" "github_actions" {
  name = "${var.name}-github-actions"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Federated = var.oidc_provider_arn }
      Action    = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringEquals = { "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com" }
        StringLike   = { "token.actions.githubusercontent.com:sub" = [for ref in var.allowed_refs : "repo:${var.repository}:ref:${ref}"] }
      }
    }]
  })
}

# Despliegue de versiones en Beanstalk (crea versiones, sube el bundle, actualiza el entorno).
resource "aws_iam_role_policy_attachment" "beanstalk" {
  role       = aws_iam_role.github_actions.name
  policy_arn = "arn:aws:iam::aws:policy/AdministratorAccess-AWSElasticBeanstalk"
}

resource "aws_iam_role_policy" "github_actions" {
  name = "ci-and-operations"
  role = aws_iam_role.github_actions.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "EcrLogin"
        Effect   = "Allow"
        Action   = ["ecr:GetAuthorizationToken"]
        Resource = "*"
      },
      {
        Sid    = "EcrPush"
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:BatchGetImage",
          "ecr:CompleteLayerUpload",
          "ecr:DescribeImages",
          "ecr:InitiateLayerUpload",
          "ecr:PutImage",
          "ecr:UploadLayerPart",
        ]
        Resource = var.ecr_repository_arns
      },
      {
        Sid      = "PowerOnDemandAsg"
        Effect   = "Allow"
        Action   = ["autoscaling:UpdateAutoScalingGroup"]
        Resource = var.autoscaling_group_arn
      },
      {
        Sid      = "PowerOnDemandDb"
        Effect   = "Allow"
        Action   = ["rds:StartDBInstance", "rds:StopDBInstance"]
        Resource = var.db_instance_arn
      },
      {
        Sid      = "Describe"
        Effect   = "Allow"
        Action   = ["rds:DescribeDBInstances", "autoscaling:DescribeAutoScalingGroups"]
        Resource = "*"
      },
      {
        # Apagado automático que deja programado el encendido a demanda.
        Sid      = "OneTimeSchedules"
        Effect   = "Allow"
        Action   = ["scheduler:CreateSchedule", "scheduler:DeleteSchedule", "scheduler:GetSchedule"]
        Resource = "arn:aws:scheduler:*:${data.aws_caller_identity.current.account_id}:schedule/${var.schedule_group_name}/*"
      },
      {
        Sid       = "PassSchedulerRole"
        Effect    = "Allow"
        Action    = ["iam:PassRole"]
        Resource  = var.scheduler_role_arn
        Condition = { StringEquals = { "iam:PassedToService" = "scheduler.amazonaws.com" } }
      },
    ]
  })
}
