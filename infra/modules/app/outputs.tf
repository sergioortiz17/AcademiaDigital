output "application_name" {
  value = aws_elastic_beanstalk_application.this.name
}

output "environment_name" {
  value = aws_elastic_beanstalk_environment.this.name
}

output "environment_cname" {
  value = aws_elastic_beanstalk_environment.this.cname
}

output "autoscaling_group_name" {
  description = "ASG que crea Beanstalk; lo usan el apagado programado y el encendido a demanda."
  value       = one(aws_elastic_beanstalk_environment.this.autoscaling_groups)
}

output "autoscaling_group_arn" {
  value = data.aws_autoscaling_group.this.arn
}

output "solution_stack_name" {
  value = data.aws_elastic_beanstalk_solution_stack.docker.name
}
