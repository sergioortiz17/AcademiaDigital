# AcademiaDigital · DevTools

Panel de administración de datos para **desarrollo/testing**. Aplicación .NET 8
**separada** del backend/frontend reales: reutiliza `Infrastructure` (AppDbContext +
repositorios) y `Application` (use-cases/handlers) del backend vía `ProjectReference`,
sin duplicar lógica. No forma parte de la solución ni de las imágenes de producción.

> ⚠️ **Solo desarrollo.** Ejecuta operaciones destructivas (reset total de la base).
> Tiene salvaguardas para no poder apuntar a producción (ver más abajo).

## Qué hace

1. **Reset completo + reseed** — replica, a nivel de datos, la secuencia de
   `scripts/reset_and_reseed.sh`: `EnsureDeleted` → `Migrate` → carga los 3 seeds
   (`seed_desarrollo_software_2023.sql`, `seed_enfermeria.sql`, `seed_usuarios_demo.sql`)
   **en ese orden**. Pide confirmación explícita (escribir `RESET` en un modal).
2. **Carrera + Plan de estudios (CSV)** — reutiliza `ImportStudyPlanFromCsvCommandHandler`.
   Podés importar un plan a una carrera existente o crear una carrera nueva y luego el plan.
3. **Alta de usuarios (3 roles)** — Alumno reutiliza `RegisterUseCase`
   (crea `User` + `Student` + `StudentCareer`, requiere carrera); Profesor/Admin reutilizan
   `IUserRepository.CreateAsync` (hashea BCrypt igual que el backend). Al crear un
   **Profesor** también se crea su perfil `Teacher` (necesario para poder asignarle materias).
4. **Asignar materia a profesor** — crea/elige una `TeachingPosition` para el curso y
   período, y crea el `TeacherAssignment` vía `AssignTeacherCommandHandler`.
5. **Listado** — carreras, usuarios (con conteo por rol) y asignaciones vigentes.

Puerto **8090** (backend real 8000, frontend real 4200). Sin autenticación (interno).

## Cómo levantarlo

Requiere el Postgres del compose principal corriendo (misma red):

```bash
# 1) Base de datos del proyecto principal
docker compose up -d db

# 2) DevTools (compose separado; NO se levanta con el up normal)
docker compose -f docker-compose.dev-tools.yml up --build
```

Abrí **http://localhost:8090**.

Reutiliza el `.env` de la raíz (`POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB`)
y monta `./seeds` dentro del contenedor. No inventa credenciales.

### Correr localmente sin Docker (opcional)

```bash
cd dev-tools/AcademiaDigital.DevTools
ALLOW_DESTRUCTIVE_DB_TOOLS=true dotnet run
# usa la connstring de appsettings.json (Host=localhost) y ./seeds del repo
```

## Salvaguardas anti-producción

La app **no arranca** salvo que se cumplan **ambas**:

1. `ALLOW_DESTRUCTIVE_DB_TOOLS=true` en el entorno.
2. El `Host` de la connection string sea de dev conocido: `localhost`, `127.0.0.1`,
   `::1` o `db` (nombre del servicio Postgres en docker-compose).

Si falla cualquiera, lanza excepción al inicio (ver `DevToolsSafety.cs`).

## Endpoints (API interna)

| Método | Ruta | Función |
|--------|------|---------|
| GET  | `/api/info` | objetivo (host DB) + advertencia |
| GET  | `/api/overview` | listados (carreras, usuarios, asignaciones) |
| GET  | `/api/careers` | carreras |
| GET  | `/api/careers/{id}/courses` | materias de una carrera |
| GET  | `/api/teachers` | profesores activos |
| POST | `/api/reset` | reset+reseed (body `{ "confirm": true }`) |
| POST | `/api/careers/import-plan` | multipart: `file` CSV + `careerId` **o** `careerCode`/`careerName` |
| POST | `/api/users` | `{ name,lastName,email,password,dni,role,careerId? }` (role: 1=Alumno,2=Profesor,3=Admin) |
| POST | `/api/teacher-assignments` | `{ teacherId, courseId, academicYear, semester }` |

## Notas de diseño

- **No** destruye el volumen Docker (no hace `down -v`): el reset opera sobre el
  `AppDbContext` (`EnsureDeleted`+`Migrate`), lo que deja la base equivalente a un
  reseed sin necesidad de acceso al daemon de Docker desde el contenedor.
- Es un proyecto **standalone**: no está en `backend-dotnet/AcademiaDigital.sln`, así
  nunca se compila ni empaqueta con la build de producción.
