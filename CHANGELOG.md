# Changelog

Cambios relevantes de AcademiaDigital por versión. El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el número de versión, [versionado semántico](https://semver.org/lang/es/). Ver [VERSIONADO.md](VERSIONADO.md).

## [Sin publicar]

## [1.10.0] - 2026-11-20

Asignaciones de a varios y arreglos para celular.

### Agregado

- **Asignar división a varios alumnos a la vez** (menú "Asignaciones"): se eligen carrera y división y se marcan los alumnos de una lista con buscador (nombre, legajo o DNI), "Solo sin división" y "Seleccionar todos". Cada alumno muestra su división actual. El plan de estudios, el año académico y el año del plan los toma el servidor. Endpoint `POST /api/v1/careers/{careerId}/divisions/{id}/students` (hasta 300 alumnos). Si un alumno falla, los demás se asignan igual y la pantalla lista los que no se pudieron asignar y por qué.
- **Asignar materias a un docente en un paso** (legajo del docente → "Asignar materias"): se eligen carrera, división y cargo, y se marcan las materias del año de la división ("Ver otros años" muestra el resto del plan). Ya no hace falta crear antes la comisión: si existe una vacante se usa, y si no existe se crea con el cuatrimestre (o anual) del plan vigente. Cada materia indica si tiene comisión vacante, si la comisión se crea, si ya es del docente o si la tiene otro docente. Endpoint `POST /api/v1/teachers/{id}/assignments/bulk`.

### Corregido

- **Celular en horizontal:** no se podía desplazar la página hasta el final porque el alto usaba `100vh`, que en Android incluye la barra del navegador. Ahora usa `100dvh`.
- **"Activar inscripción"** se superponía al título "Gestión de Inscripciones" en Android: el encabezado ahora pasa el botón abajo cuando no entra.
- **Diálogos más anchos que la pantalla en el celular** (13 diálogos con ancho mínimo fijo): ahora el ancho mínimo nunca supera el de la pantalla.
- **Etiquetas cortadas en los diálogos del celular** ("Carre" en vez de "Carrera"): un estilo global pisaba el ancho que Material le da a la etiqueta flotante.
- Campos de filtro de "Condición de alumnos" y "Comisiones" que desbordaban en pantallas chicas, y la ayuda del legajo docente que se superponía en el formulario de docente.
- Las comisiones creadas para materias anuales ahora quedan marcadas como anuales (antes nunca se guardaba).

## [1.9.0] - 2026-11-15

### Agregado

- **Animaciones en la landing** (Lottie): educación en la portada, indicadores de gestión en "Cómo empezar" (sus violetas pasados al azul de la landing) y un avión de papel sobre el formulario, recoloreado con el azul metálico (el ala lleva un degradado). Provienen del proyecto `gestion-academica-frontend`.
- **Ícono de la pestaña (favicon):** el librito de Academia Digital en blanco sobre el azul metálico de la landing (`favicon.svg`, con versiones `.ico` y `apple-touch-icon.png`). Reemplaza el ícono de Angular que se veía en producción. **El ITSC muestra su propio logo** (la "it" en su azul, `favicon-itsc.svg`): la aplicación cambia el ícono al reconocer la institución por defecto. La landing, el panel y las demás instituciones usan el de Academia Digital. El librito es el ícono `auto_stories` de Material Symbols (Google, Apache 2.0).
- El reproductor (`lottie-web` 5.13.0, versión liviana solo SVG) se descarga recién cuando la animación entra en pantalla, en un fragmento aparte de 42 KB comprimido; no afecta al resto de la aplicación. Las animaciones se pausan fuera de pantalla y respetan "reducir movimiento" del sistema.

## [1.8.0] - 2026-11-15

Landing comercial y pedidos de plataforma.

### Agregado

- **Landing en `academiadigital.<dominio>`** (en local, `/landing`): azul metálico, beneficios, "Cómo empezar", "Instituciones que confían en nosotros" (ITSC) y el formulario **"Pide tu plataforma"**, con el subdominio deseado y una vista previa (`<slug>.dryvn.org`).
- **Pedidos de plataforma** (`PlatformRequests`, esquema `control`): `POST /api/v1/platform/requests` es público (5 por hora por IP, trampa para bots, subdominio validado y no tomado). Listarlos (`GET`) y revisarlos (`PATCH`) requiere la clave de plataforma. Cada pedido avisa por correo a `Platform:NotifyEmail`.
- **Panel en `admin.<dominio>`** (`/platform`): lista de pedidos con "Crear institución", que precarga subdominio, dominio `<slug>.dryvn.org` y admin inicial, y aprueba el pedido al crearla. También permite "Rechazar".
- La raíz del sitio depende del dominio: `academiadigital.*` → landing, `admin.*` → panel, cualquier otro → login de la institución.
- Infra: certificado comodín `*.dryvn.org` (unidad `itsc/wildcard-certificate`, validado en Cloudflare) para el ALB, y `Platform__NotifyEmail` en Beanstalk.

## [1.7.0] - 2026-11-15

Multitenant con base compartida. Ver [MULTITENANT_BASE_COMPARTIDA.md](MULTITENANT_BASE_COMPARTIDA.md).

### Cambiado

- **Todas las instituciones comparten la base `academiadigital`, cada una en su propio esquema.** ITSC sigue en `public` (no se migran datos), el catálogo de instituciones pasa de la base `academiadigital_control` al esquema `control`, y una institución nueva (`usb`) se crea como esquema `usb`. `Tenant.DatabaseName` pasa a `Tenant.SchemaName` (migración `TenantSchemaPerInstitution`).
- Cada esquema lleva su propio historial de migraciones.
- Nuevos identificadores reservados para instituciones: `public`, `academiadigital`, `finance-*` y `pg-*`.

### Corregido

- **El arranque ya no borra la base** si encuentra tablas sin historial de migraciones: registra un error crítico y se detiene. Antes ejecutaba `EnsureDeletedAsync`, que con la base compartida eliminaría todas las instituciones.

## [1.6.0] - 2026-11-15

Alta de usuarios controlada por la institución.

### Agregado

- **Sección "Aspirantes"** (menú del Admin): cuentas del registro público pendientes de alta, con filtro por rol (Alumnos/Profesores), búsqueda por nombre, email o DNI, fecha de registro y carrera. Botón "Dar de alta" por fila, casillas con "Seleccionar todos" y alta en masa. La pestaña "Dados de alta (últimos 7 días)" muestra las altas recientes.
- **Dominio de correo docente por institución** (`TeacherEmailDomain`): quien se registra con un email de ese dominio (en ITSC, `itscordoba.edu.ar`) queda como Profesor con su perfil docente, sin elegir carrera. Al darlo de alta, el perfil queda activo para asignarle materias y divisiones. Se configura al crear la institución en `/platform` y, para ITSC, con `Tenancy__DefaultTeacherEmailDomain`.
- API: `GET /api/v1/admin/applicants` y `POST /api/v1/admin/applicants/approve`. `GET /api/v1/tenant/current` informa el dominio docente.
- Clave de la página `/platform` en SSM (`Platform__AdminKey`, Terraform).

### Cambiado

- **El registro público ya no habilita la cuenta**: queda pendiente y el login responde 403 "Tu cuenta está pendiente de aprobación" hasta que un Admin la da de alta. Los usuarios existentes, los creados por un Admin, la plataforma, DevTools y los seeds quedan aprobados.
- Gestión de Usuarios muestra solo cuentas dadas de alta.
- El menú "Registros" pasa a llamarse "Registros Académicos".
- Migraciones: `AddUserRegistrationApproval` (base de cada institución) y `AddTenantTeacherEmailDomain` (base de control).

## [1.5.0] - 2026-11-15

### Agregado

- **Versión visible**: la barra lateral muestra al pie la versión publicada ("Versión 1.5.0"). Sale de `frontend-angular/src/environments/version.ts`, que `tools/version.sh` actualiza y verifica junto con el resto.
- **Acceso a Google Classroom** en el menú de usuario: abre `https://classroom.google.com/` en una pestaña nueva.

### Cambiado

- El menú de usuario ya no muestra "Mis Mensajes"; su lugar lo ocupa "Classroom". La ruta `/app/messages` sigue existiendo.

## [1.4.1] - 2026-11-15

### Corregido

- **El correo de recuperación no se enviaba en producción.** El rol de la instancia solo podía usar la identidad SES del dominio remitente; con la cuenta en sandbox, SES también exige permiso sobre la identidad del destinatario y respondía *AccessDenied* (el backend lo registraba y la pantalla igual decía "revisa tu correo"). El permiso `SendEmail` ahora cubre `identity/*` de la cuenta y región (Terraform, unidad `app`).
- **El encabezado seguía consultando `/api/v1/devtools/time` cada 30 segundos** (403 en producción). El arreglo de 1.4.0 comprobaba si el error era un `HttpErrorResponse`, pero el `ErrorInterceptor` lo reemplaza por un `ApiError`; ahora se lee el `status` del error. Con la función deshabilitada se hace una sola consulta por carga de página. Test nuevo: `time-travel.service.spec.ts`, con el interceptor real.

## [1.4.0] - 2026-11-15

Recuperación de contraseña por correo (Amazon SES).

### Agregado

- **Correo de recuperación**: el pedido de "¿Olvidaste tu contraseña?" envía un enlace a `/auth/reset-password?token=...` desde `no-reply@itsc.dryvn.org` por Amazon SES, con el rol de la instancia y sin claves. En desarrollo el correo se escribe en el log (`Email:Provider=Log`).
- El enlace usa el dominio de la institución que hizo el pedido (multitenant).
- Límite de 5 pedidos de recuperación por IP cada 15 minutos.

### Cambiado

- **El token de recuperación ya no vuelve en la respuesta** de `POST /api/v1/users/forgot-password`: la respuesta es siempre la misma, exista o no la cuenta. Si se informa el DNI, tiene que coincidir.
- El token queda atado a la institución y a la contraseña actual: no sirve en otra institución ni se puede usar dos veces. Al cambiar la contraseña se cierran las sesiones abiertas.
- Frontend: se eliminó el paso del código de 6 dígitos (el código válido era siempre `000000`); el enlace del correo abre directamente el formulario de contraseña nueva.

### Corregido

- Un token de recuperación inválido o vencido respondía 500; ahora responde 400.
- Al abrir el perfil, Admin y Profesor veían "El usuario actual no es un alumno.": la pantalla pedía la ficha de estudiante a cualquier rol. Ahora solo la pide para Alumno.
- El encabezado del panel consultaba `/api/v1/devtools/time` cada 30 segundos y en producción mostraba "No tenés permisos para realizar esta acción". Ahora, si la función está deshabilitada, consulta una sola vez y sin alerta.

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

[Sin publicar]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/sergioortiz17/AcademiaDigital/releases/tag/v1.0.0
