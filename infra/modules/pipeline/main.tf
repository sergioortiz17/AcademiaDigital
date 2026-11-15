# CI/CD dentro de la cuenta: GitHub → CodePipeline → CodeBuild (arm64) → ECR → Beanstalk.
# GitHub no guarda credenciales de AWS: AWS lee el repositorio por la conexión de CodeConnections.
#
# Etapas:
#   Source  push a la rama configurada (por defecto main)
#   Build   deploy/buildspec.yml: construye backend, finance y frontend para arm64, los sube
#           a ECR con el SHA del commit y arma el bundle de Beanstalk (docker-compose.yml)
#   Deploy  sube el bundle como versión nueva del entorno (solo si deploy_enabled = true)

# ── Bucket de artefactos ─────────────────────────────────────────────────────

resource "aws_s3_bucket" "artifacts" {
  bucket = "${var.name}-pipeline-${data.aws_caller_identity.current.account_id}"
}

resource "aws_s3_bucket_ownership_controls" "artifacts" {
  bucket = aws_s3_bucket.artifacts.id
  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_public_access_block" "artifacts" {
  bucket                  = aws_s3_bucket.artifacts.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "artifacts" {
  bucket = aws_s3_bucket.artifacts.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_policy" "artifacts" {
  bucket = aws_s3_bucket.artifacts.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "DenyInsecureTransport"
      Effect    = "Deny"
      Principal = "*"
      Action    = "s3:*"
      Resource  = [aws_s3_bucket.artifacts.arn, "${aws_s3_bucket.artifacts.arn}/*"]
      Condition = { Bool = { "aws:SecureTransport" = "false" } }
    }]
  })

  depends_on = [aws_s3_bucket_public_access_block.artifacts]
}

# Los artefactos solo sirven durante la ejecución; Beanstalk guarda su propia copia del bundle.
resource "aws_s3_bucket_lifecycle_configuration" "artifacts" {
  bucket = aws_s3_bucket.artifacts.id
  rule {
    id     = "expire-artifacts"
    status = "Enabled"
    filter {}
    expiration {
      days = 30
    }
  }
}

# ── CodeBuild ────────────────────────────────────────────────────────────────

resource "aws_cloudwatch_log_group" "build" {
  name              = "/aws/codebuild/${var.name}-images"
  retention_in_days = var.log_retention_days
}

resource "aws_codebuild_project" "images" {
  name          = "${var.name}-images"
  description   = "Construye las imágenes arm64, las sube a ECR y arma el bundle de Beanstalk."
  service_role  = aws_iam_role.build.arn
  build_timeout = var.build_timeout_minutes

  source {
    type      = "CODEPIPELINE"
    buildspec = "deploy/buildspec.yml"
  }

  artifacts {
    type = "CODEPIPELINE"
  }

  environment {
    type                        = "ARM_CONTAINER"
    compute_type                = var.build_compute_type
    image                       = var.build_image
    image_pull_credentials_type = "CODEBUILD"
    privileged_mode             = true # necesario para docker build

    environment_variable {
      name  = "ECR_REGISTRY"
      value = var.ecr_registry
    }
    environment_variable {
      name  = "ECR_PREFIX"
      value = var.ecr_prefix
    }
  }

  # Reutiliza capas de Docker entre builds cercanos en el tiempo (sin costo extra).
  cache {
    type  = "LOCAL"
    modes = ["LOCAL_DOCKER_LAYER_CACHE"]
  }

  logs_config {
    cloudwatch_logs {
      group_name = aws_cloudwatch_log_group.build.name
    }
  }
}

# ── CodePipeline ─────────────────────────────────────────────────────────────

resource "aws_codepipeline" "this" {
  name           = "${var.name}-deploy"
  role_arn       = aws_iam_role.pipeline.arn
  pipeline_type  = "V2"
  execution_mode = "QUEUED" # cada push se despliega en orden; ninguno pisa al anterior

  artifact_store {
    type     = "S3"
    location = aws_s3_bucket.artifacts.bucket
  }

  stage {
    name = "Source"

    action {
      name             = "GitHub"
      category         = "Source"
      owner            = "AWS"
      provider         = "CodeStarSourceConnection"
      version          = "1"
      namespace        = "SourceVariables"
      output_artifacts = ["source"]

      configuration = {
        ConnectionArn        = var.connection_arn
        FullRepositoryId     = var.repository
        BranchName           = var.branch
        OutputArtifactFormat = "CODE_ZIP"
        DetectChanges        = "true"
      }
    }
  }

  stage {
    name = "Build"

    action {
      name             = "Images"
      category         = "Build"
      owner            = "AWS"
      provider         = "CodeBuild"
      version          = "1"
      input_artifacts  = ["source"]
      output_artifacts = ["bundle"]

      configuration = {
        ProjectName = aws_codebuild_project.images.name
        EnvironmentVariables = jsonencode([{
          name  = "IMAGE_TAG"
          value = "#{SourceVariables.CommitId}"
          type  = "PLAINTEXT"
        }])
      }
    }
  }

  dynamic "stage" {
    for_each = var.deploy_enabled ? [1] : []

    content {
      name = "Deploy"

      action {
        name            = "Beanstalk"
        category        = "Deploy"
        owner           = "AWS"
        provider        = "ElasticBeanstalk"
        version         = "1"
        input_artifacts = ["bundle"]

        configuration = {
          ApplicationName = var.name
          EnvironmentName = var.name
        }
      }
    }
  }

  depends_on = [aws_iam_role_policy.pipeline, aws_iam_role_policy_attachment.pipeline_beanstalk]
}
