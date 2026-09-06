# Seeds — AcademiaDigital (PostgreSQL)

Scripts SQL para poblar una base de datos recién creada (esquema ya migrado con
`dotnet ef database update` / `MigrateAsync` automático al levantar el backend,
pero sin datos todavía). No se aplican solos — hay que correrlos a mano.

## Requisito previo

El contenedor de Postgres tiene que estar corriendo y el backend ya tiene que
haber aplicado las migraciones (las tablas deben existir). Ver `docker compose
up --build -d` en la raíz del proyecto.

## Orden de ejecución

1. **`seed_desarrollo_software_2023.sql`** — Carrera "Desarrollo de Software"
   (código `DS2023`): 22 materias, plan de estudios y correlatividades.
2. **`seed_enfermeria.sql`** — Carrera "Enfermería" (código `ENF2024`): 16
   materias, plan de estudios y correlatividades.
3. **`seed_usuarios_demo.sql`** — Crea dos usuarios de prueba:
   - **Admin**: `admin@academiadigital.local` / `Admin123!`
   - **Estudiante**: `estudiante@academiadigital.local` / `Estudiante123!`
     (queda inscripto en `DS2023` — por eso este script va **último**, necesita
     que la carrera ya exista).

Los tres scripts son idempotentes: si el dato ya existe (mismo `code` de
carrera o mismo `email` de usuario), no lo duplican.

## Cómo correrlos

Con el contenedor de Postgres levantado (nombre del servicio: `db` en
`docker-compose.yml`; nombre del contenedor puede variar, confirmar con
`docker ps`):

```bash
docker exec -i <contenedor_postgres> psql -U postgres -d AcademiaDigital < seeds/seed_desarrollo_software_2023.sql
docker exec -i <contenedor_postgres> psql -U postgres -d AcademiaDigital < seeds/seed_enfermeria.sql
docker exec -i <contenedor_postgres> psql -U postgres -d AcademiaDigital < seeds/seed_usuarios_demo.sql
```

## Verificar que cargó bien

```bash
docker exec -i <contenedor_postgres> psql -U postgres -d AcademiaDigital -c 'SELECT code, name FROM "Careers";'
docker exec -i <contenedor_postgres> psql -U postgres -d AcademiaDigital -c 'SELECT email, role FROM "Users";'
```

Y probar el login real:

```bash
curl -X POST http://localhost:8000/api/v1/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@academiadigital.local","password":"Admin123!"}'
```

## Nota

Las contraseñas de este README son de **desarrollo/tesis únicamente**, no usar
en un ambiente real. Los hashes dentro de `seed_usuarios_demo.sql` están
generados con BCrypt (mismo algoritmo que usa el backend al registrar un
usuario real), así que el login los valida sin ningún tratamiento especial.
