# Encendido y apagado de la instancia de monitoreo junto con la plataforma.
# Los logs quedan en S3 y las métricas en el disco: apagar no pierde datos.

resource "aws_iam_role" "scheduler" {
  count = var.schedule_enabled ? 1 : 0
  name  = "${var.name}-monitoring-scheduler"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "scheduler.amazonaws.com" }
      Action    = "sts:AssumeRole"
      Condition = { StringEquals = { "aws:SourceAccount" = data.aws_caller_identity.current.account_id } }
    }]
  })
}

resource "aws_iam_role_policy" "scheduler" {
  count = var.schedule_enabled ? 1 : 0
  name  = "start-stop-monitoring"
  role  = aws_iam_role.scheduler[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["ec2:StartInstances", "ec2:StopInstances"]
      Resource = aws_instance.this.arn
    }]
  })
}

locals {
  monitoring_schedules = var.schedule_enabled ? {
    start         = { cron = var.schedule.start, action = "startInstances" }
    stop-weekday  = { cron = var.schedule.stop_weekday, action = "stopInstances" }
    stop-saturday = { cron = var.schedule.stop_saturday, action = "stopInstances" }
  } : {}
}

resource "aws_scheduler_schedule" "this" {
  for_each = local.monitoring_schedules

  name                         = "${var.name}-monitoring-${each.key}"
  schedule_expression          = each.value.cron
  schedule_expression_timezone = var.schedule_timezone

  flexible_time_window {
    mode = "OFF"
  }

  target {
    arn      = "arn:aws:scheduler:::aws-sdk:ec2:${each.value.action}"
    role_arn = aws_iam_role.scheduler[0].arn
    input    = jsonencode({ InstanceIds = [aws_instance.this.id] })
  }
}
