variable "name" {
  type = string
}

variable "repository" {
  description = "Repositorio de GitHub en formato owner/repo."
  type        = string
}

variable "allowed_refs" {
  description = "Refs que pueden asumir el rol."
  type        = list(string)
  default     = ["refs/heads/main"]
}

variable "oidc_provider_arn" {
  description = "Proveedor OIDC de GitHub de la cuenta (unidad shared/github-oidc-provider)."
  type        = string
}

variable "ecr_repository_arns" {
  type = list(string)
}

variable "autoscaling_group_arn" {
  type = string
}

variable "db_instance_arn" {
  type = string
}

variable "schedule_group_name" {
  type = string
}

variable "scheduler_role_arn" {
  type = string
}
