# AcademiaDigital — Levantar PostgreSQL localmente (sin Docker completo)

El proyecto usa **PostgreSQL 16** como base de datos. Esta guía explica cómo dejarlo corriendo en tu máquina local para poder desarrollar sin necesitar levantar todo el `docker-compose`.

---

## Opciones disponibles

| Opción | Requisito | Recomendado para |
|--------|-----------|-----------------|
| [A) Docker solo para PostgreSQL](#opción-a-docker-solo-para-postgresql-recomendado) | Docker instalado | La mayoría de los casos — más simple y limpio |
| [B) PostgreSQL nativo en Linux](#opción-b-postgresql-nativo-en-linux-ubuntu--debian) | Ubuntu/Debian 20.04+ | Si no querés usar Docker en absoluto |
| [C) PostgreSQL nativo en Windows](#opción-c-postgresql-nativo-en-windows) | Windows 10/11 | Desarrolladores en Windows |

---

## Opción A: Docker solo para PostgreSQL (recomendado)

Levantás **únicamente el contenedor de la base de datos**, sin el backend ni el frontend. Docker no tiene que estar en "Docker Desktop", alcanza con tener el daemon corriendo.

### 1. Levantar el contenedor

```bash
docker run -d \
  --name postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=Admin1234! \
  -e POSTGRES_DB=AcademiaDigital \
  -p 5432:5432 \
  postgres:16-alpine
```

> La primera vez descarga la imagen (~90 MB), tarda pocos segundos.

### 2. Verificar que está corriendo

```bash
docker ps --filter name=postgres
```

Esperá unos segundos hasta que el estado sea `healthy` o simplemente `Up`. Podés revisar los logs con:

```bash
docker logs postgres
```

Cuando veas la línea `database system is ready to accept connections`, está listo.

### 3. Comandos útiles del contenedor

```bash
# Detener sin borrar datos
docker stop postgres

# Volver a iniciar
docker start postgres

# Borrar el contenedor (los datos se pierden)
docker rm -f postgres

# Ver logs en tiempo real
docker logs -f postgres
```

> Los datos **no persisten** si hacés `docker rm`. Para guardarlos entre sesiones usá un volumen:
> ```bash
> docker run -d \
>   --name postgres \
>   -e POSTGRES_USER=postgres \
>   -e POSTGRES_PASSWORD=Admin1234! \
>   -e POSTGRES_DB=AcademiaDigital \
>   -p 5432:5432 \
>   -v postgres_data:/var/lib/postgresql/data \
>   postgres:16-alpine
> ```

---

## Opción B: PostgreSQL nativo en Linux (Ubuntu / Debian)

Si no querés usar Docker, podés instalar PostgreSQL directamente desde los repos oficiales de la distro.

### 1. Instalar PostgreSQL 16

```bash
sudo apt-get update
sudo apt-get install -y postgresql postgresql-contrib
```

> Si tu distro trae por defecto una versión distinta a la 16, podés agregar el repo oficial de PostgreSQL (`apt.postgresql.org`) para instalar la 16 exacta — no es estrictamente necesario para desarrollo, cualquier PostgreSQL 14+ sirve.

### 2. Configurar el usuario y la base

```bash
sudo -u postgres psql -c "ALTER USER postgres PASSWORD 'Admin1234!';"
sudo -u postgres createdb AcademiaDigital
```

### 3. Verificar que el servicio esté corriendo

```bash
systemctl status postgresql
```

Si no está corriendo:

```bash
sudo systemctl start postgresql
sudo systemctl enable postgresql  # para que arranque solo al iniciar el sistema
```

### 4. Habilitar conexiones por password (si hace falta)

Por defecto algunas instalaciones de PostgreSQL en Linux usan autenticación `peer` para conexiones locales. Si `psql -U postgres` te pide password y falla, editá `pg_hba.conf` (la ruta típica es `/etc/postgresql/16/main/pg_hba.conf`) y cambiá el método `peer`/`ident` de las líneas `local` a `md5`, después reiniciá el servicio:

```bash
sudo systemctl restart postgresql
```

---

## Opción C: PostgreSQL nativo en Windows

### 1. Descargar el instalador

Descargá el instalador desde la página oficial:

**https://www.postgresql.org/download/windows/**

Elegí la versión **16.x**.

### 2. Instalar PostgreSQL

1. Ejecutá el instalador descargado
2. Dejá los componentes por defecto (incluye pgAdmin 4, útil para inspeccionar la base visualmente)
3. Cuando pida la contraseña del superusuario `postgres`, ingresá `Admin1234!` para que coincida con `appsettings.Development.json`
4. Dejá el puerto por defecto `5432`
5. Finalizá la instalación (no hace falta correr Stack Builder al final)

### 3. Verificar la conexión

Abrí PowerShell o CMD y ejecutá (necesitás tener `psql` en el PATH, lo instala el paquete oficial en `C:\Program Files\PostgreSQL\16\bin`):

```powershell
psql -h localhost -U postgres -d postgres -c "SELECT version();"
```

Te va a pedir la contraseña (`Admin1234!`).

### 4. Crear la base de datos

```powershell
psql -h localhost -U postgres -c "CREATE DATABASE ""AcademiaDigital"";"
```

### 5. Abrir el firewall (si es necesario)

```powershell
# Ejecutar como Administrador
New-NetFirewallRule -DisplayName "PostgreSQL 5432" `
  -Direction Inbound -Protocol TCP -LocalPort 5432 -Action Allow
```

---

## Verificar la conexión a PostgreSQL

Una vez que tenés PostgreSQL corriendo (por cualquiera de las tres opciones), podés verificar la conexión con `psql`.

### Instalar el cliente `psql` (si no lo tenés, para probar desde fuera del contenedor)

```bash
sudo apt-get install -y postgresql-client
```

### Probar la conexión

```bash
PGPASSWORD=Admin1234! psql -h localhost -p 5432 -U postgres -d AcademiaDigital -c "SELECT version();"
```

Si devuelve la versión de PostgreSQL, la conexión funciona correctamente.

> Con el contenedor de Docker Compose de este repo (servicio `db`, container_name `postgres`) también podés conectarte sin instalar nada localmente, ejecutando `psql` **dentro** del contenedor:
> ```bash
> docker exec -it postgres psql -U postgres -d AcademiaDigital
> ```

---

## Crear la base de datos con EF Core

Una vez que PostgreSQL está corriendo, desde la carpeta `backend-dotnet/` ejecutá:

### Primera vez (crear las tablas)

```bash
cd backend-dotnet

# Instalar la herramienta EF en la versión correcta para .NET 8
dotnet tool install --global dotnet-ef --version "8.0.13"
export PATH="$PATH:$HOME/.dotnet/tools"

# (Este repo ya trae un manifiesto de herramientas local — alcanza con
# "dotnet tool restore" si preferís no instalar dotnet-ef global)
dotnet tool restore

# Crear la migración inicial (si no existe la carpeta Migrations/)
dotnet ef migrations add InitialCreate \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API

# Aplicar la migración — crea todas las tablas en la DB "AcademiaDigital"
dotnet ef database update \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

> En la práctica no hace falta correr `database update` a mano: `Program.cs` corre `db.Database.MigrateAsync()` automáticamente al iniciar la API (con reintentos acotados por si Postgres todavía no acepta conexiones).

### Verificar que las tablas se crearon

```bash
PGPASSWORD=Admin1234! psql -h localhost -p 5432 -U postgres -d AcademiaDigital -c "\dt"
```

o, contra el contenedor de Docker Compose:

```bash
docker exec -it postgres psql -U postgres -d AcademiaDigital -c "\dt"
```

---

## Connection string usada en desarrollo

El archivo [backend-dotnet/src/AcademiaDigital.API/appsettings.Development.json](backend-dotnet/src/AcademiaDigital.API/appsettings.Development.json) ya tiene configurado:

```
Host=localhost;Port=5432;Database=AcademiaDigital;Username=postgres;Password=Admin1234!;
```

No hace falta modificar nada si usaste `Admin1234!` como contraseña al configurar PostgreSQL.

Con `docker compose` (`docker-compose.yml` en la raíz del repo), el backend recibe en cambio `ConnectionStrings__DefaultConnection` armada a partir de las variables de entorno `POSTGRES_USER`, `POSTGRES_PASSWORD` y `POSTGRES_DB` definidas en tu `.env` (ver [SEED_DATA.md](SEED_DATA.md) y el `.env` de ejemplo — el archivo real está gitignored).

---

## Troubleshooting

### Error: `Connection refused` / `could not connect to server`

- **Docker (A):** verificá que el contenedor esté corriendo: `docker ps --filter name=postgres`
- **Linux (B):** verificá el servicio: `systemctl status postgresql`
- **Windows (C):** confirmá que el servicio "postgresql-x64-16" esté *Running* en `services.msc`
- Verificá que el puerto 5432 esté escuchando:
  - Linux: `ss -tlnp | grep 5432`
  - Windows (PowerShell): `netstat -an | findstr 5432`

### Error: `password authentication failed for user "postgres"`

- Asegurate de usar exactamente `Admin1234!` como contraseña
- **Linux (B):** si acabás de instalar el paquete y no seteaste la contraseña, corré `sudo -u postgres psql -c "ALTER USER postgres PASSWORD 'Admin1234!';"`
- Revisá el método de autenticación en `pg_hba.conf` (debe ser `md5` o `scram-sha-256`, no `peer`/`ident`, para conexiones desde la app)

### Error al correr `dotnet ef`

- Verificá que tenés instalada la herramienta: `dotnet ef --version`
- Si da "command not found", instalala y agregá las tools al PATH:
  ```bash
  dotnet tool install --global dotnet-ef --version "8.0.13"
  export PATH="$PATH:$HOME/.dotnet/tools"
  ```
  - **Windows (PowerShell):** `$env:PATH += ";$env:USERPROFILE\.dotnet\tools"`
- Este repo también trae un manifiesto local (`backend-dotnet/.config/dotnet-tools.json`): `dotnet tool restore` alcanza sin instalar nada global.

### Error: `dotnet-ef` se instaló en versión 10 pero el proyecto es .NET 8

`dotnet tool install --global dotnet-ef` sin versión instala la última (10.x), que no es compatible con proyectos `net8.0`. La solución es desinstalarla y fijar la versión exacta:

```bash
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version "8.0.13"
```

> En zsh, **no uses `8.*`** — el shell lo interpreta como un glob y falla. Siempre especificá la versión exacta entre comillas.

### Error: `startup project doesn't reference Microsoft.EntityFrameworkCore.Design`

El paquete `Design` está en `AcademiaDigital.Infrastructure` pero EF también lo necesita en el proyecto de startup (`AcademiaDigital.API`). Agregalo fijando la versión 8:

```bash
dotnet add src/AcademiaDigital.API package Microsoft.EntityFrameworkCore.Design --version "8.0.13"
```

### Los scripts de `scripts/*.sql` fallan con `relation "careers" does not exist` (todo en minúsculas)

Los nombres de tabla del modelo (`Careers`, `Courses`, `StudyPlans`, etc.) son PascalCase. PostgreSQL pliega a minúsculas cualquier identificador sin comillas, así que hace falta escribir `"Careers"` (con comillas dobles) en cualquier SQL manual — los scripts de este repo ya lo hacen. Las columnas en cambio están mapeadas explícitamente en snake_case (`career_id`, `is_active`, etc.) salvo un puñado de tablas nuevas del módulo de gestión de alumnos que no tienen `HasColumnName` explícito y quedan en PascalCase (ej. `"StudentCareers"."StudentId"`) — revisá la migración (`Infrastructure/Migrations/..._InitialCreate.cs`) si tenés dudas sobre el casing real de una columna.

---

## Flujo completo de desarrollo local

```
1. PostgreSQL corriendo (Docker o nativo)
        ↓
2. dotnet ef database update  ← opcional, la API también migra sola al arrancar
        ↓
3. dotnet run --project src/AcademiaDigital.API
        ↓
4. npm start (en frontend-angular/)
        ↓
5. http://localhost:4200
```

Ver también:
- [SEED_DATA.md](SEED_DATA.md) — cómo cargar los datos de ejemplo (carreras, planes de estudio, usuarios demo)
- [backend-dotnet/README.md](backend-dotnet/README.md) — cómo correr la API
- [frontend-angular/README.md](frontend-angular/README.md) — cómo correr el frontend
