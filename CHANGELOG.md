# Changelog

Cambios relevantes de AcademiaDigital por versión. El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el número de versión, [versionado semántico](https://semver.org/lang/es/). Ver [VERSIONADO.md](VERSIONADO.md).

## [Sin publicar]

## [1.1.0] - 2026-11-14

Observabilidad autogestionada: quién se conecta, desde dónde, qué errores hay y cómo están las instancias.

### Agregado

- **Logs estructurados en JSON** en backend y Finance fuera de Development, con una línea por request (`Event = http_request`): método, ruta, estado, duración, IP real, usuario, rol y user agent. Sin query string ni cuerpos.
- **Auditoría de accesos:** evento `login_success` con usuario, rol e IP. Los intentos fallidos quedan en el log del request (401/423 en `/api/v1/users/login`).
- **Errores del frontend:** `ErrorHandler` global de Angular que reporta los errores de JavaScript a `POST /api/v1/client-errors` (anónimo, limitado por IP, sin query string ni datos de formularios).
- **Monitoreo en AWS** (`infra/modules/monitoring`): EC2 arm64 con Grafana, Loki (logs en S3, retención de 90 días) y Prometheus (métricas, 30 días), sin puertos abiertos a internet; Grafana se abre con Session Manager. Dashboard "Accesos, errores e instancias" versionado en el repositorio.
- **Agente Grafana Alloy** en cada instancia de Beanstalk: logs de los contenedores a Loki y métricas del host a Prometheus.

### Cambiado

- **IP real del cliente detrás del ALB:** con `ReverseProxy__TrustForwardedHeaders=true` (solo en Beanstalk) el backend toma la IP de `X-Forwarded-For`. Corrige también el rate limiter de admisiones, que veía la IP del proxy para todos.
- Logs de Entity Framework en `Warning` en producción: las consultas SQL dejan de escribirse en el log.

## [1.0.0] - 2026-11-14

Primera versión de producción de la plataforma.

### Agregado

- **Frontend (Angular 21):** módulos de autenticación (inicio de sesión, registro, recuperación de contraseña), dashboard por rol (alumno, profesor, administración), carreras y materias, inscripciones a cursado y a mesas de examen, calificaciones, asistencia, historial académico, certificados, calendario, mensajes, perfil, docentes y administración.
- **Acceso rápido a Google Classroom** desde el dashboard para los tres roles.
- **Backend (.NET 8):** API del monolito para usuarios, admisiones, carreras, planes de estudio, correlatividades, comisiones y divisiones, inscripciones y rematriculaciones, libretas de calificaciones, mesas de examen, asistencia, calendario, certificados y docentes, sobre PostgreSQL.
- **Microservicio Finance (.NET 8):** servicio separado con su propia base de datos, integrado con el monolito por HTTP.
- **Infraestructura en AWS con Terragrunt y Terraform:** VPC propia, RDS PostgreSQL, Elastic Beanstalk con Docker sobre arm64, certificado HTTPS de ACM, correo con SES, archivos en S3, horario de encendido y apagado, presupuesto con alertas.
- **CI/CD:** CodePipeline y CodeBuild arm64 dentro de la cuenta de AWS, conectados a GitHub sin credenciales guardadas.
- **Versionado semántico** con una versión única para frontend, backend y Finance (`VERSION`, `tools/version.sh`).
- Plan de pruebas de sistema con 75 casos (`PRUEBAS_SISTEMA.md`).

[Sin publicar]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/sergioortiz17/AcademiaDigital/releases/tag/v1.0.0
