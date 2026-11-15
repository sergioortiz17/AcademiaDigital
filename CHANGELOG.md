# Changelog

Cambios relevantes de AcademiaDigital por versión. El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el número de versión, [versionado semántico](https://semver.org/lang/es/). Ver [VERSIONADO.md](VERSIONADO.md).

## [Sin publicar]

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

[Sin publicar]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/sergioortiz17/AcademiaDigital/releases/tag/v1.0.0
