# AcademiaDigital — Poblar la base de datos con Carreras, Materias y Usuarios demo

Esta guía explica cómo cargar los datos de dos carreras (Desarrollo de Software 2023 y Enfermería) junto con sus planes de estudio, materias, tipos de curso y correlatividades, más dos usuarios de demo (Admin y Estudiante) — y cómo resetear esos datos si hace falta empezar de cero.

> Requisito previo: la base de datos y las tablas ya deben existir. Si todavía no las creaste, seguí primero [DATABASE_SETUP.md](DATABASE_SETUP.md) (sección "Crear la base de datos con EF Core") — o simplemente levantá `docker compose up -d db backend`, que corre las migraciones solo al arrancar.

---

## Scripts disponibles

| Script | Ubicación | Qué hace |
|--------|-----------|----------|
| `seed_desarrollo_software_2023.sql` | [scripts/seed_desarrollo_software_2023.sql](scripts/seed_desarrollo_software_2023.sql) | Carga 1 Carrera (`"Careers"`), 4 tipos de curso (`"CourseTypes"`), 22 materias (`"Courses"`), 1 plan de estudios (`"StudyPlans"`), 22 relaciones plan-materia (`"StudyPlanCourses"`) y sus correlatividades (`"CoursePrerequisites"`) |
| `reset_desarrollo_software_2023.sql` | [scripts/reset_desarrollo_software_2023.sql](scripts/reset_desarrollo_software_2023.sql) | Borra todo lo cargado por el seed de arriba (busca la carrera por `code = 'DS2023'`, no rompe si ya está vacío) |
| `seed_enfermeria.sql` | [scripts/seed_enfermeria.sql](scripts/seed_enfermeria.sql) | Carga la carrera Enfermería (`code = 'ENF2024'`), 16 materias, 1 plan de estudios y 18 correlatividades, según `PLAN DE ESTUDIO ENFERMERIA.pdf`. **Tiene 2 placeholders**: `code` del plan y `total_credits` (el PDF fuente no los especifica) — revisar antes de usar en un ambiente compartido |
| `reset_enfermeria.sql` | [scripts/reset_enfermeria.sql](scripts/reset_enfermeria.sql) | Borra todo lo cargado por el seed de Enfermería (busca por `code = 'ENF2024'`) |
| `seed_usuarios_demo.sql` | [scripts/seed_usuarios_demo.sql](scripts/seed_usuarios_demo.sql) | Crea un usuario Admin y un usuario Estudiante de demo (ver credenciales más abajo). El estudiante queda inscripto en la carrera `DS2023` — **requiere haber corrido antes** `seed_desarrollo_software_2023.sql` |
| `import_instrumentacion_quirurgica.csv` | [scripts/import_instrumentacion_quirurgica.csv](scripts/import_instrumentacion_quirurgica.csv) | No es un script SQL — se carga desde la UI/API de gestión de carreras (import CSV), no con `psql` |

Todos los scripts `.sql` corren dentro de una transacción (`BEGIN` / `COMMIT`), así que si algo falla no queda data a medias.

> **Nota:** `CourseTypes` se inserta de forma idempotente (`IF NOT EXISTS`) y **no** se borra en el reset — es un catálogo compartido que pueden usar otras carreras. Las carreras (`Careers`, `Courses`, `StudyPlans`, etc.) no son idempotentes: si corrés el seed de una carrera dos veces sin resetear antes, vas a duplicarla. El seed de usuarios demo **sí** es idempotente (`IF NOT EXISTS ... WHERE email = ...`): correrlo de nuevo no duplica nada.

---

## 1. Verificar que PostgreSQL esté corriendo

```bash
docker ps --filter name=postgres
```

Si no está levantado, arrancalo (ver [DATABASE_SETUP.md](DATABASE_SETUP.md)), o con Docker Compose desde la raíz del repo:

```bash
docker compose up -d db
```

## 2. Ejecutar los scripts de seed

Desde la raíz del repo (`AcademiaDigital/`), corriendo `psql` **dentro** del contenedor de PostgreSQL de `docker-compose.yml` (container `postgres`, base `AcademiaDigital`, usuario `postgres`):

```bash
docker exec -i postgres psql -U postgres -d AcademiaDigital < scripts/seed_desarrollo_software_2023.sql
docker exec -i postgres psql -U postgres -d AcademiaDigital < scripts/seed_enfermeria.sql
docker exec -i postgres psql -U postgres -d AcademiaDigital < scripts/seed_usuarios_demo.sql
```

> El usuario `estudiante` necesita que ya exista la carrera `DS2023`, así que corré primero `seed_desarrollo_software_2023.sql` y después `seed_usuarios_demo.sql`.

Si preferís correr `psql` desde el host (necesitás el cliente `postgresql-client` instalado y el puerto 5432 publicado, que ya lo hace `docker-compose.yml`):

```bash
PGPASSWORD=Admin1234! psql -h localhost -p 5432 -U postgres -d AcademiaDigital \
  -f scripts/seed_desarrollo_software_2023.sql
```

## 3. Verificar que los datos se cargaron

```bash
docker exec -i postgres psql -U postgres -d AcademiaDigital -c \
  'SELECT id, name, code, total_credits FROM "Careers";'

docker exec -i postgres psql -U postgres -d AcademiaDigital -c \
  'SELECT count(*) AS materias FROM "Courses";'
```

Deberías ver las carreras **"Desarrollo de Software"** (`DS2023`, 22 materias) y **"Enfermería"** (`ENF2024`, 16 materias) — 38 materias en total si cargaste ambas.

## 4. Verificar el login de los usuarios demo

Credenciales (solo desarrollo/tesis, **no usar en producción**):

| Rol | Email | Password |
|-----|-------|----------|
| Admin | `admin@academiadigital.local` | `Admin123!` |
| Estudiante | `estudiante@academiadigital.local` | `Estudiante123!` |

Con el backend corriendo (`docker compose up -d backend` o `dotnet run`):

```bash
curl -X POST http://localhost:8000/api/v1/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@academiadigital.local","password":"Admin123!"}'

curl -X POST http://localhost:8000/api/v1/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"estudiante@academiadigital.local","password":"Estudiante123!"}'
```

Ambos deberían devolver `{"success":true,"token":"...","user":{...}}` con un JWT válido.

---

## Resetear y volver a cargar

Como los seeds de carreras no son idempotentes, si necesitás recargarlos desde cero corré primero el script de reset y después el seed correspondiente:

```bash
docker exec -i postgres psql -U postgres -d AcademiaDigital < scripts/reset_desarrollo_software_2023.sql
docker exec -i postgres psql -U postgres -d AcademiaDigital < scripts/seed_desarrollo_software_2023.sql
```

El reset busca la carrera por `code = 'DS2023'` (o `'ENF2024'` en el caso de Enfermería), así que no importa qué IDs le haya asignado la base — y no falla si ya está vacía (podés correrlo dos veces seguidas sin problema).

> No hay un script de reset para `seed_usuarios_demo.sql`: si necesitás borrar los usuarios demo, hacelo a mano (`DELETE FROM "Users" WHERE email IN (...)`) teniendo en cuenta las FKs hacia `"Students"` y `"StudentCareers"` para el usuario estudiante.

---

## Agregar una nueva carrera

Para poblar otra carrera, copiá `scripts/seed_desarrollo_software_2023.sql` (y su contraparte de reset) como base, y ajustá:

1. Los datos de `"Careers"` (name, code, description, total_credits, duration_years)
2. La lista de `"Courses"` (una fila por materia, con código propio, ej. `XXYYYY-01`)
3. El `"StudyPlans"` (code, name, effective_from)
4. Las filas de `"StudyPlanCourses"` (year_number, semester, course_type_id, workload_hours)
5. Las `"CoursePrerequisites"` si hay correlatividades
6. En el reset, cambiá el `code` que busca en `"Careers"` por el de la carrera nueva

Correlo igual que el de Desarrollo de Software, con `docker exec -i postgres psql -U postgres -d AcademiaDigital < scripts/<tu-script>.sql`.

> Recordá: los nombres de tabla van entre comillas dobles (`"Careers"`, no `Careers`) porque son PascalCase y PostgreSQL pliega a minúsculas cualquier identificador sin comillas.
