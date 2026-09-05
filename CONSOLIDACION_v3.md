# Consolidación `v3`: management + fix de migración + PostgreSQL

Rama `v3` (creada desde `feat/frontend/management`). Integra las tres ramas que
divergían del ancestro común `b4ae3cf` ("Merge pull request #284 from …/modulo_3")
y migra la infraestructura a PostgreSQL.

## Qué se integró de cada rama

### 1. `feat/frontend/management` (base)
Trabajo del equipo de frontend + backend: navegación de Alumno y Profesor, carga de
asistencias/notas/maestros, módulos de Admisiones, Docentes, Asistencia, Planillas
(gradebooks), Certificados, Finanzas, Pagos y Recibos, con sus ~31 migraciones EF
(originalmente para SQL Server) y ~316 archivos que la rama `fix` no tenía.

> **Sobre el diff de ~3.700 inserciones / ~96.000 eliminaciones** entre esta rama y
> `fix`: se investigó y **no es pérdida de trabajo ni un problema de line-endings**.
> Son 316 archivos (en su mayoría `*.Designer.cs` autogenerados de EF + código de los
> módulos nuevos) que existen en `management` y no en `fix`, porque `fix` salió del
> mismo ancestro con un changeset chico. Por eso se mergeó **con `management` como base**
> (los merges traen `fix` y `postgres` hacia adentro), lo que preserva esos 316 archivos.

### 2. `fix/duplicate-user-profile-columns-migration` (4 commits)
- **`071e501`** — fix real de la migración de columnas duplicadas de perfil de usuario.
- **`RAW 2` / `RAW 3`** — pese al nombre, es trabajo real: feature de **import/diff de
  planes de estudio por CSV** (RAW 3 refactoriza y reemplaza el career-import de RAW 2:
  `StudyPlanImport` + `StudyPlanDiff`, backend + componentes Angular `career-create`,
  `career-management`, `study-plan-import`, `study-plan-diff`).
- **`Console logs`** — descartado: eran `console.log` de debug (se eliminaron).

### 3. `feat/migracion-a-postgres-main` (1 commit, `36e9f36`)
Cambio de proveedor SQL Server → PostgreSQL: `UseSqlServer`→`UseNpgsql`, paquete
`Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11`, connection string Npgsql, manejo de
reintentos `PostgresException`/`NpgsqlException` en `Program.cs`, `Scalar.AspNetCore`,
`CsvHelper`, seeds SQL + `SEED_DATA.md`, `DATABASE_SETUP.md` y `docker-compose.yml`
migrados a Postgres, y Configurations con `xmin` como concurrency token.

## Conflictos resueltos y cómo

### Merge de `fix` (commit `9f5eb6c`) — 4 conflictos
| Archivo | Resolución |
|---|---|
| `Migrations/20260728221736_AddMissingUserProfileColumns.cs` | Se dejó la versión **no-op** de management (el raw SQL de SQL Server de `fix` no aplica a Postgres; además las migraciones se regeneran). |
| `core/interceptors/auth.interceptor.ts` | Se mantuvo la estructura de management (retry + parche temporal); el fix de fondo se hizo aparte. |
| `features/admin/admin-routing.module.ts` | **UNIÓN** de rutas (attendance/teacher/gradebook de management + careers de `fix`). |
| `features/admin/admin.module.ts` | **UNIÓN** de imports y `declarations` de ambos lados. |

### Merge de `postgres` (commit `87eb052`) — 6 conflictos
| Archivo | Resolución |
|---|---|
| `API/Program.cs` | **UNIÓN** de usings (`System.Threading.RateLimiting` + `Npgsql`) y del bloque de arranque (resolve de `IAdmissionChallengeVerifier` de management + lógica de reintento Npgsql de postgres). |
| `interceptor` / `admin-routing.module.ts` / `admin.module.ts` | Se tomó `v3` (ya era superconjunto tras el merge de `fix`). |
| `Migrations/` (modify/delete + snapshot) | Se **eliminó toda la carpeta `Migrations/`** y se regeneró (ver abajo). |

## Regeneración de migraciones (decisión clave)

Las **31 migraciones de management estaban escritas para SQL Server** (`nvarchar`,
`datetime2`, `bit`, `SqlServerPropertyBuilderExtensions`) — no compilan ni corren sobre
Npgsql. La rama postgres, por su lado, había reemplazado las ~16 migraciones viejas por
una sola `InitialCreate` que **no conocía los módulos nuevos** de management.

Solución (commit `f954974`): se borró toda la carpeta `Migrations/` y se **regeneró una
única `InitialCreate` para PostgreSQL** desde el modelo consolidado, tras portar los
constructos SQL-Server-only de las Configurations de los módulos de management:

- `IsRowVersion()` → `Ignore(RowVersion)` + `UseXminAsConcurrencyToken()` (AdmissionForm,
  OutboxMessage, AdmissionApplication).
- `HasColumnType("nvarchar(max)")` → `"text"` (×7: Outbox, AdmissionApplication,
  AdmissionAgreement, Receipt, CertificateIssuance, Finance ×2).
- `HasFilter("[col] …")` → sintaxis Postgres sin corchetes; filtros sobre `bool is_current`
  pasan de `[is_current] = 1` a `is_current` (×11 en ExamTable, TeacherAssignment,
  Gradebook, Attendance, Finance, CertificateRequest, Payment, AdmissionForm).
- 3 repositorios (`AdmissionRepository`, `RematriculationRepository`, `TeacherRepository`)
  pasaron de `Microsoft.Data.SqlClient` `SqlException { Number: 2601 or 2627 }` a Npgsql
  `PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }` (23505).

Migración generada: `20260905184721_InitialCreate` — 100% tipos Postgres
(`integer`, `character varying`, `timestamp with time zone`, `boolean`, `text`, `xid`),
0 restos de SQL Server.

> **Nota:** regenerar como una sola `InitialCreate` pierde el historial granular de
> migraciones y la capacidad de migrar "in place" una base SQL Server existente. Para un
> proyecto que arranca de cero sobre Postgres es lo adecuado y coincide con el enfoque que
> ya traía la rama de Postgres.

## Infraestructura Docker (Postgres)

- `docker-compose.yml`: servicio `db` `postgres:16-alpine` con `POSTGRES_USER/PASSWORD/DB`,
  healthcheck `pg_isready`, backend con connection string Npgsql apuntando al host `db` y
  `depends_on: condition: service_healthy`, frontend Angular tras nginx.
- **Dockerfiles**: backend (.NET 8) y frontend (Angular + nginx) son agnósticos de la base
  (la conexión se inyecta por variable de entorno) — no requirieron cambios.
- `.env` (raíz, **gitignored**): actualizado a variables Postgres, eliminado el obsoleto
  `SA_PASSWORD`. Se agregó **`.env.example`** (trackeado) para que
  `cp .env.example .env && docker compose up --build` levante todo end-to-end.

Credenciales de desarrollo/tesis (no inventadas — reutilizadas de la rama Postgres):
`POSTGRES_USER=postgres`, `POSTGRES_PASSWORD=Admin1234!`, `POSTGRES_DB=AcademiaDigital`.
**Cambiar en cualquier entorno real.**

## Fix de fondo del interceptor (commit `45bd37e`)

Reemplaza el parche temporal que comentaba el logout automático ante 401:

- `gradebook-management.component.ts` y `attendance-management.component.ts` implementan
  `OnDestroy` con un `Subject destroy$` y encadenan `takeUntil(this.destroy$)` en **todas**
  las subscripciones HTTP (5 y 6 respectivamente). Al destruirse el componente se cancelan
  las requests en vuelo → ya no llega un 401 tardío tras navegar.
- `auth.interceptor.ts`: se **reactiva** el logout + redirect automático ante 401, ahora
  seguro porque el root cause quedó resuelto.
- Se re-eliminaron los `console.log` de debug que el merge de postgres había reintroducido
  por auto-merge.

## Verificación

- **Backend**: `dotnet build` → *Build succeeded*, 0 errores. `dotnet test` → **299 tests
  OK** (Domain 194, Application 97, Architecture 8), 0 fallidos.
- **Migración**: `dotnet ef migrations add InitialCreate` OK, 100% Postgres.
- **Frontend**: `npm ci --legacy-peer-deps` + `npm run build --configuration=production`
  → *Application bundle generation complete*, 0 errores (solo warnings de budget preexistentes).
- **Docker**: `docker compose config -q` válido e interpolado OK.
- **Sin marcadores de conflicto** en el código fuente; working tree limpio.

### Advertencias benignas conocidas
- `UseXminAsConcurrencyToken` marcado obsolete (enfoque elegido por la rama Postgres; funciona).
- `MSB3277` conflicto EF Core Relational 8.0.11 vs 8.0.13 solo en `ArchitectureTests`
  (Npgsql 8.0.11 arrastra EF 8.0.11) — inofensivo, los tests pasan.

## Commits de `v3`
```
45bd37e fix(frontend): cancelacion de subscripciones (takeUntil) + reactivar logout 401
acc7cf8 chore(docker): documentar variables de entorno para Postgres (.env.example)
f954974 fix(backend): portar modelo y repos a PostgreSQL + regenerar InitialCreate
87eb052 merge: integrar feat/migracion-a-postgres-main en v3 (SQL Server -> PostgreSQL)
9f5eb6c merge: integrar fix/duplicate-user-profile-columns-migration en v3
0351b69 chore(v3): snapshot parche temporal interceptor 401 antes de consolidar
```

## Pendiente de confirmar
- Las credenciales de la base son las de la rama Postgres (dev/tesis). Definir valores
  reales/secretos para cualquier entorno compartido o productivo.
