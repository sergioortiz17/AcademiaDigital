output "parameter_arns" {
  description = "ARN de cada secreto, por nombre de variable de entorno."
  value = {
    BACKEND_DB_CONNECTION = aws_ssm_parameter.backend_db.arn
    FINANCE_DB_CONNECTION = aws_ssm_parameter.finance_db.arn
    JWT_SECRET_KEY        = aws_ssm_parameter.jwt.arn
  }
}
