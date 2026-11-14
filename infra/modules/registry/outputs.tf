output "repository_arns" {
  value = [for repo in aws_ecr_repository.this : repo.arn]
}

output "repository_urls" {
  value = { for name, repo in aws_ecr_repository.this : name => repo.repository_url }
}
