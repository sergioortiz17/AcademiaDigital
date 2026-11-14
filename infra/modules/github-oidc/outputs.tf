output "role_arn" {
  description = "Rol a configurar en los workflows (aws-actions/configure-aws-credentials)."
  value       = aws_iam_role.github_actions.arn
}
