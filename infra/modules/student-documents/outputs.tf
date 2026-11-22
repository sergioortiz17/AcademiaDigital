output "bucket_name" {
  value = aws_s3_bucket.documents.bucket
}

output "bucket_arn" {
  value = aws_s3_bucket.documents.arn
}

output "read_policy_arn" {
  description = "Política de solo lectura para asociar a quien descarga los documentos."
  value       = aws_iam_policy.read.arn
}
