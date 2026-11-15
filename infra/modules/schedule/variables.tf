variable "name" {
  description = "Nombre del grupo de schedules y prefijo del rol."
  type        = string
}

variable "enabled" {
  description = "false deja la plataforma siempre encendida (no crea los schedules)."
  type        = bool
  default     = true
}

variable "timezone" {
  type    = string
  default = "America/Argentina/Cordoba"
}

variable "cron" {
  description = "Expresiones cron de EventBridge Scheduler, en la zona horaria indicada."
  type = object({
    db_start          = string
    app_start         = string
    app_stop_weekday  = string
    db_stop_weekday   = string
    app_stop_saturday = string
    db_stop_saturday  = string
  })
  default = {
    db_start          = "cron(40 6 ? * MON-SAT *)"
    app_start         = "cron(0 7 ? * MON-SAT *)"
    app_stop_weekday  = "cron(0 22 ? * MON-FRI *)"
    db_stop_weekday   = "cron(15 22 ? * MON-FRI *)"
    app_stop_saturday = "cron(0 13 ? * SAT *)"
    db_stop_saturday  = "cron(15 13 ? * SAT *)"
  }
}

variable "autoscaling_group_name" {
  type = string
}

variable "autoscaling_group_arn" {
  type = string
}

variable "instance_count" {
  description = "Instancias al encender."
  type        = number
}

variable "db_instance_identifier" {
  type = string
}

variable "db_instance_arn" {
  type = string
}
