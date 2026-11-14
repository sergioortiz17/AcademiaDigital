# Elastic Beanstalk con plataforma Docker (Amazon Linux 2023) y balanceador ALB.
# Terraform es dueño de la aplicación, el entorno y TODA su configuración.
# La CI solo sube versiones nuevas de la aplicación (bundle con docker-compose.yml).
# No cambiar configuración desde la consola ni con `eb setenv`: Terraform lo revierte.

data "aws_elastic_beanstalk_solution_stack" "docker" {
  most_recent = true
  name_regex  = "^64bit Amazon Linux 2023 (.*) running Docker$"
}

resource "aws_elastic_beanstalk_application" "this" {
  name        = var.name
  description = "AcademiaDigital"

  appversion_lifecycle {
    service_role          = aws_iam_role.service.arn
    max_count             = 30
    delete_source_from_s3 = true
  }
}

locals {
  base_settings = [
    # Red
    { namespace = "aws:ec2:vpc", name = "VPCId", value = var.vpc_id },
    { namespace = "aws:ec2:vpc", name = "Subnets", value = join(",", var.instance_subnet_ids) },
    { namespace = "aws:ec2:vpc", name = "ELBSubnets", value = join(",", var.load_balancer_subnet_ids) },
    { namespace = "aws:ec2:vpc", name = "ELBScheme", value = "public" },
    # Subnets públicas sin NAT: las instancias necesitan IP pública para salir a internet.
    { namespace = "aws:ec2:vpc", name = "AssociatePublicIpAddress", value = "true" },

    # Instancias
    { namespace = "aws:ec2:instances", name = "InstanceTypes", value = var.instance_type },
    { namespace = "aws:ec2:instances", name = "SupportedArchitectures", value = var.instance_architecture },
    { namespace = "aws:autoscaling:launchconfiguration", name = "IamInstanceProfile", value = aws_iam_instance_profile.instance.name },
    { namespace = "aws:autoscaling:launchconfiguration", name = "SecurityGroups", value = var.instance_security_group_id },
    { namespace = "aws:autoscaling:launchconfiguration", name = "RootVolumeType", value = "gp3" },
    { namespace = "aws:autoscaling:launchconfiguration", name = "RootVolumeSize", value = tostring(var.root_volume_size_gb) },
    { namespace = "aws:autoscaling:launchconfiguration", name = "DisableIMDSv1", value = "true" },

    # Capacidad en horario de encendido (el apagado nocturno lo maneja el módulo schedule).
    { namespace = "aws:autoscaling:asg", name = "MinSize", value = tostring(var.instance_count) },
    { namespace = "aws:autoscaling:asg", name = "MaxSize", value = tostring(var.instance_count) },

    # Entorno con balanceador
    { namespace = "aws:elasticbeanstalk:environment", name = "EnvironmentType", value = "LoadBalanced" },
    { namespace = "aws:elasticbeanstalk:environment", name = "LoadBalancerType", value = "application" },
    { namespace = "aws:elasticbeanstalk:environment", name = "ServiceRole", value = aws_iam_role.service.arn },

    # HTTPS con el certificado de ACM. HTTP queda habilitado; la redirección a HTTPS se
    # resuelve en el nginx del frontend con X-Forwarded-Proto (pendiente, ver README).
    { namespace = "aws:elbv2:listener:443", name = "ListenerEnabled", value = "true" },
    { namespace = "aws:elbv2:listener:443", name = "Protocol", value = "HTTPS" },
    { namespace = "aws:elbv2:listener:443", name = "SSLCertificateArns", value = var.certificate_arn },
    { namespace = "aws:elbv2:listener:443", name = "SSLPolicy", value = "ELBSecurityPolicy-TLS13-1-2-2021-06" },
    { namespace = "aws:elbv2:listener:default", name = "ListenerEnabled", value = "true" },

    # Proceso que recibe el tráfico: el nginx del frontend en el puerto 80 de la instancia.
    { namespace = "aws:elasticbeanstalk:environment:process:default", name = "Port", value = "80" },
    { namespace = "aws:elasticbeanstalk:environment:process:default", name = "Protocol", value = "HTTP" },
    { namespace = "aws:elasticbeanstalk:environment:process:default", name = "HealthCheckPath", value = var.health_check_path },
    { namespace = "aws:elasticbeanstalk:environment:process:default", name = "MatcherHTTPCode", value = "200" },

    # Salud y despliegues sin perder capacidad
    { namespace = "aws:elasticbeanstalk:healthreporting:system", name = "SystemType", value = "enhanced" },
    { namespace = "aws:elasticbeanstalk:command", name = "DeploymentPolicy", value = "RollingWithAdditionalBatch" },
    { namespace = "aws:elasticbeanstalk:command", name = "BatchSizeType", value = "Fixed" },
    { namespace = "aws:elasticbeanstalk:command", name = "BatchSize", value = "1" },
    { namespace = "aws:autoscaling:updatepolicy:rollingupdate", name = "RollingUpdateEnabled", value = "true" },
    { namespace = "aws:autoscaling:updatepolicy:rollingupdate", name = "RollingUpdateType", value = "Health" },

    # Parches de plataforma gestionados (ventana en UTC, dentro del horario de encendido).
    { namespace = "aws:elasticbeanstalk:managedactions", name = "ManagedActionsEnabled", value = "true" },
    { namespace = "aws:elasticbeanstalk:managedactions", name = "PreferredStartTime", value = var.managed_updates_start_time },
    { namespace = "aws:elasticbeanstalk:managedactions:platformupdate", name = "UpdateLevel", value = "minor" },

    # Logs de las instancias y contenedores a CloudWatch
    { namespace = "aws:elasticbeanstalk:cloudwatch:logs", name = "StreamLogs", value = "true" },
    { namespace = "aws:elasticbeanstalk:cloudwatch:logs", name = "RetentionInDays", value = tostring(var.log_retention_days) },
    { namespace = "aws:elasticbeanstalk:cloudwatch:logs", name = "DeleteOnTerminate", value = "false" },
  ]

  env_settings = [
    for key, value in var.environment_variables :
    { namespace = "aws:elasticbeanstalk:application:environment", name = key, value = value }
  ]

  secret_settings = [
    for key, arn in var.secret_parameter_arns :
    { namespace = "aws:elasticbeanstalk:application:environmentsecrets", name = key, value = arn }
  ]

  settings = concat(local.base_settings, local.env_settings, local.secret_settings)
}

resource "aws_elastic_beanstalk_environment" "this" {
  name                = var.name
  application         = aws_elastic_beanstalk_application.this.name
  solution_stack_name = data.aws_elastic_beanstalk_solution_stack.docker.name
  tier                = "WebServer"

  dynamic "setting" {
    for_each = local.settings

    content {
      namespace = setting.value.namespace
      name      = setting.value.name
      value     = setting.value.value
    }
  }
}

# El ASG lo crea Beanstalk; se expone su ARN para el horario de servicio y la CI.
data "aws_autoscaling_group" "this" {
  name = one(aws_elastic_beanstalk_environment.this.autoscaling_groups)
}

data "aws_elastic_beanstalk_hosted_zone" "current" {}

resource "aws_route53_record" "app" {
  zone_id = var.dns_zone_id
  name    = var.domain_name
  type    = "A"

  alias {
    name                   = aws_elastic_beanstalk_environment.this.cname
    zone_id                = data.aws_elastic_beanstalk_hosted_zone.current.id
    evaluate_target_health = true
  }
}
