# AcademiaDigital

> Tesis del Curso Nocturno del **ITSC** (Instituto Tecnológico Superior Córdoba)

![Angular](https://img.shields.io/badge/Angular-21-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp&logoColor=white)
![NgRx](https://img.shields.io/badge/NgRx-Store-BA2BD2?style=for-the-badge&logo=reactivex&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-En_desarrollo-orange?style=for-the-badge)

---

## Equipo

| Frontend | Backend | QA | DevOps | Admin DB | UX/UI | Project Manager |
|----------|---------|-----|--------|----------|-------|-----------------|
| Ivan Maturano | Juan Pedraza | Jazmin Luna | Agos Pereyra | Agustin Cordoba | Gimena Galan | Rodri Leyria |
| Claudia Rodríguez | Anahi Gordillo | Juan Pedraza | Rocio Acosta | Anahi Gordillo | Agos Arce | Sergio Ortiz |
| Gabriel Condori | Rodri Leyria | Agos Pereyra | Nahuel Velez | Sergio Ortiz | Cintia Ramirez | Profe Maria |
| Elias Giralda | Sergio Ortiz | Edgar Rivarola | Jazmin Luna | Rodri Leyria | Facundo Pereyra | Gabriel Molina |
| | Jazmin Luna | | Agos Pereyra | | | |

---

## Stack

| Capa | Tecnología |
|------|-----------|
| Frontend | Angular 21 + NgRx + ngx-translate |
| Backend | ASP.NET Core 8 Web API (Clean Architecture) |
| Base de datos | SQL Server 2022 |
| Contenedores | Docker Compose |

---

## Estructura

```
AcademiaDigital/
├── frontend-angular/   # App Angular 21
├── backend-dotnet/     # API .NET 8
└── docker-compose.yml  # Orquestación local
```

---

## Documentación por servicio

| Servicio | README |
|----------|--------|
| Frontend Angular | [frontend-angular/README.md](frontend-angular/README.md) |
| Backend .NET | [backend-dotnet/README.md](backend-dotnet/README.md) |
| Base de datos (SQL Server local) | [DATABASE_SETUP.md](DATABASE_SETUP.md) |

---

## Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (o Docker Engine + Compose v2)
- Git

> SQL Server 2022 requiere al menos **2 GB de RAM** asignados a Docker.

---

## Levantar localmente

### 1. Clonar y entrar a la carpeta

```bash
git clone https://github.com/sergioortiz17/AcademiaDigital.git
cd AcademiaDigital
```

### 2. Configurar variables de entorno

Crear un archivo `.env` en la raíz del proyecto:

```env
# Contraseña de SQL Server (debe cumplir política: mayúscula, número, símbolo)
SA_PASSWORD=Admin1234!

# Clave secreta para firmar los JWT (mínimo 32 caracteres)
JWT_SECRET_KEY=cambia-esto-por-una-clave-larga-y-segura
```

> El archivo `.env` es leído automáticamente por Docker Compose.  
> **No subas este archivo a git.**

### 3. Levantar los servicios

```bash
docker compose up --build
```

Docker levantará en orden:

1. **SQL Server** — espera a estar healthy (~30 s la primera vez)
2. **Backend .NET** — conecta a SQL Server y levanta en el puerto 8000
3. **Frontend Angular** — se sirve con Nginx en el puerto 4200

### 4. Crear las tablas (primera vez)

```bash
cd backend-dotnet

# Instalar la herramienta EF si no la tenés
dotnet tool install --global dotnet-ef

# Crear la migración inicial (solo la primera vez)
dotnet ef migrations add InitialCreate \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API

# Aplicar al SQL Server
dotnet ef database update \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

---

## URLs

| Servicio | URL |
|----------|-----|
| Frontend Angular | http://localhost:4200 |
| Backend API | http://localhost:8000 |
| Swagger (probar APIs) | http://localhost:8000/swagger |
| SQL Server | localhost:1433 |

---

## Probar la API con Swagger

1. Abrir **http://localhost:8000/swagger**
2. Ejecutar `POST /api/v1/users/register` para crear un usuario
3. Ejecutar `POST /api/v1/users/login` → copiar el campo `token` de la respuesta
4. Click en **Authorize** (candado arriba a la derecha)
5. Pegar el token → **Authorize**
6. Ya podés ejecutar los endpoints protegidos (logout, checkSession, editar usuario)

---

## Detener los servicios

```bash
docker compose down
```

Para también borrar los datos de la base:

```bash
docker compose down -v
```

---

## Enlaces útiles

- [Drive del proyecto ITSC](https://drive.google.com/drive/folders/1gTeaCPzwwIdfQ5e7BksBQUGXjsnhKY7n?usp=drive_link)
- [Issues del proyecto](https://github.com/sergioortiz17/AcademiaDigital/issues)
- [Tablero Kanban](https://github.com/users/sergioortiz17/projects/2)
