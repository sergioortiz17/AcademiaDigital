# Institución: Instituto Técnico Superior Córdoba (ITSC).
# Para sumar otra institución: copiar esta carpeta, cambiar estos valores y aplicar.
locals {
  scope       = "client"
  client      = "itsc"
  environment = "prod"

  domain_name = "itsc.dryvn.org"

  # Capacidad
  instance_type         = "t4g.small"
  instance_architecture = "arm64"
  instance_count        = 1 # pasar a 2 para alta disponibilidad: aplicar app y schedule (ver DESPLIEGUE-ITSC.md)
  db_instance_class     = "db.t4g.micro"
  db_multi_az           = false # una sola instancia: prioridad costo. Activar antes de abrir al público si se necesita alta disponibilidad

  # Horario de servicio (modules/schedule). false = siempre encendido.
  power_schedule_enabled = true

  github_repository = "sergioortiz17/AcademiaDigital"
}
