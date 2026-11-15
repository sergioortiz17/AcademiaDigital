output "arn" {
  value = aws_codeconnections_connection.github.arn
}

output "status" {
  description = "PENDING hasta que se complete en la consola; después AVAILABLE."
  value       = aws_codeconnections_connection.github.connection_status
}
