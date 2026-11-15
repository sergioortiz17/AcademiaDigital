variable "name" {
  description = "Prefijo de los recursos (academiadigital-<institución>-<ambiente>)."
  type        = string
}

variable "vpc_id" {
  type = string
}

variable "subnet_id" {
  description = "Subnet pública: la instancia necesita salir a internet (no hay NAT), pero no recibe tráfico de internet."
  type        = string
}

variable "private_ip_hostnum" {
  description = "Host dentro del CIDR de la subnet para la IP privada fija (las apps envían logs y métricas a esa IP)."
  type        = number
  default     = 10
}

variable "app_security_group_id" {
  description = "Security group de las instancias de Beanstalk: es el único origen que puede enviar logs y métricas."
  type        = string
}

variable "instance_type" {
  description = "Graviton chica: Grafana + Loki + Prometheus con el volumen de un instituto."
  type        = string
  default     = "t4g.small"
}

variable "root_volume_size_gb" {
  description = "Disco para Prometheus (TSDB), el WAL y la caché de Loki y la base de Grafana."
  type        = number
  default     = 30
}

variable "loki_retention_days" {
  description = "Días que Loki conserva los logs en S3. Alinear con lo que la institución informe sobre datos personales."
  type        = number
  default     = 90
}

variable "prometheus_retention_days" {
  type    = number
  default = 30
}

variable "images" {
  description = "Imágenes de los contenedores. Versiones fijas: actualizarlas a propósito, no por accidente."
  type = object({
    grafana    = string
    loki       = string
    prometheus = string
  })
  default = {
    grafana    = "grafana/grafana:11.4.0"
    loki       = "grafana/loki:3.3.2"
    prometheus = "prom/prometheus:v3.0.1"
  }
}

variable "docker_compose_version" {
  description = "Plugin de Docker Compose (AL2023 no lo trae en sus repositorios)."
  type        = string
  default     = "v2.32.1"
}

# Horario: la instancia sigue el horario de la plataforma (no hay nada que mirar con todo apagado).
variable "schedule_enabled" {
  type    = bool
  default = true
}

variable "schedule_timezone" {
  type    = string
  default = "America/Argentina/Cordoba"
}

variable "schedule" {
  description = "Encendido antes que la app y apagado después, para registrar el arranque y el cierre."
  type = object({
    start         = string
    stop_weekday  = string
    stop_saturday = string
  })
  default = {
    start         = "cron(50 6 ? * MON-SAT *)"
    stop_weekday  = "cron(30 22 ? * MON-FRI *)"
    stop_saturday = "cron(30 13 ? * SAT *)"
  }
}
