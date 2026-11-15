output "schedule_group_name" {
  value = aws_scheduler_schedule_group.this.name
}

output "scheduler_role_arn" {
  description = "Rol que usan los schedules; el encendido a demanda lo reutiliza para el apagado automático."
  value       = aws_iam_role.scheduler.arn
}
