# AcademiaDigital — Backend .NET

![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

API REST construida con **ASP.NET Core 8** siguiendo **Clean Architecture**. Usa **Entity Framework Core 8** con SQL Server y autenticación via **JWT**.

> Antes de levantar el backend necesitás tener SQL Server corriendo localmente. Ver [../DATABASE_SETUP.md](../DATABASE_SETUP.md) para instrucciones completas (con o sin Docker).

---

## Requisitos previos

| Herramienta | Versión | Verificar |
|-------------|---------|-----------|
| .NET SDK | 8.0 | `dotnet --version` |
| SQL Server | 2019+ (o 2022) | instancia local o remota |
| dotnet-ef (CLI) | 8.x | `dotnet ef --version` |

> Instalar la herramienta EF Core en la versión correcta para este proyecto:
> ```bash
> dotnet tool install --global dotnet-ef --version "8.0.13"
> export PATH="$PATH:$HOME/.dotnet/tools"
> ```
> **Importante:** no uses `8.*` en zsh — el shell lo interpreta como un glob y falla. Siempre especificá la versión exacta entre comillas.

---

## Estructura del proyecto

```
backend-dotnet/
└── src/
    ├── AcademiaDigital.API/            # Capa de presentación: controllers, program.cs
    ├── AcademiaDigital.Application/    # Casos de uso, interfaces, DTOs
    ├── AcademiaDigital.Domain/         # Entidades y reglas de negocio
    └── AcademiaDigital.Infrastructure/ # EF Core, repositorios, JWT, servicios externos
```

---

## Configuración

### 1. Connection String y JWT

El archivo [src/AcademiaDigital.API/appsettings.Development.json](src/AcademiaDigital.API/appsettings.Development.json) ya tiene valores listos para desarrollo local:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AcademiaDigital;User Id=sa;Password=Admin1234!;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "dev_secret_key_123_academia_digital",
    "ExpirationDays": "7"
  }
}
```

Si tu instancia de SQL Server usa autenticación de Windows o un puerto distinto, editá la cadena de conexión en ese archivo.

### 2. Configuración de CORS

El archivo [src/AcademiaDigital.API/appsettings.json](src/AcademiaDigital.API/appsettings.json) tiene permitidos por defecto:

- `http://localhost:4200` (frontend Angular en desarrollo)
- `http://localhost:3000`

No hace falta modificar nada para correr localmente junto al frontend.

---

## Levantar el backend

```bash
# Desde la raíz del repositorio
cd backend-dotnet

# Restaurar paquetes NuGet
dotnet restore

# Compilar la solución
dotnet build

# Correr la API en modo Development
dotnet run --project src/AcademiaDigital.API --launch-profile http
```

La API queda disponible en **http://localhost:5073**.  
Swagger UI en **http://localhost:5073/swagger**.

---

## Migraciones de base de datos

### Primera vez (crear la base de datos)

```bash
# Desde la carpeta backend-dotnet/
dotnet ef migrations add InitialCreate \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API

dotnet ef database update \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

### Cada vez que se agregan cambios al modelo

```bash
dotnet ef migrations add <NombreDeLaMigracion> \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API

dotnet ef database update \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

### Revertir la última migración

```bash
dotnet ef migrations remove \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

---

## Probar la API con Swagger

1. Abrir **http://localhost:5073/swagger**
2. Ejecutar `POST /api/v1/users/register` para crear un usuario
3. Ejecutar `POST /api/v1/users/login` → copiar el campo `token` de la respuesta
4. Click en **Authorize** (candado arriba a la derecha) → pegar el token → **Authorize**
5. Ya podés ejecutar los endpoints protegidos

---

## Comandos útiles

```bash
# Restaurar paquetes
dotnet restore

# Compilar
dotnet build

# Correr la API
dotnet run --project src/AcademiaDigital.API

# Correr tests (si existen)
dotnet test

# Ver migraciones aplicadas
dotnet ef migrations list \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

---

## Troubleshooting

### `dotnet-ef` se instaló en versión 10 pero el proyecto es .NET 8

`dotnet tool install --global dotnet-ef` sin versión instala la última (10.x), incompatible con `net8.0`. Desinstalá y fijá la versión:

```bash
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version "8.0.13"
```

### Error: `startup project doesn't reference Microsoft.EntityFrameworkCore.Design`

EF necesita el paquete `Design` también en el proyecto API (no solo en Infrastructure). Agregalo:

```bash
dotnet add src/AcademiaDigital.API package Microsoft.EntityFrameworkCore.Design --version "8.0.13"
```

---

## Flujo esperado para desarrollo local

1. Asegurarse de tener SQL Server corriendo y accesible en `localhost,1433`
2. Ejecutar las migraciones para crear las tablas (solo la primera vez)
3. Correr `dotnet run --project src/AcademiaDigital.API`
4. Levantar el frontend Angular (ver [../frontend-angular/README.md](../frontend-angular/README.md))
