# Evidencia M4 — admisión, cupos, correlativas, documentos, acuerdos y rematriculación

## Alcance

Primer incremento funcional de M4 sobre el entorno Docker E2E aislado:

- API `http://localhost:8010`.
- SQL Server `localhost:1434`.
- Base descartable `AcademiaDigitalE2E`.
- Migraciones M4 aplicadas hasta `20260822175416_AddAdmissionAgreementsAndOutbox`.

## Resultado

```text
Docker build API Release: OK
TypeScript typecheck: OK
Migration/backfill regression: OK
API M4: 7/7 passed
API Regression completa: 25/25 passed
Playwright duration: 20.5 s
Allure HTML: generado correctamente
```

La suite específica `test:api:m4` agrupa admisión, administración de períodos, inscripción a materias y rematriculación.

## Comportamiento validado

- Consulta anónima de formulario activo por `slug`.
- Orden y contrato Zod de campos configurables.
- Aceptación obligatoria de términos.
- Alta anónima de solicitud con estado `PreEnrolled`.
- Reserva configurable de 72 horas.
- Persistencia de una única solicitud por formulario/email/DNI.
- Desafío antiabuso ausente o inválido rechazado con `403` antes de consultar/persistir; token válido aceptado mediante el adaptador E2E `StaticToken`.
- Ráfaga pública limitada por IP con `429`, contrato de error y header `Retry-After`.
- Respuesta `409` frente a duplicados y `400` sin términos aceptados.
- Administración de formularios y solicitudes restringida a Admin, con 401 anónimo y 403 alumno.
- Listado paginado con filtro por formulario, estado y búsqueda por email/DNI.
- Detalle con campos enviados e historial append-only.
- Transiciones `PreEnrolled → Enrolled → Confirmed` auditadas con actor y motivo.
- Bloqueo de `Confirmed` cuando falta un requisito documental obligatorio o sólo está presentado.
- Alta, listado y revisión Admin de documentos propios de la postulación, con permisos 401/403 y contrato Zod.
- Confirmación habilitada únicamente después de aprobar todos los requisitos globales/de carrera vigentes.
- Creación atómica de un acuerdo `Pending` y un mensaje outbox deduplicado al confirmar la postulación.
- Consulta de metadatos y descarga del acuerdo restringidas a Admin, con permisos 401/403.
- Respuesta `409` al intentar descargar el acuerdo antes de procesar el outbox.
- Procesamiento reintentable del outbox, transición del acuerdo a `Ready` y persistencia de SHA-256.
- Descarga de un PDF real con `Content-Type: application/pdf` y encabezado `%PDF-1.4`.
- Respuesta `409` al intentar salir de un estado terminal.
- Desactivación administrativa y posterior `404` en la consulta pública.
- Dos altas concurrentes sobre una vacante producen una reserva y una espera.
- Espera sin vencimiento, expiración de la reserva y promoción FIFO auditada.
- Ampliación de capacidad promueve al siguiente candidato; reducción bajo ocupación responde `409`.
- Formulario dirigido opcionalmente a una comisión activa y compatible con la carrera.
- Exposición de código, nombre, ciclo, año y turno de la comisión en el contrato público.
- Rechazo `409` de un segundo formulario que intente dividir el cupo de la misma comisión.
- Cupos independientes para comisiones/turnos diferentes y compatibilidad de formularios generales preexistentes.
- Rematriculación Admin al año académico inmediato siguiente con permisos 401/403.
- Validación de estudiante, carrera, plan, comisión, ciclo y año de cursada.
- Cierre de la asignación anterior, conservación del plan vigente y una única asignación nueva.
- Dos rematriculaciones concurrentes producen una creación y un conflicto `409`, sin duplicar historial.
- El alta real de inscripción valida el plan vigente y rechaza una materia cuya correlativa estricta no fue aprobada.
- Las nueve operaciones administrativas de períodos responden 401 sin sesión y 403 para Alumno; sólo la consulta del período activo permanece disponible para cualquier sesión.
- El cupo por turno cuenta estudiantes distintos aunque cada inscripción genere varias filas de materias.
- La reducción de una cuota por debajo de su ocupación responde `409`.
- Dos alumnos concurrentes sobre una sola vacante restante producen exactamente una inscripción y un conflicto `409`.
- Ausencia de regresiones en los escenarios previos de la suite completa.

La base de desarrollo `AcademiaDigital`, la API `5073`, SQL Server `1433` y los cambios locales del frontend no fueron modificados.
