# Horario de servicio con EventBridge Scheduler, en hora local (sin conversiones a UTC).
#   Lunes a viernes 07:00-22:00 · Sábado 07:00-13:00 · Domingo apagado.
# La base arranca antes que la app y se detiene después, para que la app nunca quede
# encendida sin base. Las acciones actúan directo sobre el ASG de Beanstalk (igual que
# sus acciones programadas), así que no cambian la configuración que maneja Terraform.

data "aws_caller_identity" "current" {}

locals {
  app_on = {
    AutoScalingGroupName = var.autoscaling_group_name
    MinSize              = var.instance_count
    MaxSize              = var.instance_count
    DesiredCapacity      = var.instance_count
  }

  app_off = {
    AutoScalingGroupName = var.autoscaling_group_name
    MinSize              = 0
    MaxSize              = 0
    DesiredCapacity      = 0
  }

  db = { DbInstanceIdentifier = var.db_instance_identifier }

  update_asg = "arn:aws:scheduler:::aws-sdk:autoscaling:updateAutoScalingGroup"

  schedules = {
    db-start = {
      description = "Arranca la base antes de abrir (lunes a sábado)"
      expression  = var.cron.db_start
      target      = "arn:aws:scheduler:::aws-sdk:rds:startDBInstance"
      input       = local.db
    }
    app-start = {
      description = "Enciende la app (lunes a sábado)"
      expression  = var.cron.app_start
      target      = local.update_asg
      input       = local.app_on
    }
    app-stop-weekday = {
      description = "Apaga la app (lunes a viernes)"
      expression  = var.cron.app_stop_weekday
      target      = local.update_asg
      input       = local.app_off
    }
    db-stop-weekday = {
      description = "Detiene la base después de la app (lunes a viernes)"
      expression  = var.cron.db_stop_weekday
      target      = "arn:aws:scheduler:::aws-sdk:rds:stopDBInstance"
      input       = local.db
    }
    app-stop-saturday = {
      description = "Apaga la app (sábado)"
      expression  = var.cron.app_stop_saturday
      target      = local.update_asg
      input       = local.app_off
    }
    db-stop-saturday = {
      description = "Detiene la base después de la app (sábado)"
      expression  = var.cron.db_stop_saturday
      target      = "arn:aws:scheduler:::aws-sdk:rds:stopDBInstance"
      input       = local.db
    }
  }
}

resource "aws_scheduler_schedule_group" "this" {
  name = var.name
}

resource "aws_iam_role" "scheduler" {
  name = "${var.name}-scheduler"

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
  name = "power-schedule"
  role = aws_iam_role.scheduler.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["autoscaling:UpdateAutoScalingGroup"]
        Resource = var.autoscaling_group_arn
      },
      {
        Effect   = "Allow"
        Action   = ["rds:StartDBInstance", "rds:StopDBInstance"]
        Resource = var.db_instance_arn
      },
    ]
  })
}

resource "aws_scheduler_schedule" "this" {
  for_each = var.enabled ? local.schedules : {}

  name        = each.key
  group_name  = aws_scheduler_schedule_group.this.name
  description = each.value.description

  schedule_expression          = each.value.expression
  schedule_expression_timezone = var.timezone

  flexible_time_window {
    mode = "OFF"
  }

  target {
    arn      = each.value.target
    role_arn = aws_iam_role.scheduler.arn
    input    = jsonencode(each.value.input)

    retry_policy {
      maximum_retry_attempts       = 3
      maximum_event_age_in_seconds = 900
    }
  }
}
