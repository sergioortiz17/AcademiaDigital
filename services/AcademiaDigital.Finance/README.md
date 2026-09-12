# AcademiaDigital.Finance — servicio de finanzas (microservicio)

Único microservicio del sistema (ver
[ADR 0001](../../backend-dotnet/docs/adr/0001-finance-como-servicio-resto-monolito.md)). Gestiona
conceptos financieros, aranceles, beneficios, planes de pago, deudas de alumnos, pagos,
reconciliación, reversos y recibos (compliance AFIP). **Es un sistema de registro y
consulta: NUNCA bloquea el flujo académico del monolito.**

> Estado de este documento: **especificación de diseño** (previo a mover el código desde
> el monolito). Se actualizará con instrucciones exactas de build/run al implementarlo.

## Alcance (entidades)

FinancialConcept, FinancialRate, FinancialBenefit, BillingPlan, BillingPlanItem,
DebtGenerationBatch, StudentDebt, PaymentMethod, Payment, PaymentAllocation,
PaymentReconciliation, PaymentReversal, Receipt, ReceiptSequence.

## Arquitectura

Réplica del estilo de capas del monolito, para que las cohortes vean el mismo patrón:

```
services/AcademiaDigital.Finance/
  src/AcademiaDigital.Finance.Domain/          entidades (ricas donde aplica: Receipt, ReceiptSequence) + enums + policies (FinancePolicy, PaymentPolicy)
  src/AcademiaDigital.Finance.Application/      handlers (Finance, Payments, Receipts) + interfaces de repos + cliente de directorio (nombres)
  src/AcademiaDigital.Finance.Infrastructure/   FinanceDbContext (schema 'finance') + repos + migraciones + cliente HTTP al monolito + generador de PDF de recibos
  src/AcademiaDigital.Finance.API/              controllers + Program.cs
  Dockerfile
docker-compose.finance.yml
```

El código **se mueve** desde el monolito (no se duplica). Tras la extracción, el monolito
ya no tiene las entidades/handlers/repos/config/controller de Finance.

## Base de datos

- Finance usa una **base de datos propia (`academiadigital_finance`) en la misma instancia**
  Postgres que el monolito (`AcademiaDigital`). Base separada = aislamiento real: una
  recreación del esquema del monolito nunca puede tocar los datos de Finance.
- Dentro de su base, las tablas van en el schema `finance` (`HasDefaultSchema("finance")` +
  `MigrationsHistoryTable` en ese schema). La base se crea sola al arrancar (bootstrap
  idempotente contra la base de mantenimiento `postgres`).
- `FinanceDbContext` propio + migraciones propias (`InitialCreate`).
- **Sin migración de datos**: las tablas de Finance estaban vacías (verificado); solo se
  re-siembran los **4 PaymentMethods** de catálogo.
- Migrable a instancia separada en el futuro cambiando solo la connection string.

### ⚠️ Corte de navegaciones EF cruzadas

Estas entidades hoy (en el monolito) tienen navegaciones EF hacia entidades que se quedan
en el monolito. Al mover a Finance se **elimina la navegación y su relación EF**, dejando
solo el id escalar (que ya existe en todos los casos):

| Entidad Finance | Navegación a eliminar | Id que queda |
|-----------------|----------------------|--------------|
| Payment | Student, CreatedByUser, ConfirmationRequestedByUser, ConfirmedByUser | StudentId, CreatedByUserId, ConfirmationRequestedByUserId, ConfirmedByUserId |
| PaymentReconciliation | CreatedByUser | CreatedByUserId |
| PaymentReversal | CreatedByUser | CreatedByUserId |
| Receipt | IssuedByUser | IssuedByUserId |
| FinancialRate | Career | CareerId |
| BillingPlan | Career, CreatedByUser | CareerId, CreatedByUserId |
| DebtGenerationBatch | GeneratedByUser | GeneratedByUserId |
| StudentDebt | Student, StudentCareer | StudentId, StudentCareerId |

**Se conservan** las navegaciones **internas** de Finance (viven en el mismo servicio):
`Payment.PaymentMethod`, `Payment.Allocations/Reconciliations/Reversals/Receipt`,
`PaymentAllocation.StudentDebt/Payment`, `Receipt.Payment`.

## Contrato HTTP

### Finance expone (lo consume el monolito)

```
POST /api/v1/finance/debts/generate
  body: { studentId, careerId, studentCareerId, billingPlanId, academicYear }
  → genera las deudas del alumno para esa carrera/año (registro contable).
  200: { batchId, generatedDebtCount, totalAmount }
  Lo llama el monolito al matricular, FIRE-AND-FORGET: si falla, el monolito NO
  revierte ni bloquea la matriculación (se reintenta/reconciliá después).

GET  /api/v1/finance/students/{studentId}/debts?status=Pending
  → deudas del alumno (informativo, para pantallas).
  200: { studentId, debts: [ { id, concept, dueDate, totalAmount, paidAmount, status } ] }

GET  /api/v1/finance/students/{studentId}/debts/summary
  → resumen liviano { studentId, totalOwed, overdueCount }.
  ⚠️ SOLO informativo. El monolito NO lo usa para condicionar inscripción (decisión de
  negocio: la deuda nunca bloquea el flujo académico).

(+ endpoints internos de Finance ya existentes: métodos de pago, crear/confirmar/
  reconciliar/reversar pagos, emitir/descargar recibos — se mueven tal cual del monolito.)
```

### Finance consume del monolito (solo nombres para mostrar)

```
GET {monolith}/api/v1/users/{userId}/display-name   → { userId, fullName }
GET {monolith}/api/v1/students/{studentId}/display   → { studentId, fullName, legajo }
GET {monolith}/api/v1/careers/{careerId}             → { careerId, name, code }  (ya existe)
```

Implementado como `IDirectoryClient` en Finance.Application, con impl HTTP en
Finance.Infrastructure. **Caché local en memoria (TTL ~5 min)**; si el monolito no
responde, **degrada a mostrar el id** en vez de fallar. Nunca hace lógica sobre estos
datos, solo presentación.

## Comunicación y resiliencia

- monolito→Finance (`/debts/generate`) es fire-and-forget con timeout corto; el error se
  loguea, no se propaga a la matriculación.
- Finance→monolito (nombres) degradado a id ante falla.
- Ninguna transacción cruza servicios. Consistencia eventual aceptada para la deuda.

## Cómo se levanta

```bash
# el monolito + su Postgres ya corriendo (docker-compose.yml principal)
docker compose up -d db backend
# el servicio Finance (reusa la misma instancia Postgres, schema 'finance')
docker compose -f docker-compose.finance.yml up --build
```

- Puerto **8091** (monolito 8000, frontend 4200, dev-tools 8090).
- Al arrancar: crea el schema `finance` (idempotente), aplica su migración `InitialCreate`
  y siembra los 4 `PaymentMethods` de catálogo. Verificado end-to-end.
- **Swagger** http://localhost:8091/swagger · **Scalar** http://localhost:8091/scalar/

### Autenticación entre servicios

Finance es un servicio interno: **no re-autentica JWT**. Confía en la identidad que el
monolito/gateway le reenvía por headers `X-User-Id` y `X-User-Role` en el borde interno.
Los endpoints de admin exigen `X-User-Role: Admin`. `debts/generate` además exige un
header `Idempotency-Key` (el monolito usa `matricula-{studentCareerId}-{academicYear}`).

### Nota sobre generación de deuda en la matriculación

El monolito llama a `POST /debts/generate` de forma **fire-and-forget** al matricular un
alumno (`CreateStudentCommandHandler` → `IFinanceClient`, timeout 5s, errores logueados y
nunca propagados: la matriculación jamás se bloquea). Hoy ese llamado pasa
`billingPlanId = null` porque el alta de alumno todavía no resuelve un plan de facturación;
en ese caso el cliente **loguea y omite** (el seam queda cableado). Para generar deuda real,
un admin debe crear antes un `BillingPlan` en Finance y disparar la generación con ese id.
