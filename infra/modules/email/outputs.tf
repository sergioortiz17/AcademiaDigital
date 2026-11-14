output "identity_arn" {
  value = aws_sesv2_email_identity.this.arn
}

output "domain_name" {
  value = aws_sesv2_email_identity.this.email_identity
}
