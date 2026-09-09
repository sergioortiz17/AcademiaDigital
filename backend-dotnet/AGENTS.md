# AGENTS.md — Backend AcademiaDigital

Este archivo es la memoria operativa del backend. Aplica a todo lo que está dentro de `backend-dotnet/` y debe consultarse antes de modificar código. La implementación y las migraciones son la fuente de verdad; los README y documentos de análisis pueden quedar desactualizados.

## Estado actual

Actualizado: **2026-08-24**.

- Solución ASP.NET Core **.NET 8 / C# 12**, con Entity Framework Core 8 y SQL Server.
- Arquitectura en cuatro proyectos: `Domain`, `Application`, `Infrastructure` y `API`.
- Rama observada al actualizar este documento: `modulo_5_backend`.
- Último commit que afectó `backend-dotnet/`: `87f13cb` (`test(api): add Playwright regression suite and Allure CI`, 2026-08-01).
- Última migración: `20260824165321_AddDigitalReceipts`.
- Verificación local hecha el 2026-08-24: build Release pasa con **0 warnings y 0 errores**; Unit Tests pasan **291/291** —194 Domain y 97 Application—, ArchUnitNET **8/8**, modelo EF sin cambios pendientes y `npm.cmd run typecheck` pasa.
- La solución incluye unit tests de Domain y Application, además de `tests/AcademiaDigital.ArchitectureTests/`. Las pruebas funcionales automatizadas siguen en `tests/api-regression/` (Playwright + TypeScript + Zod + Allure).
- Regresión E2E vigente: typecheck, migración/backfill M1–M11, M4 **7/7**, M5 **3/3**, M6–M11 **1/1** cada uno y **34/34 tests pasaron** sobre SQL Server Docker el 2026-08-24; Swagger confirmó 196 operaciones y Allure fue generado. Evidencia en `tests/api-regression/analysis/m11-receipts-2026-08-24.md`.

## Arquitectura objetivo y dependencias

El backend es un monolito modular con **Clean Architecture**, casos de uso explícitos estilo Command/Query Handler, repositorios específicos y Unit of Work. Esta sección es normativa: el código heredado que no la cumpla se considera una excepción, no un patrón para copiar.

```text
HTTP
  -> API Controller
  -> Application Command/Query Handler
  -> Domain rules + repository/service ports
  -> Infrastructure implementation
  -> SQL Server or external adapter
```

Dirección de dependencias permitida:

- `Domain`: no referencia otros proyectos de la solución.
- `Application` → `Domain`.
- `Infrastructure` → `Application` + `Domain`.
- `API` → `Application` + `Infrastructure`.

La referencia `API` → `Infrastructure` existe para el composition root y el arranque. No autoriza a controllers a consumir `AppDbContext`, repositorios concretos o servicios concretos de Infrastructure.

### Responsabilidad de cada capa

| Capa | Debe contener | No debe contener |
|---|---|---|
| Domain | Entidades, enums, excepciones de negocio, invariantes, servicios puros e interfaces de repositorios existentes. | EF Core, SQL, HTTP, DTOs de transporte, configuración, logging, filesystem o SDKs externos. |
| Application | Commands, queries, handlers, DTOs, facades, autorización contextual y puertos como Unit of Work, archivos, email o PDF. | `DbContext`, LINQ de EF, SQL, `HttpContext`, `IActionResult`, controllers o implementaciones de proveedores. |
| Infrastructure | `AppDbContext`, configuraciones EF, migraciones, repositorios y adaptadores concretos de JWT, archivos, email o PDF. | Decisiones de flujo HTTP, permisos de endpoint u orquestación extensa de casos de uso. |
| API | Controllers delgados, modelos exclusivamente HTTP, middleware, Swagger y composición de dependencias. | Reglas de negocio, consultas EF, transacciones, acceso directo a archivos o construcción manual de repositorios. |

Reglas de ubicación:

- Los repositorios de entidades mantienen sus contratos en `Domain/Interfaces/Repositories` por consistencia con la solución actual. No duplicar la misma abstracción en Application.
- Los puertos de infraestructura requeridos por un caso de uso nuevo —por ejemplo `IFileStorage`, `IEmailSender` o `IPdfGenerator`— van en `Application/Interfaces` y su implementación en Infrastructure.
- Los DTOs que cruzan controller y handler van en Application. Un modelo puede quedar en API sólo si representa una preocupación puramente HTTP, como binding multipart o headers.
- Las entidades Domain nunca se devuelven directamente desde un controller. Application debe proyectarlas a DTOs.
- Las reglas deterministas y reutilizables pertenecen a Domain Services; las reglas que necesitan repositorios se orquestan en Application.

## Patrón canónico para una funcionalidad

Cada operación nueva debe formar una vertical slice dentro de las cuatro capas existentes:

1. Definir o ajustar entidad, enum e invariantes en Domain.
2. Definir el puerto de persistencia o servicio sólo si no existe uno adecuado.
3. Crear un `Command` o `Query` y un handler en `Application/UseCases/<Modulo>`.
4. Hacer que el handler valide relaciones, aplique reglas, coordine repositorios y devuelva un DTO.
5. Implementar persistencia/adaptadores en Infrastructure y su configuración EF cuando corresponda.
6. Exponer el caso de uso desde un controller delgado y registrar sus dependencias en DI.
7. Agregar migración, pruebas de regla y regresión de API en la misma entrega.

Convenciones del patrón:

- Mantener Commands y Queries separados conceptualmente, aunque no se use una librería CQRS.
- Continuar con handlers registrados explícitamente. No introducir MediatR, un bus interno, generic repositories ni otro framework transversal en un único módulo sin una decisión arquitectónica para toda la solución.
- Un handler representa un caso de uso. No crear servicios genéricos con CRUD de módulos distintos ni agregar responsabilidades nuevas a `StudentManagementService`.
- Los módulos se comunican mediante contratos de Application o reglas/entidades compartidas de Domain; nunca invocando controllers ni repositorios concretos de otro módulo.
- Usar `TimeProvider` inyectado en código nuevo dependiente del tiempo —expiraciones, reservas, asistencia, pagos— en lugar de dispersar `DateTime.UtcNow`, para permitir pruebas deterministas.

## Persistencia, atomicidad y concurrencia

- El handler de Application es dueño del límite transaccional de un caso de uso multi-entidad.
- Para una única escritura simple puede conservarse el patrón actual del repositorio. Para dos o más escrituras relacionadas, usar `IUnitOfWork.ExecuteInTransactionAsync`; no abrir transacciones en controllers.
- No crear transacciones anidadas ni mezclar una transacción manual con llamadas que usen otro `DbContext`.
- Toda operación reintentable o expuesta a doble envío debe tener idempotencia respaldada por un índice único, no sólo por una consulta previa.
- Toda unicidad o invariante que pueda romperse por concurrencia debe validarse en Application y reforzarse en SQL Server con índices, constraints o `rowversion`.
- En M4, una `Commission` representa un único pool de admisión: puede tener como máximo un `AdmissionForm`, el formulario dirigido exige capacidad explícita y el lock serializable por formulario protege ese pool. `CommissionId = null` se reserva para formularios generales/legacy. No permitir varios formularios para una comisión sin introducir primero un agregado de capacidad compartida y migrar esta invariante.
- En M4, la rematriculación es append-only y única por `StudentCareer + AcademicYear`; sólo avanza al año calendario inmediato siguiente y reemplaza la asignación académica vigente dentro de la misma transacción serializable. No implementar rematriculaciones como sobrescritura de `StudentCareer` ni agregarlas a `StudentManagementService`.
- En M4, `Confirmed` exige una versión `Approved` de cada `DocumentRequirement` obligatorio, activo, vigente y aplicable globalmente o a la carrera. Los documentos de postulación son versiones del requisito: al presentar una nueva se expira la versión `Submitted`/`Approved` anterior; no reutilizar `StudentDocument` antes de que exista un `Student` ni mover este flujo a `StudentManagementService`.
- En M4, la transición a `Confirmed` crea atómicamente exactamente un snapshot inmutable del acuerdo y un mensaje outbox con clave de deduplicación única. La generación del PDF, el guardado de archivos y la notificación ocurren después del commit mediante puertos de Application; nunca ejecutar esos efectos externos dentro de la transacción SQL.
- Los acuerdos se identifican mediante claves lógicas y nunca exponen rutas físicas. `AdmissionStorage__RootPath` debe apuntar a un volumen durable en producción; conservar la validación que impide escapar de esa raíz.
- En M4, `CreateEnrollmentCommandHandler` debe validar el plan vigente del alumno y ejecutar `EnrollmentEligibilityPolicy`: una correlativa `Strict` faltante bloquea, una `Soft` sólo advierte y una materia aprobada o en curso no puede reinscribirse. Mantener esta regla alineada con `GetEligibleCoursesForStudentQueryHandler`.
- El cupo de `EnrollmentPeriod` representa estudiantes distintos por turno, no filas de materias. Toda inscripción bloquea el período con `UPDLOCK/HOLDLOCK`, revalida actividad, duplicado y capacidad dentro de una transacción serializable; la actualización de cuotas usa el mismo lock y nunca puede bajar de la ocupación actual.
- El alta pública de admisión conserva `[EnableRateLimiting("PublicAdmissionSubmission")]` y verifica `challengeToken` mediante `IAdmissionChallengeVerifier` antes de leer o escribir datos. Particionar por `RemoteIpAddress`, nunca por un `X-Forwarded-For` no validado; los proxies confiables deben normalizar la IP en el borde.
- En M5, cada `User` con rol `Profesor` puede tener como máximo un `Teacher`; `UserId` y `EmployeeNumber` tienen índices únicos. La baja del legajo es lógica, idempotente y auditada con fecha, actor y motivo: no reintroducir delete físico ni adjuntar una navegación `User` no trackeada al insertar `Teacher`.
- En M5, la documentación docente usa `TeacherDocument`, no `StudentDocument` ni documentos de admisión. Cada alta crea una versión nueva por `TeacherId + DocumentType`, expira la anterior `Submitted`/`Approved` y conserva todo el historial. La numeración se serializa bloqueando el legajo y se refuerza con un índice único; no calcular la versión fuera de esa transacción.
- Un documento docente sólo puede revisarse desde `Submitted` hacia `Approved` o `Rejected`; el rechazo exige observación y toda revisión registra fecha y usuario. Aceptar sólo referencias HTTPS o claves lógicas `storage://`, nunca rutas físicas del host.
- En M5, `TeachingPosition` representa el cargo/vacante estable y conserva `TeacherId + IsVacant` sólo como proyección compatible del estado vigente. `TeacherAssignment` es la fuente append-only del historial: asignar/finalizar actualiza ambas estructuras atómicamente bajo lock serializable y nunca elimina el historial.
- Un cargo nuevo debe vincular materia, comisión y ciclo compatibles. No cambiar su definición si ya tuvo asignaciones, no desactivarlo mientras esté ocupado y no usar delete físico. La consulta `/teachers/me/assignments` resuelve el `Teacher` desde el usuario autenticado; nunca acepta un `TeacherId` aportado por el profesor.
- En M6, `AttendanceSession` representa una planilla de una oferta académica —materia, comisión, ciclo y semestre— y conserva el cargo usado para crearla sólo como trazabilidad. La unicidad natural abarca fecha, hora y modalidad entre todos los cargos de esa oferta; `Idempotency-Key` también es único y ambos controles se ejecutan bajo transacción serializable.
- Un Profesor sólo puede crear, consultar, cargar, cerrar o exportar sesiones cubiertas por su historial real de `TeacherAssignment` en la fecha de clase. No autorizar por un `TeacherId`, `CourseId` o `CommissionId` recibido del cliente. Admin puede operar todas las ofertas; Alumno sólo consulta `/attendance/me/summary`, resolviendo su identidad desde la sesión.
- `AttendanceRecord` es único por `AttendanceSession + Enrollment` y la carga masiva hace upsert únicamente para alumnos del roster derivado de inscripción y comisión. `Justified` nunca se acepta en carga: sólo una justificación Admin append-only puede establecerlo, conservando estado previo, actor, fecha, categoría, motivo y evidencia HTTPS o `storage://`.
- El cálculo usa unidades de la sesión: `Present = 1`, `Late = 0.5`, `Absent = 0`; `Justified` queda fuera del denominador. El riesgo compara el resultado de sesiones cerradas contra `CourseApprovalRule.MinimumAttendancePercentage`; no inventar un umbral global cuando la materia no lo define.
- Una sesión cerrada no se edita. Una sesión abierta vence 48 horas después del fin de la clase/día; sólo una reapertura Admin con motivo en `AttendanceSessionReopenings` habilita corrección retroactiva y el siguiente cierre vuelve a bloquearla. No borrar sesiones, registros, justificaciones ni reaperturas para corregir asistencia.
- Las exportaciones de asistencia se generan a través de `IAttendanceReportGenerator`. CSV debe conservar UTF-8 con BOM para Excel y PDF debe devolverse como archivo; no generar documentos en controllers ni exponer rutas físicas.
- En M7, `Gradebook` es único por materia, comisión, ciclo y semestre, además de su `Idempotency-Key`. El Profesor sólo crea o modifica la planilla de un `TeachingPosition` con asignación vigente; no autorizar mediante `TeacherId`, curso o comisión recibidos del cliente. Las evaluaciones pertenecen a la planilla, tienen nombre/orden únicos y sus pesos deben sumar exactamente 100%.
- `GradeEntryRevision` es append-only por evaluación e inscripción: cada corrección expira la revisión vigente y crea la versión siguiente dentro de una transacción serializable. No sobrescribir ni borrar notas históricas. Sólo `Draft` admite carga; `Submitted`, `Approved`, `Published` y `Closed` quedan bloqueados hasta una reapertura Admin con motivo en `GradebookReopenings`.
- El workflow de cursada es `Draft → Submitted → Approved → Published → Closed`. Profesor/Admin pueden cargar y enviar; sólo Admin/Secretaría aprueba, publica, cierra o reabre. El Alumno sólo consulta `/gradebooks/me` y nunca recibe planillas no publicadas. El cierre calcula promedio ponderado en escala 0–10, redondea a dos decimales y actualiza `Enrollment.FinalGrade/Status` atómicamente según `CourseApprovalRule`; el fallback compatible para regularidad es 6 cuando la regla legacy no define mínimo.
- En M7, `ExamTable` es única por materia, fecha y llamado y también exige idempotencia. El tribunal tiene exactamente un presidente, al menos un vocal y docentes activos sin duplicados. Un Profesor sólo lista/consulta/carga actas de mesas donde integra el tribunal; crear, iniciar acta, publicar y reabrir son operaciones Admin.
- La inscripción a examen es única por mesa e inscripción académica, sólo admite `EnrollmentStatus.Regularized` antes del vencimiento y numera el intento bajo lock serializable. El Alumno sólo puede inscribir su propia `Enrollment`; nunca aceptar un `StudentId` aportado por el cliente como autorización.
- `ExamGradeRevision` conserva versiones append-only y usa `CourseApprovalRule.MinimumFinalExamGrade` —default compatible 6— para validar aprobado/desaprobado. El alumno sólo ve el resultado después de publicar. Al rectificar un acta publicada, restaurar primero `PreviousEnrollmentStatus/PreviousFinalGrade` y luego aplicar la revisión vigente; no dejar una aprobación anterior ni corregir con delete físico.
- En M8, `CertificateRequest` conserva la solicitud y su revisión, mientras `CertificateIssuance` es el ledger histórico inmutable. Los nombres heredados se normalizan mediante `CertificatePolicy`; toda solicitud nueva debe vincularse a una `StudentCareer` activa y validar elegibilidad por tipo. No aceptar un `StudentId` o carrera ajena como autorización, ni emitir solicitudes que no estén `Approved`.
- Las rutas heredadas `/certificates/my` y `/certificates/all` deben conservar el contrato Angular `Pending/Approved/Rejected`: los estados internos `Issuing/Issued` se proyectan como `Approved`, y el progreso real se expone de forma aditiva en `issuance.status`. El filtro legacy `status=Approved` incluye los tres estados internos; no ampliar el enum público sin coordinar el frontend.
- La numeración M8 es una única serie global `CERT-00000001`. Toda emisión toma primero `CertificateSequences(1)` con `UPDLOCK/HOLDLOCK`, y recién después bloquea solicitud y lee datos académicos dentro de una transacción serializable. Este orden global evita deadlocks; no invertirlo ni calcular `MAX + 1`. Secuencia, snapshot y ledger se confirman juntos.
- PDF y almacenamiento de certificados ocurren después del commit mediante `ICertificatePdfGenerator` e `IFileStorage`. Si fallan, la emisión queda `Failed` y se reintenta con la misma reserva; nunca asignar otro correlativo. Al quedar `Ready`, guardar SHA-256 y marcar la solicitud `Issued`. La descarga recalcula el hash y sólo admite Admin o el `UserId` propietario.
- Los certificados emitidos, snapshots y correlativos no se borran ni sobrescriben. Las rutas físicas nunca cruzan HTTP; exponer sólo `PublicId`, clave lógica y endpoint de descarga. Cualquier futura revocación debe ser compensatoria y append-only.
- En M9, `FinancialConcept`, `FinancialRate`, `FinancialBenefit`, `BillingPlan` y sus ítems son configuración mutable; `StudentDebt` y `DebtGenerationBatch` son evidencia histórica append-only. Toda deuda guarda snapshot de tarifa, recargo, beneficio e importe final. Cambiar conceptos, tarifas o beneficios nunca recalcula una deuda emitida; ajustes futuros deben ser movimientos compensatorios de M10.
- Todos los importes financieros iniciales son ARS `decimal(18,2)` y se redondean a dos decimales con `MidpointRounding.AwayFromZero`. El cálculo aplica primero recargo por vencimiento y luego exactamente un descuento o beca, el de mayor importe; ante empate se prioriza la beca. Una beca sólo es elegible si existe un `StudentScholarship` `Granted`, vigente y del ciclo correspondiente.
- La generación M9 deriva destinatarios desde las `StudentCareer` activas de la carrera del plan; nunca recibe listas de `StudentId` del cliente. `/finance/debts/me` resuelve al alumno desde `CurrentUser`; la consulta por `studentId` es exclusivamente Admin.
- La generación masiva toma primero el rango de `Idempotency-Key` y después bloquea la fila de `BillingPlan` mediante `UPDLOCK/HOLDLOCK`, dentro de una transacción serializable. No invertir este orden. `DebtGenerationBatch.IdempotencyKey` y la unicidad natural `StudentCareerId + BillingPlanItemId` deben seguir reforzadas por índices únicos: la misma clave devuelve el lote previo y otra clave para un plan ya emitido entra en conflicto.
- En M10, `Payment` es el ledger principal; `PaymentAllocation`, `PaymentReconciliation` y `PaymentReversal` conservan imputación y movimientos compensatorios append-only. No editar ni eliminar pagos confirmados, conciliaciones o reversas. Una corrección restaura saldos mediante una única reversión total auditada.
- Todo borrador M10 identifica al alumno por DNI normalizado y distribuye exactamente el importe ARS entre deudas propias distintas. Validar pertenencia, estado y saldo al crear, y volver a validar el sobrepago después de bloquear las deudas al confirmar o aprobar una transferencia.
- Efectivo, débito y crédito impactan la deuda al confirmar. Una transferencia exige referencia externa, queda `PendingReconciliation` y no reserva saldo; aprobarla puede entrar en conflicto si otro pago consumió el saldo mientras estaba pendiente. Rechazarla exige una nota y nunca modifica la deuda.
- La confirmación M10 exige `Idempotency-Key` de 8–100 caracteres. Dentro de la transacción serializable se toma primero el rango de esa clave y después la fila del pago; el índice único filtrado en `Payments.confirmation_idempotency_key` es obligatorio. La misma clave para el mismo pago devuelve el resultado previo; usarla en otro pago responde conflicto.
- Confirmación, conciliación y reversión bloquean primero el pago y luego sus `StudentDebt` en orden ascendente mediante `UPDLOCK/HOLDLOCK`. Mantener ese orden global, recargar los valores trackeados después del lock y aplicar deuda/pago/auditoría en el mismo commit. No mover locks o transacciones a controllers.
- `/payments/me` deriva el alumno desde el usuario autenticado; nunca acepta un `StudentId` del cliente. Alta, confirmación, conciliación, reversión e historial por `studentId` son Admin. Los cuatro medios se obtienen del catálogo persistido; no hardcodear sus IDs en casos de uso o clientes.
- En M11, cada nuevo pago confirmado debe tener exactamente un `Receipt`. La reserva de `ReceiptSequence(1)`, el snapshot y el ledger ocurren en la misma transacción serializable que confirma el pago o aprueba la transferencia, después de bloquear pago y deudas. La secuencia usa `UPDLOCK/HOLDLOCK`; no usar `MAX + 1`, series paralelas ni reservar antes de validar/aplicar las deudas.
- `Receipt` es evidencia histórica inmutable. El número global usa `REC-00000001`; una reversión del pago no elimina ni reescribe el recibo. Una transferencia `PendingReconciliation` o rechazada no tiene comprobante. Los pagos M10 históricos no se backfillean implícitamente; un reintento idempotente de confirmación/conciliación puede completar la reserva faltante.
- PDF y almacenamiento M11 ocurren después del commit mediante `IReceiptPdfGenerator` e `IFileStorage`. Un fallo deja `Failed` y debe reintentarse sobre el mismo correlativo; `Ready` exige SHA-256. Nunca ejecutar filesystem dentro de la transacción, exponer rutas físicas ni generar documentos desde el controller.
- `/receipts` y la regeneración son Admin. Detalle/descarga permiten Admin o alumno propietario; `/students/me/receipts` siempre deriva identidad desde `CurrentUser`. La descarga recalcula el hash. `FiscalCae` y `FiscalQrData` permanecen opcionales hasta una integración fiscal explícita.
- SQL Server y el volumen que contiene `AdmissionStorage__RootPath` forman una única unidad de backup/restore. En Docker productivo conservar `backend_files:/app/data/files` y seguir [BACKUP_RECEIPTS.md](docs/BACKUP_RECEIPTS.md); no respaldar sólo la base ni eliminar el volumen como cleanup rutinario.
- Consultas de sólo lectura usan `AsNoTracking` y proyección cuando no se necesita el grafo completo. Evitar N+1 y `Include` indiscriminado.
- No usar `EnsureCreated` ni editar migraciones aplicadas. Todo cambio de esquema se realiza con una migración EF y prueba de upgrade/backfill.
- Entidades históricas, pagos, recibos, notas aprobadas y auditorías son append-only o se corrigen mediante una operación compensatoria; no mediante delete físico o sobrescritura silenciosa.

## Contratos HTTP y errores

- Preservar rutas, payloads, códigos y respuestas ya consumidos por Angular. Los cambios en v1 deben ser aditivos salvo aprobación explícita de un breaking change.
- Controllers protegidos heredan de `ApiControllerBase` y delegan autorización según la política vigente; no deben leer claims o tokens de formas nuevas e incompatibles.
- No agregar `try/catch` repetidos a controllers nuevos. Las excepciones conocidas deben mapearse centralmente en middleware; sólo conservar manejo local cuando sea necesario para compatibilidad de un endpoint existente.
- Validar forma y campos básicos en el borde HTTP; validar reglas de negocio y relaciones en Application/Domain.
- Propagar `CancellationToken` desde controller hasta EF o el adaptador externo.
- No documentar seguridad sólo en Swagger: cada permiso debe estar implementado y cubierto por pruebas 401/403.
- No exponer stack traces, entidades EF, secretos, tokens ni rutas físicas de almacenamiento.

## Excepciones heredadas controladas

Estas excepciones existen hoy y no deben usarse como ejemplo:

- `CalendarController` inyecta `AppDbContext` y contiene consultas/escrituras. No agregarle nuevas operaciones de esa forma; al modificar su comportamiento, extraer el caso de uso a Application y la persistencia a Infrastructure.
- `Program.cs` resuelve `AppDbContext` para migrar durante el arranque. Es una excepción válida del composition root, no una autorización para usar EF en otros archivos de API.
- `StudentManagementService` implementa muchos casos de uso desde Infrastructure. No agregar allí M4–M11; crear handlers y servicios por módulo. Separarlo sólo mediante refactor cubierto por regresión.
- Varios repositorios existentes llaman `SaveChangesAsync` internamente. No copiar ese patrón en flujos multi-entidad; encapsular la atomicidad con Unit of Work y migrar repositorios sólo cuando el caso de uso lo requiera y tenga pruebas.
- Los formatos de error actuales son mixtos. Mantener compatibilidad en endpoints existentes y usar el mapeo central acordado para endpoints nuevos.

Toda excepción arquitectónica nueva requiere motivo escrito en esta sección, alcance mínimo, prueba que la proteja y una condición clara para retirarla. Si no se documenta, no se acepta.

### Validación automática con ArchUnitNET

`tests/AcademiaDigital.ArchitectureTests/` es la especificación ejecutable de las dependencias de esta sección. Actualmente valida:

- que Domain no dependa de Application, Infrastructure ni API;
- que Application no dependa de Infrastructure ni API;
- que Infrastructure no dependa de API;
- que los controllers no dependan de Infrastructure, salvo la excepción exacta de `CalendarController`;
- que las capas `AcademiaDigital.*` no formen ciclos.

El workflow independiente `.github/workflows/architecture.yml` ejecuta estas reglas en cada pull request o push a `main` que afecte el backend, además de permitir ejecución manual. Las pruebas deben correr en Debug porque ArchUnitNET analiza los binarios compilados. Toda modificación de límites o excepción debe actualizar en conjunto estas pruebas y este documento; no se deben debilitar reglas para hacer pasar una desviación no aprobada.

## Control anti-deriva antes de cerrar una tarea

Revisar en cada cambio de backend:

1. Ningún proyecto agregó una referencia contraria a la dirección permitida.
2. Domain y Application no importan EF Core, ASP.NET Core, SQL Client ni Infrastructure.
3. Ningún controller nuevo usa `AppDbContext`, `DbSet`, SQL o un repositorio concreto.
4. Las reglas nuevas están en Domain/Application, no escondidas en controllers o adaptadores.
5. Los flujos multi-entidad tienen una transacción explícita e idempotencia cuando corresponda.
6. Ninguna entidad Domain se serializa directamente como contrato HTTP.
7. La vertical slice incluye DI, migración y pruebas proporcionales al riesgo.
8. No se amplió una excepción heredada ni se introdujo un segundo patrón para resolver el mismo problema.

Búsquedas rápidas desde `backend-dotnet/`:

```powershell
dotnet test tests/AcademiaDigital.ArchitectureTests/AcademiaDigital.ArchitectureTests.csproj -c Debug
dotnet list src/AcademiaDigital.Application/AcademiaDigital.Application.csproj reference
dotnet list src/AcademiaDigital.Infrastructure/AcademiaDigital.Infrastructure.csproj reference
dotnet list src/AcademiaDigital.API/AcademiaDigital.API.csproj reference
rg -n "Microsoft.EntityFrameworkCore|Microsoft.Data.SqlClient|AcademiaDigital.Infrastructure|Microsoft.AspNetCore" src/AcademiaDigital.Domain src/AcademiaDigital.Application
rg -n "AppDbContext|Microsoft.EntityFrameworkCore" src/AcademiaDigital.API/Controllers
```

La segunda búsqueda debe quedar sin resultados. En la tercera sólo está tolerada temporalmente la excepción registrada de `CalendarController`; cualquier coincidencia nueva es deriva arquitectónica.

Ubicaciones importantes:

- [Roadmap aprobado M4–M11](docs/ROADMAP_M4_M11.md). M1–M3 son el baseline estable, M4–M11 están cerrados para el MVP y el trabajo continúa en estabilización/release.
- [Guía funcional M4–M11 para frontend](docs/GUIA_FUNCIONAL_M4_M11_FRONTEND.md): actores, casos de uso, estados, funcionalidades, flujos integrados, brecha Angular y orden recomendado de integración.
- [Reporte de cobertura de calidad y arquitectura](docs/COBERTURA_CALIDAD_ARQUITECTURA.md): matriz de controles, herramientas, alcance, estado y brechas.
- [Matriz endpoint–permiso–test](docs/API_COVERAGE_MATRIX.md): inventario de las operaciones Swagger, guards efectivos y evidencia automatizada.
- [Evidencia M7 calificaciones y mesas](tests/api-regression/analysis/m7-grades-2026-08-22.md): alcance, defecto de contrato detectado y resultados ejecutados.
- [Evidencia M8 certificados](tests/api-regression/analysis/m8-certificates-2026-08-22.md): alcance, concurrencia, PDF, autorización y resultados ejecutados.
- [Evidencia M9 cobros y conceptos](tests/api-regression/analysis/m9-finance-2026-08-24.md): alcance financiero, concurrencia, migración y regresión integrada ejecutada.
- [Evidencia M10 pagos](tests/api-regression/analysis/m10-payments-2026-08-24.md): medios, imputación, conciliación, reversión, locks, migración y regresión integrada ejecutada.
- [Evidencia M11 recibos](tests/api-regression/analysis/m11-receipts-2026-08-24.md): correlativo, atomicidad, PDF/hash, autorización, migración y regresión integrada ejecutada.
- [Backup y restauración de recibos](docs/BACKUP_RECEIPTS.md): procedimiento coordinado para SQL Server y archivos persistentes.
- Unit tests de reglas: `tests/AcademiaDigital.Domain.UnitTests/` y `tests/AcademiaDigital.Application.UnitTests/`.
- Pruebas ejecutables de arquitectura: `tests/AcademiaDigital.ArchitectureTests/ArchitectureDependencyTests.cs`.
- Workflow de arquitectura: `../.github/workflows/architecture.yml`.
- Entrada y registro de DI: `src/AcademiaDigital.API/Program.cs`.
- Registro de repositorios/infraestructura: `src/AcademiaDigital.Infrastructure/DependencyInjection.cs`.
- Contexto y configuraciones EF: `src/AcademiaDigital.Infrastructure/Persistence/`.
- Migraciones: `src/AcademiaDigital.Infrastructure/Migrations/`.
- Contratos HTTP: controllers en `src/AcademiaDigital.API/Controllers/` y DTOs en `src/AcademiaDigital.Application/DTOs/`.
- Reglas académicas puras: `src/AcademiaDigital.Domain/Services/`.
- Análisis detallado de contratos y defectos: `tests/api-regression/analysis/backend-analysis.md`.

## Funcionalidad implementada

- Autenticación y sesión: registro, login, logout, checkSession, recuperación/cambio de contraseña y perfil.
- Administración de usuarios, roles y estado activo.
- Carreras, materias, planes de estudio, materias por plan y correlatividades.
- Estudiantes multi-carrera, plan vigente, progreso/elegibilidad y asignaciones académicas.
- Períodos e inscripciones, cupos configurables, reportes y bajas.
- Comisiones y calendario académico.
- Certificados.
- Gestión estudiantil módulo 3: estado/historial, legajo, documentos, becas y campos personalizados.
- M4 MVP completado: formulario público configurable con antiabuso, cupo transaccional aislado por comisión/turno, espera FIFO, expiración/promoción, rematriculación anual, correlativas estrictas, documentación, acuerdos PDF/outbox, administración Admin e historial append-only.
- M5 MVP completado: legajo/baja, documentos versionados, cargos/vacantes y asignaciones históricas con consulta propia aislada están implementados y cubiertos.
- M6 MVP completado: sesiones por hora/día, carga masiva idempotente, porcentajes/riesgo, justificaciones, cierre/reapertura, consultas aisladas y exportaciones CSV/PDF están implementados y cubiertos.
- M7 MVP completado: planillas ponderadas, notas versionadas, aprobación/publicación/cierre, consulta propia, mesas, tribunal, inscripciones, llamados, actas y rectificación compensatoria están implementados y cubiertos.
- M8 MVP completado: siete tipos de certificados, solicitudes/revisión, correlativo global, snapshot, PDF/hash, emisión reintentable, historial y descarga autorizada están implementados y cubiertos.
- M9 MVP completado: conceptos, tarifas, beneficios, planes/cuotas, deuda con snapshot, generación idempotente y consultas Admin/propietario están cubiertos por unitarios, migración y regresión API integrada.
- M10 MVP completado: cuatro medios, borradores por DNI, pagos parciales/multiconcepto, confirmación idempotente, conciliación manual, reversión append-only e historial Admin/propietario están cubiertos por unitarios, migración y regresión API integrada.
- M11 MVP completado: recibo automático por pago confirmado, correlativo global, snapshot, PDF/SHA-256 reintentable, historial inmutable, descarga autorizada y backup coordinado están cubiertos por unitarios, migración y regresión API integrada.

Modelo académico relevante:

```text
User (Alumno) 1--1 Student
                    `-- StudentCareer (una membresía por carrera)
                          |-- StudentStudyPlan
                          |-- StudentAcademicAssignment
                          `-- Enrollment

User (Profesor) 1--1 Teacher
                      |-- TeacherDocument (versiones append-only por tipo)
                      `-- TeacherAssignment --> TeachingPosition --> Course + Commission + ciclo

AttendanceSession --> Course + Commission + ciclo + TeachingPosition de origen
       |-- AttendanceRecord --> Enrollment + Student
       |       `-- AttendanceJustification (append-only)
       `-- AttendanceSessionReopening (auditoría append-only)

Gradebook --> Course + Commission + ciclo + TeachingPosition
     |-- GradebookEvaluation
     |     `-- GradeEntryRevision --> Enrollment + Student
     `-- GradebookReopening (auditoría append-only)

ExamTable --> Course + fecha + llamado
     |-- ExamTribunalMember --> Teacher
     |-- ExamRegistration --> Enrollment + Student
     |       `-- ExamGradeRevision (versiones append-only)
     `-- ExamTableReopening (auditoría append-only)

CertificateRequest --> User + StudentCareer + tipo/estado/revisión
       `-- CertificateIssuance (ledger inmutable) --> snapshot + PDF/SHA-256 + emisor
CertificateSequence(1) --> correlativo global y orden raíz de locks

BillingPlan --> Career + BillingPlanItem --> FinancialConcept + vencimiento
FinancialRate --> FinancialConcept + Career + ciclo + condición opcional
FinancialBenefit --> descuento/beca + vigencia + filtros
DebtGenerationBatch --> BillingPlan + Idempotency-Key
       `-- StudentDebt --> Student + StudentCareer + BillingPlanItem + snapshot inmutable

Payment --> Student + PaymentMethod + estado/auditoría
     |-- PaymentAllocation --> StudentDebt
     |-- PaymentReconciliation (append-only, sólo transferencia)
     |-- PaymentReversal (movimiento compensatorio append-only)
     `-- Receipt (ledger inmutable) --> snapshot + PDF/SHA-256 + emisor
ReceiptSequence(1) --> correlativo global REC y lock transaccional
```

`Student.CareerId` conserva la carrera principal; `StudentCareers` es la fuente de membresías activas multi-carrera. Planes, asignaciones e inscripciones deben operar mediante la membresía correcta y preservar atomicidad.

## Convenciones para cambios

- Usar async de extremo a extremo y propagar `CancellationToken` cuando la firma lo permita.
- Registrar todo handler/use case nuevo en `Program.cs`; registrar repositorios y servicios de infraestructura en `DependencyInjection.cs`.
- Para una entidad persistente nueva, actualizar en conjunto entidad, `DbSet`, configuración EF, repositorio/puerto si corresponde y migración.
- No editar manualmente archivos `*.Designer.cs` ni `AppDbContextModelSnapshot.cs`; generarlos con EF Core.
- Conservar el formato de errores existente mientras no se haga un refactor explícito: el middleware produce `{ success: false, msg }`, pero algunos controllers aún usan `ProblemDetails`.
- No asumir que el candado global de Swagger implica autorización. La seguridad real se aplica principalmente en controllers y `ActiveSessionMiddleware`; el proyecto no usa el pipeline estándar `UseAuthentication()`/`UseAuthorization()`.
- No incluir secretos reales. `appsettings.Development.json` contiene credenciales sólo para desarrollo local; producción debe usar variables `ConnectionStrings__DefaultConnection` y `Jwt__SecretKey`.
- La imagen productiva restaura y publica sólo `AcademiaDigital.API.csproj`; mantener `bin/`, `obj/`, tests, documentación y archivos de entorno fuera del contexto mediante `.dockerignore` para no mezclar artefactos del host con el build Linux.
- No modificar ni descartar cambios no relacionados del frontend o de otros colaboradores.

## Base de datos y migraciones

La API ejecuta `Database.MigrateAsync()` al arrancar y reintenta mientras SQL Server de Docker inicia. Existe un fallback heredado que, ante el mensaje `already an object named`, ejecuta `EnsureDeletedAsync()` y recrea la base. Tratarlo como un riesgo: no provocar ni ampliar ese comportamiento sin una decisión explícita, especialmente contra datos no descartables.

Comandos desde `backend-dotnet/`:

```powershell
dotnet ef migrations add <Nombre> --project src/AcademiaDigital.Infrastructure --startup-project src/AcademiaDigital.API
dotnet ef database update --project src/AcademiaDigital.Infrastructure --startup-project src/AcademiaDigital.API
dotnet ef migrations list --project src/AcademiaDigital.Infrastructure --startup-project src/AcademiaDigital.API
```

Usar `dotnet-ef` **8.x** (el README fija 8.0.13). Revisar siempre el SQL/model snapshot generado antes de dar una migración por válida.

## Verificación mínima

Desde `backend-dotnet/`:

```powershell
dotnet test tests/AcademiaDigital.Domain.UnitTests/AcademiaDigital.Domain.UnitTests.csproj -c Release
dotnet test tests/AcademiaDigital.Application.UnitTests/AcademiaDigital.Application.UnitTests.csproj -c Release
dotnet test tests/AcademiaDigital.ArchitectureTests/AcademiaDigital.ArchitectureTests.csproj -c Debug
dotnet build AcademiaDigital.sln
dotnet build AcademiaDigital.sln -c Release
```

Si cambian contratos, persistencia o reglas de negocio, validar además la suite de API:

```powershell
cd tests/api-regression
npm.cmd run typecheck
npm.cmd run test:api:smoke
# o npm.cmd run test:api para la regresión completa
```

La suite completa requiere su `.env`, Docker y una base descartable. `npm.cmd run e2e:reset` y `docker compose down -v` eliminan volúmenes/datos: sólo ejecutarlos cuando el alcance destructivo esté confirmado. Para detalles, leer `tests/api-regression/README.md`.

## Deudas y riesgos conocidos

- Careers, Courses, StudyPlans y StudyPlanCourses están expuestos sin control de sesión, aunque Swagger muestre Bearer global.
- Enrollment ya valida el plan vigente, correlativas estrictas, materias aprobadas/en curso y cupos concurrentes por turno; todavía no expone advertencias `Soft` en la respuesta de inscripción.
- El challenge de admisión usa `Disabled` por defecto para compatibilidad local. Producción debe configurar `AdmissionAntiAbuse__Challenge__Mode=Turnstile` y `AdmissionAntiAbuse__Challenge__Secret`; `StaticToken` se reserva para E2E o entornos controlados. Si existe reverse proxy, debe establecerse una política confiable de forwarded headers antes de usar la IP original.
- Los valores de turno no están normalizados: Commission usa inglés y Enrollment usa español.
- Swagger no describe con precisión todos los guards ni todos los contratos de respuesta/error.
- El adaptador actual de notificaciones de admisión escribe un artefacto local idempotente; todavía debe reemplazarse/configurarse un proveedor real para email. El procesamiento outbox se dispara explícitamente por un endpoint Admin y requiere un worker programado para operación autónoma.
- El almacenamiento local de acuerdos, certificados y recibos es efímero si no se configura `AdmissionStorage__RootPath` sobre un volumen persistente; comparten el adaptador `IFileStorage`. El compose productivo ya monta `backend_files`, pero despliegues alternativos deben proveer persistencia equivalente y backup coordinado.
- La cobertura funcional .NET tiene 291 unit tests sobre M4–M11; todavía no hay integration tests .NET dedicados de persistencia fuera de la regresión API/SQL Server, que pasa 34/34 con M11 incluido.
- Swagger está habilitado también en Production.
- `StudentManagementService.cs` concentra muchas responsabilidades y merece separación gradual, acompañada por pruebas.
- Varios repositorios llaman `SaveChangesAsync` internamente; al crear flujos multi-entidad, comprobar límites transaccionales y evitar persistencia parcial.

No corregir estas deudas incidentalmente dentro de una tarea no relacionada. Si un cambio las afecta, documentar el alcance y agregar una prueba de regresión.

## Últimos cambios relevantes

- **2026-08-24 — guía funcional M4–M11:** se consolidaron funcionalidades, actores, casos de uso, estados, API disponible, pantallas sugeridas, circuitos integrados y criterios de aceptación para frontend. El relevamiento Angular deja explícita la integración parcial de M8, el placeholder M5 y la brecha de los demás módulos. Se verificaron formato, enlaces y `git diff --check`; no se modificó código ni se reejecutaron suites para este cambio documental.
- **2026-08-24 — cierre MVP M11 recibos:** se agregó emisión automática vinculada al pago, correlativo global serializado, snapshot inmutable, PDF/SHA-256 reintentable, historial/descarga Admin o propietario, campos fiscales opcionales, volumen durable y backup coordinado. La migración es `AddDigitalReceipts`; pasaron 291/291 unit tests, build Release, ArchUnitNET 8/8, modelo EF, typecheck, migración/backfill M1–M11, M11 1/1 y regresión 34/34. Swagger confirmó 196 operaciones y Allure fue generado. M4–M11 quedan cerrados y el roadmap continúa en estabilización/release.
- **2026-08-24 — cierre MVP M10 pagos:** se agregaron cuatro medios, borradores por DNI, imputación parcial/multiconcepto, confirmación idempotente, conciliación manual de transferencias, locks serializables, reversión compensatoria e historial aislado. La migración es `AddPaymentsAndReconciliation`; pasaron 278/278 unit tests, build Release, ArchUnitNET 8/8, modelo EF, typecheck, migración/backfill M1–M10, M10 1/1 y regresión 33/33. Swagger confirmó 191 operaciones y Allure fue generado. M10 queda cerrado y el roadmap continúa en M11.
- **2026-08-24 — cierre MVP M9 cobros y conceptos:** se agregaron configuración financiera, planes/cuotas, cálculo ARS con recargo y mejor beneficio, deuda/snapshot inmutables, generación masiva idempotente con orden fijo de locks y consultas aisladas. La migración es `AddFinanceConceptsPlansAndDebts`; pasaron 256/256 unit tests, build Release, ArchUnitNET 8/8, modelo EF, typecheck, migración/backfill M1–M9, M9 1/1 y regresión 32/32. Swagger confirmó 184 operaciones y Allure fue generado. M9 queda cerrado y el roadmap continúa en M10.
- **2026-08-22 — cierre MVP M8 certificados:** se agregaron siete tipos normalizados, elegibilidad por carrera, revisión Admin, correlativo global con orden de locks concurrente, snapshot inmutable, PDF/SHA-256 reintentable, historial y descarga Admin/propietario; el filtro público conserva sólo `Pending/Approved/Rejected`. La migración es `AddCertificateIssuanceModule`; pasaron 236/236 unit tests, migración/backfill M1–M8, modelo EF sin cambios pendientes, build Release, ArchUnitNET 8/8, M8 1/1 y regresión 31/31. Swagger queda en 171 operaciones, 142 con aserción directa. M8 queda cerrado y el roadmap continúa en M9.
- **2026-08-22 — cierre MVP M7 calificaciones y mesas:** se agregaron planillas ponderadas con revisiones append-only, workflow docente–Secretaría, publicación/cierre académico, mesas con tribunal, inscripción regularizada, llamados automáticos, actas versionadas y rectificación compensatoria. La migración es `AddGradebooksAndExamTables`; pasaron 210/210 unit tests, migración M1–M7, modelo EF sin cambios pendientes, ArchUnitNET 8/8, M7 1/1 y regresión 30/30. Swagger queda en 165 operaciones, 133 con aserción directa. M7 queda cerrado y el roadmap continúa en M8.
- **2026-08-22 — cierre MVP M6 asistencias:** se agregaron sesiones por hora/día, roster por comisión, carga masiva idempotente, cálculo ponderado y riesgo, justificaciones Admin append-only, cierre/ventana de 48 horas, reapertura auditada, consultas aisladas y exportación CSV/PDF. La migración es `AddAttendanceModule`; pasaron 192/192 unit tests, migración completa, modelo EF sin cambios pendientes, ArchUnitNET 8/8, M6 1/1 y regresión 29/29. Swagger queda en 146 operaciones, 114 con aserción directa. M6 queda cerrado y el roadmap continúa en M7.
- **2026-08-22 — cierre MVP M5 cargos y asignaciones:** `TeachingPosition` incorpora comisión, ciclo, vacante y baja lógica; `TeacherAssignment` conserva designaciones append-only y sincroniza el docente vigente bajo transacción serializable. Se agregaron nueve operaciones Admin/Profesor, aislamiento de `/teachers/me/assignments` y backfill de cargos heredados. La migración pendiente consolidada es `AddTeacherDocumentsPositionsAndAssignments`; pasaron 176/176 unit tests, migración/backfill, ArchUnitNET 8/8, M5 3/3 y regresión 28/28. Swagger queda en 136 operaciones, 104 con aserción directa. M5 queda cerrado y el roadmap continúa en M6.
- **2026-08-22 — M5 documentación docente versionada:** se agregaron alta, listado y revisión Admin de documentos separados del dominio estudiantil, con formatos/tamaños controlados, auditoría y expiración de versiones anteriores bajo transacción serializable. El esquema documental se consolidó luego en la migración final M5; en este corte pasaron 166/166 unit tests, migración/backfill, ArchUnitNET 8/8, M5 2/2 y regresión 27/27. Swagger quedó en 127 operaciones, 95 con aserción directa.
- **2026-08-22 — M5 legajo docente y baja lógica:** se agregó CRUD Admin de docentes, vínculo único a usuarios Profesor, domicilio/emergencia y baja lógica idempotente auditada. La migración `AddTeacherProfilesAndSoftDelete` preserva columnas heredadas y refuerza `UserId` único; pasaron 157/157 unit tests, migración/backfill, ArchUnitNET 8/8, M5 1/1 y regresión 26/26. Swagger queda en 124 operaciones, 92 con aserción directa.
- **2026-08-22 — cierre MVP M4 antiabuso:** el alta pública incorpora rate limiting por IP con 429/`Retry-After` y challenge configurable `Disabled`/`StaticToken`/Turnstile, validado antes de persistencia y con configuración fail-fast. Pasaron 147/147 unit tests, migración/backfill, ArchUnitNET 8/8, M4 7/7 y regresión 25/25. M4 queda cerrado y el roadmap continúa en M5.
- **2026-08-22 — M4 permisos y cupos de EnrollmentPeriods:** las nueve operaciones administrativas exigen Admin con cobertura 401/403; la inscripción y actualización de cupos bloquean el período y serializan el conteo por turno para impedir sobreventa o reducción bajo ocupación. Pasaron 146/146 unit tests, migración/backfill, ArchUnitNET 8/8, M4 7/7 y regresión 25/25. Swagger permanece en 119 operaciones, 87 con aserción directa.
- **2026-08-22 — M4 acuerdos PDF y outbox durable:** confirmar una postulación persiste atómicamente un snapshot inmutable y un mensaje deduplicado; un procesador Admin reintentable genera el PDF, valida SHA-256, guarda por clave lógica y emite una notificación local idempotente fuera de la transacción. La migración es `AddAdmissionAgreementsAndOutbox`; pasaron 135/135 unit tests, migración/backfill, ArchUnitNET 8/8, M4 6/6 y regresión 24/24. Swagger queda en 119 operaciones, 80 con aserción directa.
- **2026-08-22 — M4 correlativas y documentación de admisión:** el alta de inscripción aplica el plan vigente y correlativas estrictas; las postulaciones incorporan documentos versionados y revisión Admin, y `Confirmed` exige todos los requisitos vigentes aprobados. La migración es `AddAdmissionApplicationDocuments`; pasaron 129/129 unit tests, migración/backfill, ArchUnitNET 8/8, M4 6/6 y regresión 24/24. Swagger queda en 116 operaciones, 77 con aserción directa.
- **2026-08-22 — M4 rematriculación:** se agregó renovación Admin al ciclo lectivo inmediato siguiente con validación de estudiante, membresía, plan y comisión, reemplazo transaccional de la asignación vigente e historial append-only único por carrera/ciclo. La migración es `AddStudentRematriculations`; pasaron 114/114 unit tests, migración/backfill, ArchUnitNET 8/8, M4 6/6 y regresión 24/24. Swagger queda en 113 operaciones, 73 con aserción directa.
- **2026-08-22 — M4 cupo por comisión/turno:** el formulario puede dirigirse a una comisión activa compatible; ese destino exige y conserva capacidad explícita y es único para preservar un solo pool, mientras `CommissionId = null` mantiene compatibilidad legacy. La migración es `LinkAdmissionFormsToCommissions`; pasaron 100/100 unit tests, migración/backfill, ArchUnitNET 8/8, M4 5/5 y regresión 23/23. Swagger permanece en 112 operaciones, 72 con aserción directa.
- **2026-08-22 — M4 cupo y espera FIFO:** se agregó capacidad opcional por formulario, asignación serializable con lock por formulario, espera sin reserva, expiración y promoción FIFO auditada, más configuración y barrido Admin. La migración es `AddAdmissionCapacityAndWaitlist`; pasaron 92/92 unit tests, ArchUnitNET 8/8, M4 4/4 y regresión 22/22. Swagger queda en 112 operaciones, 72 con aserción directa.
- **2026-08-22 — M4 administración de admisiones:** se agregaron seis endpoints Admin para formularios y solicitudes, filtros, transiciones validadas, concurrencia optimista e historial append-only con actor/motivo y backfill. La migración es `AddAdmissionApplicationStatusHistory`; pasaron 75/75 unit tests, ArchUnitNET 8/8, M4 3/3 y regresión total 21/21. Swagger queda en 110 operaciones, 70 con aserción directa.
- **2026-08-22 — M4 admisión pública:** se agregaron formulario configurable por `slug`, solicitud pública `PreEnrolled`, términos, reserva de 72 horas configurable, unicidad concurrente por email/DNI y la migración `AddAdmissionFormsAndApplications`. Pasaron 52/52 unit tests, migración/backfill, API M4 y regresión completa 20/20; se inventariaron las 104 operaciones Swagger en la matriz endpoint–permiso–test.
- **2026-08-22 — inicio de Etapa 0:** se agregaron 37 unit tests (29 Domain y 8 Application), el check CI `Unit Tests` y un baseline Docker E2E reproducible. Pasaron Unit Tests 37/37, migración/backfill, API Regression 19/19, generación Allure y ArchUnitNET 8/8; se corrigió el empaquetado Docker para aislar artefactos del host.
- **2026-08-22 — reporte de calidad:** se documentó la matriz AGENTS/CLAUDE → unitarios → API Regression → ArchUnitNET, incluyendo herramientas, cobertura, evidencia y brechas. Se verificaron enlaces, formato y `git diff --check`; no se reejecutaron suites para este cambio documental.
- **2026-08-22 — gate ArchUnitNET:** se agregó un proyecto xUnit v3 con 8 reglas de dependencias, controllers y ciclos, más el workflow independiente `Backend Architecture`; suite Debug 8/8 y builds Debug/Release verificados.
- **2026-08-22 — controles anti-deriva:** se formalizaron responsabilidades por capa, vertical slice canónica, límites transaccionales, contratos HTTP, excepciones heredadas y checklist de validación arquitectónica.
- **2026-08-22 — roadmap M4–M11:** se documentaron diagnóstico, etapas, contratos previstos, estrategia de automatización y criterios de cierre. M1–M3 quedan como baseline funcional y el trabajo continúa desde M4.
- **2026-08-22 — documentación operativa:** se relevó el backend y se creó este archivo. Builds Debug/Release y typecheck verificados; no se ejecutó la regresión Docker.
- **2026-08-01 — regresión API:** se agregó la suite Playwright/TypeScript/Zod/Allure, entornos Docker E2E, validación de migración/backfill y flujos P0/P1.
- **2026-08-01 — estudiantes multi-carrera:** se incorporó `StudentCareer`, relaciones por membresía, altas atómicas y la migración `AddStudentCareersAndAtomicity`.
- **2026-07-27/28 — módulo 3:** estado e historial estudiantil, comisiones, asignaciones, documentos, becas, campos personalizados y columnas de perfil faltantes.
- **2026-07-02 — académico:** períodos de inscripción, cupos, anualidad de materias y eventos de calendario.

## Mantenimiento obligatorio de este archivo

Al finalizar cualquier tarea que cambie el backend, actualizar este `AGENTS.md` en el mismo cambio:

1. Cambiar la fecha de `Estado actual`.
2. Ajustar arquitectura, módulos, migración más reciente, comandos o riesgos si dejaron de ser ciertos.
3. Agregar una entrada breve al inicio de `Últimos cambios relevantes` con fecha, resultado y verificaciones ejecutadas.
4. Conservar sólo las entradas que aporten contexto vigente; resumir o eliminar historial obsoleto para que el archivo siga siendo corto y útil.
5. No afirmar que una prueba pasó si no se ejecutó en esa tarea; diferenciar claramente evidencia actual de resultados históricos.
6. Actualizar reglas o excepciones arquitectónicas si una decisión aprobada cambia los límites de capas; nunca normalizar una desviación sin documentarla.
