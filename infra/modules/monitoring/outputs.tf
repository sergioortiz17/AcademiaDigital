output "instance_id" {
  description = "Para Session Manager: aws ssm start-session --target <id> ..."
  value       = aws_instance.this.id
}

output "private_ip" {
  value = aws_instance.this.private_ip
}

output "loki_push_url" {
  description = "Endpoint al que Alloy (en Beanstalk) envía los logs."
  value       = "http://${aws_instance.this.private_ip}:3100/loki/api/v1/push"
}

output "prometheus_remote_write_url" {
  description = "Endpoint al que Alloy (en Beanstalk) envía las métricas."
  value       = "http://${aws_instance.this.private_ip}:9090/api/v1/write"
}

output "grafana_password_parameter" {
  description = "Parámetro SSM con la contraseña del usuario admin de Grafana."
  value       = aws_ssm_parameter.grafana_admin.name
}

output "bucket" {
  value = aws_s3_bucket.this.bucket
}
