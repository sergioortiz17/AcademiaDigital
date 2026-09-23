# Matriz de cobertura de API

**Fecha del inventario:** 2026-08-24

**Fuente:** Swagger `v1` ejecutado desde la imagen Docker E2E M11 y guards efectivos de los controllers.

Esta matriz registra las 196 operaciones HTTP expuestas actualmente. El permiso indicado es el que aplica el código, no el esquema Bearer global mostrado por Swagger.

Leyenda de cobertura:

- ✅: el endpoint tiene una aserción directa en API Regression.
- 🟨: se usa solamente como preparación o cleanup; no cuenta como cobertura contractual completa.
- ❌: no existe escenario automatizado de API.

## M1 — Usuarios y autenticación

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/admin/users` | Admin | ❌ | Pendiente suite Admin. |
| `PATCH /api/v1/admin/users/{id}/role` | Admin | ❌ | Pendiente suite Admin. |
| `PATCH /api/v1/admin/users/{id}/active` | Admin | ❌ | Pendiente suite Admin. |
| `DELETE /api/v1/admin/users/{id}` | Admin | ❌ | Pendiente suite Admin. |
| `POST /api/v1/users/login` | Público | ✅ | `authentication.spec.ts` y setup de fixtures. |
| `POST /api/v1/users/register` | Público | ✅ | Enrollment, multi-career y validaciones. |
| `POST /api/v1/users/logout` | Sesión | ✅ | `authentication.spec.ts`. |
| `POST /api/v1/users/checkSession` | Sesión | ✅ | `authentication.spec.ts`. |
| `PUT /api/v1/users/change-password` | Sesión | ❌ | Sin escenario. |
| `POST /api/v1/users/forgot-password` | Público | ❌ | Sin escenario. |
| `POST /api/v1/users/reset-password` | Público | ❌ | Sin escenario. |
| `GET /api/v1/users/profile` | Sesión/propietario | ✅ | `authentication.spec.ts`. |
| `PUT /api/v1/users/profile` | Sesión/propietario | ❌ | Sin escenario. |
| `POST /api/v1/users/edit` | Sesión | ❌ | Contrato heredado sin escenario. |

## M2 — Catálogo académico

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/careers` | Público sin guard | ✅ | Smoke y escenarios académicos. |
| `POST /api/v1/careers` | Público sin guard | ✅ | Setup académico con aserción. |
| `GET /api/v1/careers/{id}` | Público sin guard | ✅ | Smoke 404 y consultas. |
| `PUT /api/v1/careers/{id}` | Público sin guard | ❌ | Sin escenario. |
| `DELETE /api/v1/careers/{id}` | Público sin guard | 🟨 | Cleanup best-effort. |
| `GET /api/v1/careers/{careerId}/courses` | Público sin guard | ✅ | `academic-student-assignment.spec.ts`. |
| `POST /api/v1/careers/{careerId}/courses` | Público sin guard | ✅ | Setup académico con aserción. |
| `PUT /api/v1/careers/{careerId}/courses/{courseId}` | Público sin guard | ❌ | Sin escenario. |
| `DELETE /api/v1/careers/{careerId}/courses/{courseId}` | Público sin guard | 🟨 | Cleanup best-effort. |
| `GET /api/v1/careers/{careerId}/study-plans` | Público sin guard | ✅ | Flujo académico. |
| `POST /api/v1/careers/{careerId}/study-plans` | Público sin guard | ✅ | Setup académico con aserción. |
| `GET /api/v1/careers/{careerId}/study-plans/{studyPlanId}/courses-grouped` | Público sin guard | ✅ | Flujo académico. |
| `PUT /api/v1/careers/{careerId}/study-plans/{studyPlanId}` | Público sin guard | ❌ | Sin escenario. |
| `POST /api/v1/careers/{careerId}/study-plans/{studyPlanId}/activate` | Público sin guard | ✅ | Setup académico con aserción. |
| `GET /api/v1/study-plans/{studyPlanId}/courses` | Público sin guard | ✅ | Flujo académico. |
| `POST /api/v1/study-plans/{studyPlanId}/courses` | Público sin guard | ✅ | Setup académico con aserción. |
| `PUT /api/v1/study-plans/{studyPlanId}/courses/{studyPlanCourseId}` | Público sin guard | ❌ | Sin escenario. |
| `DELETE /api/v1/study-plans/{studyPlanId}/courses/{studyPlanCourseId}` | Público sin guard | 🟨 | Cleanup best-effort. |
| `GET /api/v1/study-plans/{studyPlanId}/courses/{courseId}/prerequisites` | Público sin guard | ❌ | Sin escenario API; regla sí cubierta en Domain. |
| `POST /api/v1/study-plans/{studyPlanId}/courses/{courseId}/prerequisites` | Público sin guard | ✅ | Setup con aserción y bloqueo efectivo de inscripción por correlativa estricta en `enrollment.spec.ts`. |
| `DELETE /api/v1/study-plans/{studyPlanId}/courses/{courseId}/prerequisites/{prerequisiteCourseId}` | Público sin guard | ❌ | Sin escenario. |

## M3 — Gestión estudiantil

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/careers/{careerId}/commissions` | Admin | ✅ | Flujo académico. |
| `POST /api/v1/careers/{careerId}/commissions` | Admin | ✅ | Setup académico con aserción. |
| `GET /api/v1/careers/{careerId}/commissions/{id}` | Admin | ❌ | Cliente disponible, sin aserción vigente. |
| `PUT /api/v1/careers/{careerId}/commissions/{id}` | Admin | ❌ | Sin escenario. |
| `DELETE /api/v1/careers/{careerId}/commissions/{id}` | Admin | 🟨 | Cleanup best-effort. |
| `GET /api/v1/students` | Admin | ✅ | Smoke, filtros y autorización. |
| `POST /api/v1/students` | Admin | ✅ | Alta, validaciones, atomicidad y concurrencia. |
| `GET /api/v1/students/{studentId}` | Admin/propietario | ✅ | Flujos e aislamiento. |
| `PUT /api/v1/students/{studentId}` | Admin | ❌ | Sin escenario. |
| `DELETE /api/v1/students/{studentId}` | Admin | 🟨 | Cleanup best-effort. |
| `GET /api/v1/students/{studentId}/careers` | Admin/propietario | ✅ | Multi-career e aislamiento. |
| `POST /api/v1/students/{studentId}/careers` | Admin | ✅ | Multi-career y documentos. |
| `PATCH /api/v1/students/{studentId}/status` | Admin | ❌ | Sin cliente ni escenario. |
| `GET /api/v1/students/{studentId}/status-history` | Admin/propietario | ❌ | Sin cliente ni escenario. |
| `GET /api/v1/students/{studentId}/record` | Admin/propietario | ✅ | Flujos de legajo y becas. |
| `POST /api/v1/students/{studentId}/academic-assignments` | Admin | ✅ | Asignación y multi-career. |
| `GET /api/v1/students/{studentId}/academic-assignments` | Admin/propietario | ✅ | Asignación y multi-career. |
| `GET /api/v1/students/{studentId}/documents` | Admin/propietario | ✅ | `documents.spec.ts` y autorización P1. |
| `POST /api/v1/students/{studentId}/documents` | Admin | ✅ | `documents.spec.ts` y autorización P1. |
| `GET /api/v1/students/{studentId}/documents/{documentId}` | Admin/propietario | ✅ | Ciclo documental. |
| `DELETE /api/v1/students/{studentId}/documents/{documentId}` | Admin | ✅ | Ciclo documental. |
| `PATCH /api/v1/students/{studentId}/documents/{documentId}/status` | Admin | ✅ | Ciclo documental. |
| `GET /api/v1/students/{studentId}/pending-documents` | Admin/propietario | ✅ | Documentos y autorización P1. |
| `GET /api/v1/students/{studentId}/scholarships` | Admin/propietario | ✅ | Becas y autorización P1. |
| `POST /api/v1/students/{studentId}/scholarships` | Admin | ✅ | Ciclo de becas y autorización. |
| `PUT /api/v1/students/{studentId}/scholarships/{id}` | Admin | ✅ | Ciclo de becas. |
| `DELETE /api/v1/students/{studentId}/scholarships/{id}` | Admin | ✅ | Ciclo de becas. |
| `GET /api/v1/students/{studentId}/custom-values` | Admin/propietario | ✅ | Campos personalizados y aislamiento. |
| `PUT /api/v1/students/{studentId}/custom-values` | Admin | ✅ | Campos personalizados y atomicidad. |
| `GET /api/v1/students/{studentId}/academic-history` | Admin/propietario | ✅ | Enrollment. |
| `GET /api/v1/students/{studentId}/eligible-courses` | Admin/propietario | ✅ | Flujo académico, multi-career y autorización. |
| `GET /api/v1/students/{studentId}/academic-progress` | Admin/propietario | ✅ | Flujo académico y multi-career. |
| `POST /api/v1/students/{studentId}/study-plan` | Admin | ✅ | Flujo académico y enrollment. |
| `GET /api/v1/document-requirements` | Admin | ✅ | Documentos y autorización P1. |
| `POST /api/v1/document-requirements` | Admin | ✅ | Ciclo documental. |
| `GET /api/v1/document-requirements/{id}` | Admin | ✅ | Ciclo documental y 404. |
| `PUT /api/v1/document-requirements/{id}` | Admin | ✅ | Ciclo documental. |
| `DELETE /api/v1/document-requirements/{id}` | Admin | ✅ | Ciclo documental. |
| `GET /api/v1/scholarships` | Admin | ✅ | Becas y autorización P1. |
| `POST /api/v1/scholarships` | Admin | ✅ | Ciclo de becas. |
| `GET /api/v1/scholarships/{id}` | Admin | ✅ | Ciclo de becas. |
| `PUT /api/v1/scholarships/{id}` | Admin | ✅ | Ciclo de becas. |
| `DELETE /api/v1/scholarships/{id}` | Admin | ✅ | Ciclo de becas. |
| `GET /api/v1/student-custom-fields` | Admin | ✅ | Campos personalizados y autorización P1. |
| `POST /api/v1/student-custom-fields` | Admin | ✅ | Campos personalizados. |
| `GET /api/v1/student-custom-fields/{id}` | Admin | ✅ | Campos personalizados. |
| `PUT /api/v1/student-custom-fields/{id}` | Admin | ✅ | Campos personalizados. |
| `DELETE /api/v1/student-custom-fields/{id}` | Admin | ✅ | Campos personalizados. |

## M4 — Inscripciones y admisión

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/admissions/forms/{slug}` | Público | ✅ | `admissions.spec.ts` — `@m4`. |
| `POST /api/v1/admissions/applications` | Público + antiabuso | ✅ | Alta, términos, duplicado, desafío 403 y rate limit 429/`Retry-After` — `@m4`. |
| `GET /api/v1/admissions/forms` | Admin | ✅ | Lista y permisos 401/403 — `@m4`. |
| `POST /api/v1/admissions/forms` | Admin | ✅ | Alta configurable, destino comisión/turno, unicidad del pool y publicación inmediata — `@m4`. |
| `PATCH /api/v1/admissions/forms/{formId}/active` | Admin | ✅ | Desactivación y posterior 404 público — `@m4`. |
| `PATCH /api/v1/admissions/forms/{formId}/capacity` | Admin | ✅ | Ampliación, promoción y rechazo de reducción bajo ocupación — `@m4`. |
| `GET /api/v1/admissions/applications` | Admin | ✅ | Filtros por formulario, estado y búsqueda — `@m4`. |
| `GET /api/v1/admissions/applications/{publicId}` | Admin | ✅ | Detalle, campos e historial append-only — `@m4`. |
| `PATCH /api/v1/admissions/applications/{publicId}/status` | Admin | ✅ | Transiciones, actor, motivo y conflicto terminal — `@m4`. |
| `GET /api/v1/admissions/applications/{publicId}/documents` | Admin | ✅ | Listado y permisos 401/403 — `@m4`. |
| `POST /api/v1/admissions/applications/{publicId}/documents` | Admin | ✅ | Presentación versionada y contrato Zod — `@m4`. |
| `PATCH /api/v1/admissions/applications/{publicId}/documents/{documentId}/review` | Admin | ✅ | Revisión, actor y gate de confirmación — `@m4`. |
| `GET /api/v1/admissions/applications/{publicId}/agreement` | Admin | ✅ | Estado Pending/Ready, integridad y permisos 401/403 — `@m4`. |
| `GET /api/v1/admissions/applications/{publicId}/agreement/download` | Admin | ✅ | Bloqueo previo y descarga PDF real — `@m4`. |
| `POST /api/v1/admissions/outbox/process` | Admin | ✅ | Procesamiento durable, permisos 401/403 y resultado — `@m4`. |
| `POST /api/v1/admissions/applications/process-expirations` | Admin | ✅ | Expiración y promoción FIFO auditada — `@m4`. |
| `POST /api/v1/students/{studentId}/rematriculations` | Admin | ✅ | Año siguiente, permisos 401/403, concurrencia, historial y asignación vigente — `@m4`. |
| `GET /api/v1/enrollments/periods` | Admin | ✅ | Listado y permisos 401/403 — `enrollment-periods-admin.spec.ts`. |
| `POST /api/v1/enrollments/periods` | Admin | ✅ | Alta y permisos 401/403 — `@m4`. |
| `GET /api/v1/enrollments/periods/active` | Cualquier sesión | ✅ | Anónimo 401 y consulta de alumno — `@m4`. |
| `GET /api/v1/enrollments/periods/{id}/students` | Admin | ✅ | Resultado funcional y permisos 401/403 — `@m4`. |
| `PUT /api/v1/enrollments/periods/{id}/quotas` | Admin | ✅ | 401/403, ampliación y rechazo bajo ocupación — `@m4`. |
| `PUT /api/v1/enrollments/periods/{id}/close` | Admin | ✅ | Cierre funcional y permisos 401/403 — `@m4`. |
| `PUT /api/v1/enrollments/periods/{id}/activate` | Admin | ✅ | Permisos 401/403 — `@m4`. |
| `DELETE /api/v1/enrollments/periods/{id}` | Admin | ✅ | Permisos 401/403 y cleanup best-effort — `@m4`. |
| `GET /api/v1/enrollments/periods/{id}/report` | Admin | ✅ | Permisos 401/403 — `@m4`. |
| `DELETE /api/v1/enrollments/periods/{id}/students/{studentId}` | Admin | ✅ | Permisos 401/403 y cleanup best-effort — `@m4`. |
| `DELETE /api/v1/enrollments/my/{periodId}` | Sesión/propietario implícito | ❌ | Sin escenario. |
| `GET /api/v1/enrollments/my` | Sesión/propietario implícito | ✅ | Enrollment. |
| `POST /api/v1/enrollments` | Sesión/alumno implícito | ✅ | Enrollment, plan vigente, correlativa estricta, cupo serializable, duplicado, período cerrado y multi-career. |

## M5 — Docentes y legajo

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/teachers` | Admin | ✅ | Lista activa/histórica y permisos 401/403 — `teachers.spec.ts`, `@m5`. |
| `GET /api/v1/teachers/{id}` | Admin | ✅ | Consulta y contrato Zod — `@m5`. |
| `POST /api/v1/teachers` | Admin | ✅ | Alta, rol Profesor y conflictos por usuario/número — `@m5`. |
| `PUT /api/v1/teachers/{id}` | Admin | ✅ | Actualización de legajo, domicilio y emergencia — `@m5`. |
| `DELETE /api/v1/teachers/{id}` | Admin | ✅ | Baja lógica idempotente con fecha, actor y motivo — `@m5`. |
| `GET /api/v1/teachers/{id}/documents` | Admin | ✅ | Listado de versiones, permisos 401/403 y contrato Zod — `teacher-documents.spec.ts`, `@m5`. |
| `POST /api/v1/teachers/{id}/documents` | Admin | ✅ | Normalización de tipo, nueva versión y expiración de la anterior — `@m5`. |
| `PATCH /api/v1/teachers/{id}/documents/{documentId}/review` | Admin | ✅ | Aprobación/rechazo auditado y observación obligatoria al rechazar — `@m5`. |
| `GET /api/v1/teachers/{id}/assignments` | Admin | ✅ | Designaciones vigentes/históricas y permisos 401/403 — `teacher-assignments.spec.ts`, `@m5`. |
| `POST /api/v1/teachers/{id}/assignments` | Admin | ✅ | Ocupación serializable, auditoría y conflicto de cargo ocupado — `@m5`. |
| `DELETE /api/v1/teachers/{id}/assignments/{assignmentId}` | Admin | ✅ | Finalización append-only y liberación de vacante — `@m5`. |
| `GET /api/v1/teachers/me/assignments` | Profesor propietario | ✅ | Resolución por usuario autenticado, historial opcional y aislamiento entre profesores — `@m5`. |
| `GET /api/v1/teaching-positions` | Admin | ✅ | Filtros, vacantes, histórico y permisos 401/403 — `@m5`. |
| `GET /api/v1/teaching-positions/{id}` | Admin | ✅ | Estado vigente del cargo y docente asignado — `@m5`. |
| `POST /api/v1/teaching-positions` | Admin | ✅ | Alta y validación materia/comisión/ciclo — `@m5`. |
| `PUT /api/v1/teaching-positions/{id}` | Admin | ✅ | Actualización sólo sin ocupación ni historial — `@m5`. |
| `DELETE /api/v1/teaching-positions/{id}` | Admin | ✅ | Baja lógica auditada exclusivamente sobre vacante — `@m5`. |

## M6 — Asistencias

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/attendance/sessions` | Admin o Profesor; Profesor filtrado por asignación | ✅ | 401/403, aislamiento entre profesores y filtros — `attendance.spec.ts`, `@m6`. |
| `POST /api/v1/attendance/sessions` | Admin o Profesor asignado | ✅ | Idempotencia por header, relación cargo/oferta, hora cátedra y día completo — `@m6`. |
| `GET /api/v1/attendance/sessions/{id}` | Admin o Profesor asignado | ✅ | Roster de comisión, contrato Zod y aislamiento docente — `@m6`. |
| `PUT /api/v1/attendance/sessions/{id}/records` | Admin o Profesor asignado | ✅ | Upsert masivo idempotente, pertenencia al roster, bloqueo por cierre/48 horas y reapertura — `@m6`. |
| `POST /api/v1/attendance/sessions/{id}/close` | Admin o Profesor asignado | ✅ | Cierre auditado y rechazo de edición posterior — `@m6`. |
| `POST /api/v1/attendance/sessions/{id}/reopen` | Admin | ✅ | 403 para Profesor y reapertura retroactiva con motivo/auditoría — `@m6`. |
| `POST /api/v1/attendance/records/{recordId}/justifications` | Admin | ✅ | Justificación append-only, evidencia lógica y exclusión del denominador — `@m6`. |
| `GET /api/v1/attendance/students/{studentId}/summary` | Admin o Profesor asignado | ✅ | Cálculo por materia/comisión, riesgo e aislamiento entre profesores — `@m6`. |
| `GET /api/v1/attendance/me/summary` | Alumno propietario implícito | ✅ | 403 para Profesor, identidad derivada de sesión y porcentaje/riesgo — `@m6`. |
| `GET /api/v1/attendance/sessions/{id}/export` | Admin o Profesor asignado | ✅ | CSV UTF-8 con BOM y PDF válido — `@m6`. |

## M7 — Calificaciones, notas y mesas de examen

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/gradebooks` | Admin o Profesor; Profesor filtrado por cargo vigente | ✅ | 401/403, listado e aislamiento docente — `grades.spec.ts`, `@m7`. |
| `POST /api/v1/gradebooks` | Admin o Profesor asignado | ✅ | Idempotencia, oferta natural, pesos al 100% y rechazo fuera del cargo — `@m7`. |
| `GET /api/v1/gradebooks/{id}` | Admin o Profesor asignado | ✅ | Roster, evaluaciones, promedio y aislamiento — `@m7`. |
| `PUT /api/v1/gradebooks/{id}/grades` | Admin o Profesor asignado; sólo `Draft` | ✅ | Carga masiva, roster, revisiones versionadas y bloqueo posterior al envío — `@m7`. |
| `POST /api/v1/gradebooks/{id}/submit` | Admin o Profesor asignado | ✅ | Completitud de planilla y transición `Draft → Submitted` — `@m7`. |
| `POST /api/v1/gradebooks/{id}/approve` | Admin | ✅ | 403 docente y transición de Secretaría — `@m7`. |
| `POST /api/v1/gradebooks/{id}/publish` | Admin | ✅ | Publicación y habilitación de visibilidad al alumno — `@m7`. |
| `POST /api/v1/gradebooks/{id}/close` | Admin | ✅ | Promedio ponderado y actualización transaccional de `Enrollment` — `@m7`. |
| `POST /api/v1/gradebooks/{id}/reopen` | Admin | ✅ | 403 docente, motivo, auditoría y nueva versión de nota — `@m7`. |
| `GET /api/v1/gradebooks/me` | Alumno propietario implícito | ✅ | Ocultamiento previo a publicación y sólo notas propias — `@m7`. |
| `GET /api/v1/exam-tables` | Admin o Profesor integrante del tribunal | ✅ | Listado filtrado por tribunal — `@m7`. |
| `POST /api/v1/exam-tables` | Admin | ✅ | Idempotencia, fecha/llamado, presidente, vocal y permisos 401/403 — `@m7`. |
| `GET /api/v1/exam-tables/{id}` | Admin o Profesor integrante del tribunal | ✅ | Detalle de tribunal, inscripciones y aislamiento del alumno — `@m7`. |
| `POST /api/v1/exam-tables/{id}/registrations` | Admin o Alumno propietario | ✅ | Regularidad, inscripción propia, idempotencia y numeración de intento — `@m7`. |
| `POST /api/v1/exam-tables/{id}/start-grading` | Admin | ✅ | Cierre de inscripción e inicio del acta — `@m7`. |
| `PUT /api/v1/exam-tables/{id}/results` | Admin o Profesor integrante del tribunal | ✅ | Umbral final, historial versionado y permisos 403 — `@m7`. |
| `POST /api/v1/exam-tables/{id}/publish` | Admin | ✅ | Publicación de acta y aprobación académica atómica — `@m7`. |
| `POST /api/v1/exam-tables/{id}/reopen` | Admin | ✅ | Reapertura auditada y rectificación compensatoria — `@m7`. |
| `GET /api/v1/exam-tables/me` | Alumno propietario implícito | ✅ | Mesas elegibles/inscriptas y resultado oculto hasta publicación — `@m7`. |

## M8 — Certificados y constancias

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/certificates/my` | Sesión | ✅ | 401 y consulta aislada de solicitudes — `certificates.spec.ts`, `@m8`. |
| `POST /api/v1/certificates/request` | Alumno | ✅ | Tipos legacy/canónicos, elegibilidad, carrera propia y duplicado activo — `@m8`. |
| `GET /api/v1/certificates/all` | Admin | ✅ | 403 Alumno, búsqueda y estados persistidos — `@m8`. |
| `POST /api/v1/certificates/{id}/approve` | Admin | ✅ | 403 Alumno, auditoría y transición `Pending → Approved` — `@m8`. |
| `POST /api/v1/certificates/{id}/reject` | Admin | ✅ | Motivo obligatorio, 403 Alumno y estado terminal — `@m8`. |
| `POST /api/v1/certificates/{id}/issue` | Admin | ✅ | Emisión concurrente, correlativos consecutivos e idempotencia — `@m8`. |
| `GET /api/v1/certificates/issued/me` | Alumno propietario implícito | ✅ | Historial emitido aislado por sesión — `@m8`. |
| `GET /api/v1/certificates/students/{studentId}/history` | Admin | ✅ | Historial por alumno consistente con la consulta propia — `@m8`. |
| `GET /api/v1/certificates/issued/{publicId}/download` | Admin o Alumno propietario | ✅ | PDF, SHA-256, descarga Admin/propietario y 403 cruzado — `@m8`. |

## M9 — Cobros y conceptos

Las 13 operaciones tienen aserciones directas en `finance.spec.ts`. El escenario `@m9` pasó 1/1 y la regresión completa pasó 32/32 sobre SQL Server Docker el 2026-08-24.

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/finance/concepts` | Admin | ✅ | 401/403, listado y normalización — `finance.spec.ts`, `@m9`. |
| `POST /api/v1/finance/concepts` | Admin | ✅ | Alta y rechazo de código duplicado — `@m9`. |
| `PUT /api/v1/finance/concepts/{id}` | Admin | ✅ | Actualización conservando identidad — `@m9`. |
| `GET /api/v1/finance/rates` | Admin | ✅ | Filtros por carrera/ciclo y contrato Zod — `@m9`. |
| `POST /api/v1/finance/rates` | Admin | ✅ | Tarifa general ARS y duplicado — `@m9`. |
| `PUT /api/v1/finance/rates/{id}` | Admin | ✅ | Cambio posterior sin alterar deuda emitida — `@m9`. |
| `GET /api/v1/finance/benefits` | Admin | ✅ | Listado de descuentos/becas — `@m9`. |
| `POST /api/v1/finance/benefits` | Admin | ✅ | Validación de beca y beneficio más favorable — `@m9`. |
| `GET /api/v1/finance/plans` | Admin | ✅ | Listado filtrado y esquema de ítems — `@m9`. |
| `POST /api/v1/finance/plans` | Admin | ✅ | Plan, cuota, vencimiento y recargo — `@m9`. |
| `POST /api/v1/finance/plans/{id}/generate` | Admin + `Idempotency-Key` | ✅ | 400 sin clave, 403, concurrencia, reintento y clave alternativa — `@m9`. |
| `GET /api/v1/finance/debts` | Admin | ✅ | Consulta por alumno, importes y snapshot — `@m9`. |
| `GET /api/v1/finance/debts/me` | Alumno propietario implícito | ✅ | 401, aislamiento entre alumnos y estabilidad histórica — `@m9`. |

## M10 — Pagos y medios

Las 7 operaciones tienen aserciones directas en `payments.spec.ts`. El escenario `@m10` pasó 1/1 y la regresión completa pasó 33/33 sobre SQL Server Docker el 2026-08-24.

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/finance/payment-methods` | Sesión | ✅ | 401, catálogo de cuatro medios y marca de conciliación — `payments.spec.ts`, `@m10`. |
| `POST /api/v1/payments` | Admin | ✅ | 403, DNI, parcial, multiconcepto, sobrepago y cuatro medios — `@m10`. |
| `POST /api/v1/payments/{publicId}/confirm` | Admin + `Idempotency-Key` | ✅ | 400 sin clave, concurrencia, reintento y conflicto por reutilización — `@m10`. |
| `POST /api/v1/payments/{publicId}/reconcile` | Admin | ✅ | Transferencia pendiente, aprobación, rechazo y nota obligatoria — `@m10`. |
| `POST /api/v1/payments/{publicId}/reverse` | Admin | ✅ | Restauración de saldos y reversión append-only idempotente — `@m10`. |
| `GET /api/v1/payments` | Admin | ✅ | Historial completo filtrado por alumno — `@m10`. |
| `GET /api/v1/payments/me` | Alumno propietario implícito | ✅ | 401, consulta propia e igualdad con historial Admin — `@m10`. |

## M11 — Recibos digitales

Las 5 operaciones tienen aserciones directas en `receipts.spec.ts`. El escenario `@m11` pasó 1/1 y la regresión completa pasó 34/34 sobre SQL Server Docker el 2026-08-24.

| Operación | Permiso efectivo | Cobertura | Evidencia |
|---|---|:---:|---|
| `GET /api/v1/receipts` | Admin | ✅ | 401/403, filtro por alumno, historial completo y correlativos globales — `receipts.spec.ts`, `@m11`. |
| `GET /api/v1/receipts/{publicId}` | Admin o Alumno propietario | ✅ | Contrato, snapshot, estado del pago y aislamiento cruzado — `@m11`. |
| `POST /api/v1/receipts/{publicId}/generate` | Admin | ✅ | 403 Alumno y reintento idempotente conservando número/hash — `@m11`. |
| `GET /api/v1/receipts/{publicId}/download` | Admin o Alumno propietario | ✅ | PDF real, SHA-256, descarga Admin/propietario y 403 cruzado — `@m11`. |
| `GET /api/v1/students/me/receipts` | Alumno propietario implícito | ✅ | 401, historial derivado de sesión y aislamiento entre alumnos — `@m11`. |

## Operaciones transversales o de módulos posteriores

| Operación | Módulo | Permiso efectivo | Cobertura | Evidencia |
|---|---|---|:---:|---|
| `GET /api/v1/calendar/events` | Calendario | Público sin guard | ❌ | Sin escenario. |
| `POST /api/v1/calendar/events` | Calendario | Admin; anónimo recibe 403 | ❌ | Sin escenario. |
| `DELETE /api/v1/calendar/events/{id}` | Calendario | Admin; anónimo recibe 403 | ❌ | Sin escenario. |

## Hallazgos y prioridades

1. Las 196 operaciones están inventariadas; 167 tienen aserción directa ejecutada, 5 sólo participan en cleanup/setup y 24 no tienen cobertura API directa.
2. Careers, Courses, StudyPlans, StudyPlanCourses y Prerequisites tienen escrituras públicas sin guard efectivo.
3. Las operaciones administrativas de EnrollmentPeriods exigen Admin y el alta pública de admisión combina challenge configurable con rate limiting por IP.
4. Swagger aplica Bearer global incluso a endpoints realmente públicos. La documentación visual no representa los guards efectivos.
5. La cobertura nueva debe actualizar esta matriz en la misma entrega; una operación no cuenta como cubierta por el solo hecho de existir en un cliente.

## Siguiente objetivo — estabilización y release

- Mantener M4–M11 dentro de la regresión obligatoria sin reabrir su alcance funcional.
- Priorizar los 24 endpoints sin aserción directa según criticidad y cerrar primero seguridad de escrituras académicas heredadas.
