# Roadmap backend M4–M11

Estado del documento: aprobado para implementación.
Última actualización: 2026-08-24.

## Objetivo y alcance

Completar un MVP operativo del backend desde el módulo 4 hasta el módulo 11, preservando los contratos ya integrados con Angular y aumentando la automatización para poder corregir y refactorizar con rapidez.

Los módulos M1, M2 y M3 se consideran terminados y forman el baseline estable. No se reabrirán funcionalmente. Sólo se permitirán cambios aditivos cuando sean indispensables para módulos posteriores, sin romper rutas, payloads ni respuestas existentes.

Decisiones acordadas:

- Una persona trabajará con una dedicación menor a 10 horas semanales; las estimaciones usan 8 horas semanales como referencia.
- El objetivo es un MVP operativo, no el cumplimiento estricto de todos los requisitos productivos del documento original.
- Los endpoints actuales se mantienen porque ya están integrados con el frontend.
- Cada usuario conserva un único rol.
- Email, PDF y almacenamiento se implementarán mediante interfaces y adaptadores local-first reemplazables.
- Los recibos serán internos, no fiscales, con una única serie correlativa global.
- Los recargos se aplicarán antes del descuento o beca más beneficioso; los beneficios no se acumularán.

## Diagnóstico inicial

La estimación se obtuvo contrastando los requisitos del roadmap original con controllers, casos de uso, entidades, configuraciones EF, migraciones y pruebas existentes. Se puntuó cada requisito como completo, parcial o ausente/no verificado.

| Módulo | Avance aproximado | Diagnóstico |
|---|---:|---|
| M4 — Inscripciones y formulario web | 100% MVP | Períodos Admin, cupos concurrentes por turno/comisión, correlativas, admisión pública, espera FIFO, documentación, rematriculación, acuerdo PDF/outbox y protección antiabuso configurable están implementados y cubiertos. |
| M5 — Docentes y legajo | 100% MVP | Legajo/baja, documentos versionados, cargos/vacantes, asignaciones append-only y consulta propia aislada están implementados y cubiertos. |
| M6 — Asistencias | 100% MVP | Sesiones por hora/día, carga masiva idempotente, cierre/reapertura, justificaciones, riesgo, consultas propias y exportación CSV/PDF están implementados y cubiertos. |
| M7 — Calificaciones y notas | 100% MVP | Planillas ponderadas, historial versionado, workflow de Secretaría, cierre académico, mesas, tribunal, inscripciones, llamados y actas rectificables están implementados y cubiertos. |
| M8 — Certificados | 100% MVP | Solicitudes tipadas, elegibilidad académica, revisión, correlativo concurrente, snapshot, PDF, hash, historial y descarga autorizada están implementados y cubiertos. |
| M9 — Cobros y conceptos | 100% MVP | Dominio, API, persistencia, migración, unitarios, escenario `@m9` y regresión integrada sobre SQL Server están implementados y cubiertos. |
| M10 — Pagos y medios | 100% MVP | Efectivo, transferencia, débito y crédito; pagos parciales/multiconcepto, conciliación, idempotencia, reversión append-only, historial y aislamiento están implementados y cubiertos. |
| M11 — Recibos digitales | 100% MVP | Emisión automática, correlativo global concurrente, snapshot inmutable, PDF/SHA-256, historial, descarga autorizada y backup conjunto SQL/archivos están implementados y cubiertos. |

El bloque M4–M11 está al **100% del MVP**. Considerando M1–M3 aceptados como completos, el avance global M1–M11 también es **100%**. El trabajo funcional del roadmap queda cerrado y continúa la Etapa 8 de estabilización y release.

La suite `tests/api-regression` tiene 34 escenarios y pasó 34/34 el 2026-08-24; M4 ejecuta 7/7, M5 3/3 y M6–M11 1/1 cada uno. La regresión de migraciones M1–M11 y la generación Allure también pasaron.

## Cronograma por etapas

| Etapa | Entrega | Estimación a 8 h/semana |
|---|---|---:|
| 0 | Preparación de automatización M4–M11 | 2–3 semanas |
| 1 | M4 — Inscripciones y admisión | 8–12 semanas |
| 2 | M5 — Docentes | 6–8 semanas |
| 3 | M6 — Asistencias | 5–7 semanas |
| 4 | M7 — Calificaciones | 8–10 semanas |
| 5 | M8 — Certificados | 5–7 semanas |
| 6 | M9 — Cobros y conceptos | 7–9 semanas |
| 7 | M10 — Pagos + M11 — Recibos | 10–14 semanas |
| 8 | Estabilización y release | 3–4 semanas |

Estimación total: **54–74 semanas**, aproximadamente 13–18 meses con la dedicación indicada. El avance se controlará mediante gates verificables y no sólo por fechas.

## Etapa 0 — Base de automatización

- Ejecutar y fijar como baseline la regresión actual.
- Crear una matriz que relacione cada operación Swagger con módulo, permisos y pruebas existentes.
- Agregar comandos `test:api:m4` hasta `test:api:m11`.
- Mantener el gate ArchUnitNET de dependencias, controllers y ciclos como segunda validación del backend.
- Incorporar proyectos xUnit para reglas rápidas de Domain y Application.
- Preparar fixtures Playwright aislados por worker para permitir paralelismo sin conflictos de sesión o cleanup.
- Mantener M1–M3 como regresión obligatoria, sin ampliar su alcance funcional.

Avance de Etapa 0 al 2026-08-24:

| Entrega | Estado | Evidencia |
|---|---|---|
| Baseline de regresión | Completado | El baseline inicial fue 19/19; la suite ampliada pasa 34/34 con M4–M11 completos y genera Allure. |
| Matriz Swagger–módulo–permiso–test | Completado | [API_COVERAGE_MATRIX.md](API_COVERAGE_MATRIX.md) inventaría las 196 operaciones del Swagger Docker M11. |
| Comandos `test:api:m4`–`test:api:m11` | Completado | Todos seleccionan slices efectivos y fueron ejecutados; `test:api:m11` cubre el flujo de recibos. |
| Gate ArchUnitNET | Completado | 8/8 reglas y workflow independiente de PR. |
| Proyectos xUnit Domain/Application | Base ampliada | 194 tests Domain + 97 tests Application, incluyendo reglas y handlers MVP de M4–M11. |
| Fixtures Playwright paralelizables | Pendiente | La suite continúa con un worker para evitar conflictos. |
| Regresión M1–M3 | Completado | Permanece incluida dentro de los 34 escenarios con última ejecución completa. |

Detalle de la ejecución: [baseline-2026-08-22.md](../tests/api-regression/analysis/baseline-2026-08-22.md).

Evidencia del incremento M4: [m4-admissions-2026-08-22.md](../tests/api-regression/analysis/m4-admissions-2026-08-22.md).

Primer incremento funcional M4 al 2026-08-22:

- `GET /api/v1/admissions/forms/{slug}` público.
- `POST /api/v1/admissions/applications` público e idempotente por formulario/email/DNI.
- Formulario configurable por campos requeridos y aceptación de términos.
- Solicitud `PreEnrolled` con reserva de 72 horas configurable y tiempo determinista.
- Índices únicos, `rowversion` y migración `AddAdmissionFormsAndApplications`.
- 15 unit tests nuevos y un escenario Playwright `@m4`.

Segundo incremento funcional M4 al 2026-08-22:

- Administración Admin de formularios: listado, alta y activación/desactivación.
- Administración Admin de solicitudes: listado paginado con filtros y consulta de detalle.
- Transiciones de estado validadas por política de dominio, con estados terminales protegidos.
- Historial append-only con fecha, actor y motivo; backfill para solicitudes preexistentes.
- Concurrencia optimista mediante `rowversion` y respuesta `409` ante conflictos.
- Migración `AddAdmissionApplicationStatusHistory` y seis operaciones Swagger nuevas.
- Permisos 401/403 y flujo administrativo cubiertos en Playwright; M4 3/3 y regresión total 21/21.

Tercer incremento funcional M4 al 2026-08-22:

- Capacidad opcional por formulario; `null` mantiene compatibilidad como cupo ilimitado.
- Asignación serializable de la última vacante con lock `UPDLOCK/HOLDLOCK` por formulario.
- Alta pública como `PreEnrolled` cuando hay cupo o `Waitlisted` sin vencimiento cuando está completo.
- Expiración de reservas `PreEnrolled`/`Enrolled` y promoción FIFO por `CreatedAt + Id`.
- Reconciliación al crear, liberar o ampliar capacidad y endpoint Admin para barrido explícito.
- Constraint SQL de capacidad, índice de cola y migración `AddAdmissionCapacityAndWaitlist`.
- Concurrencia, expiración, FIFO y reducción inválida cubiertas en Playwright; M4 4/4 y regresión total 22/22.

Cuarto incremento funcional M4 al 2026-08-22:

- Destino académico opcional del formulario mediante `CommissionId`; los formularios existentes conservan `null` como destino general.
- La comisión aporta carrera, ciclo, año y turno, y debe estar activa y pertenecer a la carrera del formulario.
- Los formularios dirigidos requieren y conservan capacidad explícita; una comisión sólo puede tener un formulario, evitando dividir o volver ilimitado su pool de vacantes.
- La consulta pública oculta formularios cuya comisión fue desactivada.
- Migración `LinkAdmissionFormsToCommissions` con FK restrictiva e índice único filtrado por `commission_id`.
- Reglas de dominio, handler y contrato Zod cubiertos; M4 5/5, migración/backfill y regresión total 23/23.

Quinto incremento funcional M4 al 2026-08-22:

- `POST /api/v1/students/{id}/rematriculations` restringido a Admin.
- Renovación únicamente al año académico inmediato siguiente para estudiantes activos no graduados ni retirados.
- Validación de membresía `StudentCareer`, plan activo y comisión compatible con carrera, ciclo y año de cursada.
- Cierre de la asignación académica vigente, alta de la nueva y conservación o reemplazo auditado del plan de estudio.
- Historial append-only con actor, fecha y notas, protegido por transacción serializable e índice único estudiante-carrera-ciclo.
- Migración `AddStudentRematriculations`; concurrencia, permisos 401/403 y persistencia cubiertos. M4 6/6 y regresión total 24/24.

Sexto incremento funcional M4 al 2026-08-22:

- El alta real de inscripciones valida el plan vigente, evita reinscribir materias aprobadas/en curso y bloquea correlativas `Strict`; las `Soft` permanecen como advertencia.
- Las postulaciones poseen documentos versionados por requisito, con alta, listado y revisión Admin; una nueva versión expira la versión presentada/aprobada anterior.
- La transición a `Confirmed` exige todos los requisitos obligatorios globales o de la carrera, vigentes a la fecha y con una versión aprobada.
- Migración `AddAdmissionApplicationDocuments` con tres FKs e índice de ciclo documental.
- 129/129 unit tests, migración/backfill, M4 6/6, regresión 24/24 y ArchUnitNET 8/8.

Séptimo incremento funcional M4 al 2026-08-22:

- La transición a `Confirmed` persiste atómicamente un snapshot inmutable del acuerdo y un evento outbox con clave única de deduplicación.
- Procesamiento Admin reintentable: genera un PDF local válido, calcula SHA-256, almacena por clave lógica y emite una notificación local idempotente reemplazable por email.
- Consulta de estado y descarga del acuerdo PDF restringidas a Admin; antes del procesamiento la descarga responde `409`.
- Migración `AddAdmissionAgreementsAndOutbox`, con unicidad por postulación/número de acuerdo y deduplicación del mensaje.
- 135/135 unit tests, migración/backfill, M4 6/6, regresión 24/24 y ArchUnitNET 8/8.

Octavo incremento funcional M4 al 2026-08-22:

- Las nueve operaciones administrativas de `EnrollmentPeriods` exigen Admin y responden 401/403 para anónimo/alumno; la consulta del período activo permanece disponible para cualquier sesión.
- La inscripción bloquea el período con `UPDLOCK/HOLDLOCK`, cuenta estudiantes distintos por turno y asigna la última vacante dentro de una transacción serializable.
- La actualización de cupos usa el mismo lock y rechaza con `409` cualquier reducción por debajo de la ocupación actual.
- Una carrera de dos alumnos por una única vacante produce exactamente una creación y un conflicto, sin sobreventa.
- 146/146 unit tests, migración/backfill, M4 7/7, regresión 25/25 y ArchUnitNET 8/8.

Noveno incremento funcional y cierre MVP M4 al 2026-08-22:

- `POST /api/v1/admissions/applications` aplica un fixed-window rate limiter por IP, devuelve `429` y publica `Retry-After` sin confiar en headers de IP enviados por el cliente.
- `challengeToken` amplía el payload sin romper consumidores y se verifica antes de consultar o persistir mediante `IAdmissionChallengeVerifier`.
- El adaptador soporta `Disabled`, `StaticToken` para entornos controlados y Cloudflare Turnstile mediante HTTPS; fallos o timeouts externos cierran el acceso.
- La configuración inválida —modo desconocido, secreto ausente o URL no HTTPS— hace fallar el arranque; producción debe inyectar el secreto mediante variables de entorno.
- E2E verifica 403 para desafío ausente/inválido y una ráfaga real con 429; 147/147 unit tests, M4 7/7, regresión 25/25 y ArchUnitNET 8/8.

## Etapa 1 — M4: inscripciones y formulario web — MVP completado

Los endpoints actuales de `/api/v1/enrollments` se conservarán para la inscripción de alumnos existentes a materias.

Se agregará un flujo separado de admisión:

- `GET /api/v1/admissions/forms/{slug}` público.
- `POST /api/v1/admissions/applications` público.
- Consulta y administración de solicitudes para personal autorizado.
- Operaciones de inscripción, confirmación, rechazo y cancelación.
- `POST /api/v1/students/{id}/rematriculations`. **Implementado**.

Modelo y reglas:

- Formulario configurable, términos y campos requeridos.
- Solicitud de admisión y reserva de vacante.
- Estados `PreEnrolled`, `Enrolled`, `Confirmed`, `Waitlisted`, `Expired` y `Rejected`.
- Lista de espera FIFO y reserva configurable de 72 horas por defecto.
- Promoción automática al liberarse un cupo.
- Control efectivo por turno y comisión usando transacciones serializables. **Implementado** tanto en admisión como en el alta de materias por `EnrollmentPeriod`.
- Verificación documental antes de confirmar y correlatividades estrictas al inscribirse. **Implementado**.
- Contrato/acuerdo PDF y notificación mediante outbox local. **Implementado**.

## Etapa 2 — M5: docentes y legajo — MVP completado

Primer incremento funcional M5 al 2026-08-22:

- Se expone CRUD administrativo `/api/v1/teachers` con lista activa o histórica, detalle, alta, actualización y baja lógica idempotente.
- El alta exige un usuario activo con rol `Profesor`; `EmployeeNumber` se normaliza y tanto ese número como `UserId` son únicos en Application y SQL Server.
- El legajo incorpora domicilio, contacto de emergencia y auditoría de baja con fecha, actor y motivo; nunca se elimina físicamente desde la API.
- La migración `AddTeacherProfilesAndSoftDelete` conserva la columna heredada `PhoneNumber`, agrega diez columnas y falla con diagnóstico si existen vínculos de usuario duplicados antes de crear el índice único.
- E2E cubre 401/403, duplicados 409, CRUD y baja lógica; 157/157 unit tests, M5 1/1 y regresión 26/26.

Segundo incremento funcional M5 al 2026-08-22:

- Se exponen listado, alta y revisión Admin en `/api/v1/teachers/{id}/documents`, separados de `StudentDocument` y de documentos de admisión.
- Cada tipo se normaliza y versiona de forma append-only; una nueva presentación expira la versión `Submitted` o `Approved` anterior dentro de una transacción serializable con lock del legajo.
- Se aceptan referencias HTTPS o `storage://`, PDF/JPEG/PNG de hasta 10 MB y vigencia no vencida. La revisión sólo permite aprobar o rechazar desde `Submitted`; el rechazo exige observación y registra actor/fecha.
- La migración pendiente se consolidó posteriormente en `20260822214829_AddTeacherDocumentsPositionsAndAssignments`; para documentos crea dos FKs, un índice cronológico y unicidad por `teacher_id + document_type + version`.
- E2E cubre 401/403, validación, aprobación, rechazo, segunda versión y expiración; 166/166 unit tests, M5 2/2, migración/backfill, ArchUnitNET 8/8 y regresión 27/27.

Tercer incremento funcional y cierre MVP M5 al 2026-08-22:

- `TeachingPosition` modela el cargo/vacante estable por materia, comisión, ciclo, semestre y tipo; conserva `TeacherId + IsVacant` como proyección compatible del docente vigente y agrega baja lógica auditada.
- `TeacherAssignment` registra cada designación de forma append-only con inicio, fin, motivos y actores. Asignar/finalizar bloquea cargo y docente y sincroniza la proyección dentro de una transacción serializable.
- Un cargo ocupado no puede cambiarse ni desactivarse; una definición con historial ya no puede mutar materia/comisión/ciclo. Materia y comisión deben compartir carrera, y la comisión debe pertenecer al mismo ciclo.
- Se implementó CRUD administrativo lógico de `/api/v1/teaching-positions`, gestión Admin de `/api/v1/teachers/{id}/assignments` y consulta Profesor `/api/v1/teachers/me/assignments` resuelta desde la identidad autenticada.
- La migración `20260822214829_AddTeacherDocumentsPositionsAndAssignments` consolida los cambios M5 pendientes, normaliza cargos heredados y crea una asignación vigente de backfill cuando existía `TeachingPositions.teacher_id`.
- E2E cubre 401/403, compatibilidad académica, vacante/ocupación, conflictos, historial, finalización, baja lógica y aislamiento entre profesores; 176/176 unit tests, M5 3/3, migración/backfill y regresión 28/28.

Completar las entidades existentes con:

- Datos personales, domicilio, contacto y emergencia.
- Legajo y documentación digital.
- Baja lógica.
- Cargos y vacantes.
- Asignaciones por materia, comisión y ciclo.
- Historial append-only de asignaciones.

API prevista:

- CRUD `/api/v1/teachers`.
- Documentos `/api/v1/teachers/{id}/documents`.
- Cargos `/api/v1/teaching-positions`.
- Asignaciones `/api/v1/teachers/{id}/assignments`.
- Consulta propia `/api/v1/teachers/me/assignments`.

El profesor sólo podrá consultar sus asignaciones; Admin y los roles administrativos habilitados gestionarán el módulo.

## Etapa 3 — M6: asistencias

Estado: **MVP completado el 2026-08-22**.

Entidades principales:

- `AttendanceSession`.
- `AttendanceRecord`.
- `AttendanceJustification`.
- Estado de cierre y reapertura.

Reglas operativas:

- Carga masiva e idempotente por comisión.
- `Present = 1`, `Late = 0.5`, `Absent = 0` y `Justified` fuera del denominador.
- Riesgo calculado contra `MinimumAttendancePercentage`.
- Bloqueo de edición después de 48 horas.
- Reapertura administrativa obligatoriamente auditada.
- Consulta por alumno, materia y comisión.
- Exportación PDF y CSV.

Entrega funcional M6:

- Diez operaciones en `/api/v1/attendance` para sesiones, planilla, cierre, reapertura, justificación, resumen y exportación.
- Una sesión identifica de forma única materia, comisión, ciclo, semestre, fecha, hora y modalidad, incluso entre distintos cargos de la misma oferta.
- `Idempotency-Key` y los índices naturales evitan duplicar sesiones; la planilla usa upsert único por `AttendanceSession + Enrollment` bajo transacción serializable.
- El roster se deriva de inscripciones activas y de la comisión académica del alumno; un profesor sólo opera ofertas cubiertas por su historial real de asignaciones.
- Las cargas admiten `Present`, `Late` y `Absent`; `Justified` sólo se obtiene mediante una justificación Admin append-only con evidencia HTTPS o `storage://`.
- El porcentaje usa unidades ponderadas, excluye justificadas del denominador y compara contra `MinimumAttendancePercentage` del plan vigente para informar riesgo.
- El cierre bloquea cambios y la ventana normal expira a las 48 horas. Una reapertura Admin queda registrada en `AttendanceSessionReopenings` y habilita la corrección retroactiva hasta el siguiente cierre.
- Alumno consulta exclusivamente `/attendance/me/summary`; Admin y Profesor consultan por alumno, materia y comisión según sus permisos.
- Exportación CSV UTF-8 compatible con Excel y PDF local-first mediante `IAttendanceReportGenerator`.
- Migración `20260822223239_AddAttendanceModule`: cuatro tablas, 13 FKs, dos constraints, unicidad de sesión/registro/justificación vigente y regresión de upgrade aprobada.
- Evidencia: 133/133 Domain, 59/59 Application, ArchUnitNET 8/8, M6 1/1 y regresión completa 29/29. Ver [m6-attendance-2026-08-22.md](../tests/api-regression/analysis/m6-attendance-2026-08-22.md).

## Etapa 4 — M7: calificaciones y notas

Estado: **completada al 100% del MVP el 2026-08-22**.

Modelo principal:

- Planilla por oferta de materia.
- Evaluaciones ponderadas y calificaciones por alumno.
- Estados `Draft`, `Submitted`, `Approved`, `Published` y `Closed`.
- Mesas de examen, tribunal, inscripciones, llamados y nota final.

Flujo:

1. El docente carga y envía.
2. Secretaría o Admin aprueba.
3. Se publica para el alumno.
4. El cierre actualiza `Enrollment.FinalGrade` y `Enrollment.Status`.

Una nota aprobada no podrá editarse: deberá reabrirse con motivo y auditoría. Los promedios se redondearán a dos decimales y el alumno sólo verá resultados publicados.

API prevista:

- `/api/v1/gradebooks` y carga masiva de notas.
- Operaciones `submit`, `approve`, `publish`, `reopen` y `close`.
- `/api/v1/exam-tables` e inscripciones a mesa.

Cierre funcional M7:

- `Gradebook` es único e idempotente por oferta académica y contiene evaluaciones configurables cuyos pesos deben sumar exactamente 100%.
- `GradeEntryRevision` conserva todas las versiones de cada nota; un índice filtrado garantiza una sola revisión vigente por evaluación e inscripción.
- El flujo docente–Secretaría implementa `Draft`, `Submitted`, `Approved`, `Published` y `Closed`; sólo Admin aprueba, publica, cierra o reabre con motivo auditado.
- La carga queda limitada al roster y al cargo vigente. El alumno consulta únicamente sus notas publicadas mediante `/gradebooks/me`.
- El cierre calcula el promedio ponderado a dos decimales y actualiza atómicamente `Enrollment.FinalGrade` y `Enrollment.Status` según `CourseApprovalRule`.
- `ExamTable` conserva fecha, llamado, sede y tribunal con exactamente un presidente y al menos un vocal; sólo integrantes del tribunal cargan resultados.
- La inscripción a mesa es propia o administrativa, requiere cursada regularizada y numera automáticamente el intento por inscripción académica.
- Las notas de acta también son revisiones append-only. Una rectificación restaura el estado/nota previo antes de aplicar el nuevo resultado para no dejar una aprobación obsoleta.
- Migración `20260822232101_AddGradebooksAndExamTables`: nueve tablas, 30 FKs, ocho constraints, 14 índices únicos y umbral final compatible en `CourseApprovalRule`.
- Evidencia: 144/144 Domain, 66/66 Application, ArchUnitNET 8/8, M7 1/1 y regresión completa 30/30. Ver [m7-grades-2026-08-22.md](../tests/api-regression/analysis/m7-grades-2026-08-22.md).

## Etapa 5 — M8: certificados — MVP completado

Se mantuvieron los endpoints actuales y se incorporaron aprobación, rechazo, emisión y descarga.

Se soportarán:

- Alumno regular.
- Matrícula.
- Materias aprobadas.
- Situación académica.
- Analítico.
- Estado académico general.
- Permiso de examen.

Cada emisión guardará número `CERT-00000001`, snapshot académico, emisor, fecha, ruta del PDF y hash SHA-256. La numeración será transaccional y los documentos emitidos no podrán eliminarse físicamente.

La generación se abstrae mediante `ICertificatePdfGenerator`; el adaptador local produce un PDF válido sin dependencia comercial y puede sustituirse sin modificar Application.

Cierre funcional M8:

- Los nombres heredados de Angular se normalizan a siete tipos canónicos; la solicitud se vincula a una membresía `StudentCareer` activa y aplica elegibilidad por tipo.
- La revisión Admin conserva actor, fecha y motivo de rechazo; una solicitud activa duplicada se impide tanto en Application como con índice filtrado.
- La emisión toma primero el lock singleton de `CertificateSequences`, crea correlativo y ledger en la misma transacción serializable, y genera el archivo fuera de esa transacción.
- Un fallo de PDF/almacenamiento deja la emisión reintentable sobre la misma reserva, evitando duplicar o perder el correlativo.
- `CertificateIssuance` conserva snapshot, emisor, fecha, clave lógica, SHA-256 y estado; la descarga recalcula el hash y sólo permite Admin o propietario.
- La migración `20260823000423_AddCertificateIssuanceModule` crea ledger/secuencia, normaliza solicitudes heredadas, las vincula a carrera y rechaza duplicados activos históricos salvo el más reciente.
- Evidencia: 161/161 Domain, 75/75 Application, ArchUnitNET 8/8, M8 1/1, migración/backfill y regresión completa 31/31. Ver [m8-certificates-2026-08-22.md](../tests/api-regression/analysis/m8-certificates-2026-08-22.md).

## Etapa 6 — M9: cobros y conceptos — MVP completado

Modelo financiero:

- Conceptos de cobro configurables.
- Tarifas por carrera y condición.
- Planes e ítems de cuotas.
- Vencimientos y recargos.
- Descuentos y becas.
- Deuda individual del alumno.

Reglas:

- Moneda inicial ARS e importes `decimal(18,2)`.
- Aplicar recargo y luego el descuento o beca más beneficioso.
- Guardar un snapshot del cálculo en la deuda.
- Modificar una tarifa no altera deudas emitidas.
- La generación masiva requiere `Idempotency-Key` e índice único.

La API se expone bajo `/api/v1/finance` mediante 13 operaciones para conceptos, tarifas, beneficios, planes, generación y consulta de deudas.

Cierre funcional M9 al 2026-08-24:

- `FinancialConcept`, `FinancialRate`, `FinancialBenefit`, `BillingPlan`, `BillingPlanItem`, `DebtGenerationBatch` y `StudentDebt` separan configuración mutable de deuda histórica inmutable.
- La tarifa puede ser general o específica por condición; la base impide duplicados concurrentes con índices filtrados.
- La beca sólo aplica cuando el alumno tiene un otorgamiento vigente para el ciclo; descuentos y becas compiten y se elige un único beneficio de mayor importe.
- La generación deriva destinatarios de las carreras activas del plan, exige `Idempotency-Key`, bloquea clave y plan en orden fijo y persiste lote, deuda y snapshot en una transacción serializable.
- La unicidad `StudentCareer + BillingPlanItem` impide regenerar un plan con otra clave. Reintentar la misma clave devuelve el mismo lote.
- La consulta `/debts/me` deriva la identidad del usuario autenticado; la consulta por alumno queda restringida a Admin.
- La migración `20260824135158_AddFinanceConceptsPlansAndDebts` y el guard de migración validan siete tablas, FKs, constraints, precisión monetaria e índices financieros.
- Evidencia local: 174/174 Domain, 82/82 Application, ArchUnitNET 8/8, build Release sin warnings, modelo EF alineado, script SQL generado y typecheck correcto.
- Evidencia integrada: migración/backfill M1–M9, M9 1/1, regresión completa 32/32 y Allure generado sobre SQL Server Docker. Swagger ejecutado confirma 184 operaciones, 13 financieras. Ver [m9-finance-2026-08-24.md](../tests/api-regression/analysis/m9-finance-2026-08-24.md).

## Etapa 7 — M10: pagos + M11: recibos — MVP completado

M10 se cerró primero como ledger de pagos. M11 integró la reserva del recibo dentro de las transacciones de confirmación o conciliación aprobada, sin romper los contratos publicados de pagos.

Pagos:

- Efectivo, transferencia, débito y crédito.
- Pagos parciales y distribución entre múltiples conceptos.
- Validación del alumno por DNI.
- Rechazo de sobrepagos.
- Historial completo por alumno.
- Conciliación manual de transferencias.
- Operador y timestamp inmutables.
- Corrección mediante reversión append-only, nunca edición.
- `Idempotency-Key` obligatorio al confirmar.

Cierre funcional M10 al 2026-08-24:

- `Payment`, `PaymentAllocation`, `PaymentReconciliation` y `PaymentReversal` conservan operador y timestamp; conciliaciones y reversas son append-only.
- El borrador identifica al alumno por DNI y distribuye un importe ARS exacto entre una o más deudas propias, rechazando duplicados, deudas canceladas y sobrepagos.
- Efectivo, débito y crédito impactan deuda al confirmar; una transferencia queda `PendingReconciliation` y sólo impacta al aprobarla manualmente.
- Confirmación, conciliación y reversión usan transacción serializable, locks de pago/deudas en orden determinista y revalidación del saldo dentro del lock.
- La confirmación exige `Idempotency-Key`, protegido por índice único filtrado: el mismo pago devuelve el resultado previo y reutilizar la clave en otro pago entra en conflicto.
- La reversión completa restaura los saldos y agrega un movimiento compensatorio; nunca edita ni elimina el pago confirmado.
- La migración `20260824144910_AddPaymentsAndReconciliation` agrega cinco tablas, cuatro medios sembrados y restricciones de integridad/auditoría.
- Evidencia: 187/187 Domain, 91/91 Application, ArchUnitNET 8/8, modelo EF alineado, migración M1–M10, M10 1/1 y regresión completa 33/33. Swagger expone 191 operaciones, 7 de M10, y Allure fue generado. Ver [m10-payments-2026-08-24.md](../tests/api-regression/analysis/m10-payments-2026-08-24.md).

Recibos:

- Emisión automática al confirmar el pago.
- Serie única global `REC-00000001`.
- Bloqueo transaccional para impedir huecos o duplicados.
- PDF interno no fiscal con identificación institucional.
- Alumno, DNI, conceptos, fecha, monto, operador y medio de pago.
- Hash SHA-256 e historial no eliminable.
- Campos opcionales para futura integración CAE/QR.
- Descarga para alumno y Tesorería.
- Backup documentado de SQL Server y del volumen local de archivos.

Cierre funcional M11 al 2026-08-24:

- `Receipt` conserva correlativo, snapshot JSON, emisor, estado de generación, clave de almacenamiento, nombre, tipo, tamaño, SHA-256 y campos fiscales opcionales. No existe operación de borrado ni de edición histórica.
- Cada nuevo pago confirmado reserva exactamente un recibo en la misma transacción serializable que aplica las deudas. La secuencia singleton se bloquea con `UPDLOCK/HOLDLOCK`; si la transacción falla, también revierte el incremento.
- Una transferencia pendiente o rechazada no genera recibo. La conciliación aprobada sí lo reserva atómicamente.
- El PDF se genera fuera de la transacción. Un fallo deja el ledger en `Failed` y el reintento usa el mismo `REC-########`, evitando duplicados y huecos lógicos.
- El snapshot incluye institución, alumno, DNI, conceptos, importe, fecha, operador y medio. Una reversión posterior mantiene el comprobante y sólo refleja el estado actual del pago por separado.
- La descarga recalcula SHA-256 y exige Admin o alumno propietario. Tesorería dispone de listado global, detalle y regeneración controlada.
- La migración `20260824165321_AddDigitalReceipts` crea `Receipts` y `ReceiptSequences`, dos FKs restrictivas, constraints, cuatro índices únicos y el singleton inicial sin backfill implícito de pagos M10 históricos.
- El volumen Docker `backend_files` hace durable el archivo y [BACKUP_RECEIPTS.md](BACKUP_RECEIPTS.md) documenta backup/restore coordinado con SQL Server.
- Evidencia: 194/194 Domain, 97/97 Application, ArchUnitNET 8/8, modelo EF alineado, migración M1–M11, M11 1/1 y regresión completa 34/34. Swagger expone 196 operaciones, 5 de M11, y Allure fue generado. Ver [m11-receipts-2026-08-24.md](../tests/api-regression/analysis/m11-receipts-2026-08-24.md).

API M10 implementada:

- `/api/v1/finance/payment-methods`.
- `/api/v1/payments`, historial y reversión.
- Operaciones de conciliación.

API M11 implementada:

- `/api/v1/receipts`, detalle, regeneración y `/api/v1/receipts/{id}/download`.
- `/api/v1/students/me/receipts`.

## Etapa 8 — Estabilización y release — en curso

M4–M11 quedan funcionalmente cerrados. Esta etapa concentra hardening transversal, observabilidad, ejecución de los workflows como checks requeridos, resolución de deuda técnica registrada y preparación operativa del despliegue, sin reabrir el alcance de dominio del MVP.

## Estrategia de pruebas automatizadas

Cada módulo tendrá cliente Playwright, schemas Zod, factories, cleanup seguro y tags propios.

Cobertura mínima por endpoint nuevo:

- Happy path.
- Sin autenticación.
- Rol incorrecto.
- Validación de entrada.
- Recurso inexistente.
- Conflicto o regla crítica de negocio.
- Persistencia y consulta posterior.

Escenarios críticos por módulo:

- M4: cupos concurrentes, lista de espera, expiración, correlatividades, documentación y rematriculación.
- M5: DNI único, baja lógica, asignaciones históricas y aislamiento docente.
- M6: carga masiva idempotente, porcentaje, justificación, bloqueo y reapertura.
- M7: promedio, aprobación, publicación, cierre y llamados de examen.
- M8: emisión concurrente, secuencia, PDF y autorización de descarga.
- M9: cálculo de recargos/descuentos y generación masiva idempotente.
- M10: pagos parciales, multiconcepto, sobrepago, idempotencia, conciliación, reversión y aislamiento.
- M11: secuencia concurrente, recibo atómico, PDF/hash y descarga autorizada.

Comandos objetivo:

```text
npm run test:api:m4
npm run test:api:m5
npm run test:api:m6
npm run test:api:m7
npm run test:api:m8
npm run test:api:m9
npm run test:api:m10
npm run test:api:m11
npm run verify:fast
npm run verify:full
```

CI requerido en cada pull request:

- Build Release.
- Validación de arquitectura con ArchUnitNET.
- Unit tests.
- Typecheck.
- Regresión de migraciones.
- Smoke.
- Suite del módulo modificado.
- Regresión completa antes del merge.

Objetivos de velocidad: smoke menor a 3 minutos y regresión completa menor a 15 minutos después de iniciar Docker. Se espera crecer desde 19 hasta aproximadamente 80–100 escenarios significativos.

## Criterio de cierre por módulo

Un módulo sólo se considera terminado cuando:

1. Sus migraciones funcionan desde una base nueva y desde el snapshot anterior.
2. No rompe endpoints integrados de M1–M4.
3. Sus reglas críticas tienen pruebas unitarias.
4. Sus endpoints tienen contratos Zod y regresión Playwright.
5. Pasan build, typecheck, suite del módulo y regresión completa.
6. README, Swagger, este roadmap y `AGENTS.md` reflejan el estado real.

## Fuera del MVP

- Cambios de frontend.
- Integración AFIP.
- Firma digital criptográfica.
- SMTP productivo.
- Almacenamiento cloud.
- Antivirus integrado.
- Exportación XLSX nativa; se entregarán PDF y CSV.

Las interfaces de email, archivos y PDF deben permitir incorporar estas capacidades sin modificar las reglas de dominio.
