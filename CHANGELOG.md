# Changelog

Cambios relevantes de AcademiaDigital por versión. El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el número de versión, [versionado semántico](https://semver.org/lang/es/). Ver [VERSIONADO.md](VERSIONADO.md).

## [Sin publicar]

## [1.3.0] - 2026-11-15

Multitenant: una base de datos por institución sobre la misma plataforma.

### Agregado

- **Base de control** (`academiadigital_control`) con el catálogo de instituciones y sus dominios. El ITSC queda como institución por defecto sobre su base actual, sin mover datos.
- **Resolución de la institución por dominio**: el frontend envía el dominio del navegador en `X-Tenant` (o se toma de `Origin`); el backend elige la base de esa institución en cada request. Un dominio no registrado responde 404.
- **Alta de instituciones** desde la pantalla `/platform` y la API `api/v1/platform/tenants`, protegidas con la clave de operador `X-Platform-Key` (`Platform__AdminKey`). Sin clave configurada la API de plataforma queda deshabilitada. El alta crea y migra la base de la institución y su usuario administrador.
- **CORS dinámico** para los dominios registrados, sin reiniciar.
- Archivos y certificados separados por institución.

### Cambiado

- Las sesiones y los usuarios viven en la base de cada institución: un usuario o un token de una institución no sirven en otra.
- El microservicio Finance solo atiende a la institución por defecto.

### Corregido

- `itsc.dryvn.org` registrado como dominio de la institución por defecto: sin él, en producción todas las llamadas a la API respondían 404.
- El identificador `finance` queda reservado: su base (`academiadigital_finance`) es la del microservicio Finance.

## [1.2.0] - 2026-11-14

Archivos en S3: la plataforma puede correr en varias instancias.

### Agregado

- **`S3FileStorage`** en el monolito y en Finance: acuerdos, certificados y recibos se guardan en el bucket de archivos (`Storage:Provider=S3`), con el rol de la instancia y sin claves. Cada servicio usa su prefijo (`backend/`, `finance/`). Las claves lógicas guardadas en la base no cambian.
- **IMDSv2 con hop limit 2** por defecto para las instancias nuevas de la cuenta (`infra/live/shared/ec2-metadata-defaults`): sin esto, los contenedores de Beanstalk no obtienen las credenciales del rol.

### Cambiado

- El adaptador de almacenamiento se elige por configuración. Por defecto sigue siendo el disco local (desarrollo y docker-compose); Beanstalk usa S3.
- Validación de claves de almacenamiento: se rechazan segmentos vacíos, `.` y `..`.

### Corregido

- Con más de una instancia, un PDF generado en una no existía en la otra y se perdía al reemplazarla.

## [1.1.0] - 2026-11-14

Observabilidad autogestionada: quién entra, desde dónde, qué errores hay y cómo están las instancias. En producción se guardan **solo errores y accesos**.

### Agregado

- **Logs estructurados en JSON** en backend y Finance fuera de Development.
- **Solo errores en producción:** `Observability__MinimumLevel=Error` sube el nivel mínimo de todas las categorías sin recompilar; Info y Warning no se escriben. Alloy aplica el mismo filtro como segunda barrera y descarta el access log de nginx.
- **Auditoría de accesos** (`AuditLog`, categoría propia que queda activa aunque el resto esté en Error): `login_success` (usuario, rol, IP, user agent), `login_failed` (estado e IP, sin email) y `logout`. Se desactiva con `Observability__AuditEnabled=false`.
- **Línea por request** (`Event = http_request`: método, ruta sin query string, estado, duración, IP, usuario, rol); en producción solo se guardan las de estado 5xx.
- **Errores del frontend:** `ErrorHandler` global de Angular que reporta los errores de JavaScript a `POST /api/v1/client-errors` (anónimo, limitado por IP, sin query string ni datos de formularios).
- **Monitoreo en AWS** (`infra/modules/monitoring`): EC2 arm64 con Grafana, Loki (logs en S3, retención de 90 días) y Prometheus (métricas, 30 días), sin puertos abiertos a internet; Grafana se abre con Session Manager. Dashboard "Accesos, errores e instancias" versionado en el repositorio.
- **Agente Grafana Alloy** en cada instancia de Beanstalk: logs filtrados a Loki y métricas del host a Prometheus.

### Cambiado

- **IP real del cliente detrás del ALB:** con `ReverseProxy__TrustForwardedHeaders=true` (solo en Beanstalk) el backend toma la IP de `X-Forwarded-For`. Corrige también el rate limiter de admisiones, que veía la IP del proxy para todos.
- **`ExceptionMiddleware`** registra como Error solo las excepciones que terminan en 5xx; las de negocio esperadas (credenciales inválidas, conflictos, no encontrado) pasan a Information y no llenan el log de errores.
- **Swagger y Scalar apagados en producción** (`Swagger__Enabled=false`) en el monolito y en Finance; siguen activos en desarrollo y en el docker-compose local.
- Logs de Entity Framework en `Warning` por defecto: las consultas SQL dejan de escribirse en el log.

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

[Sin publicar]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.3.0...HEAD
[1.3.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/sergioortiz17/AcademiaDigital/releases/tag/v1.0.0
