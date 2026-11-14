# AcademiaDigital — Backend .NET 

![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

API REST construida con **ASP.NET Core 8** siguiendo **Clean Architecture**. Usa **Entity Framework Core 8** con PostgreSQL y autenticación vía **JWT**.

> La forma recomendada de levantar PostgreSQL, Finance, backend y Angular es `docker compose up --build` desde la raíz del repositorio.

---

## Requisitos previos

| Herramienta | Versión | Verificar |
|-------------|---------|-----------|
| .NET SDK | 8.0 | `dotnet --version` |
| PostgreSQL | 16 | instancia local o Docker |
| dotnet-ef (CLI) | 8.x | `dotnet ef --version` |

> Instalar la herramienta EF Core en la versión correcta para este proyecto:
> ```bash
> dotnet tool restore
> export PATH="$PATH:$HOME/.dotnet/tools"
> ```
> **Importante:** no uses `8.*` en zsh — el shell lo interpreta como un glob y falla. Siempre especificá la versión exacta entre comillas.

---

## Estructura del proyecto

```

## Estado del MVP

Los módulos M1–M11 están completos para el alcance MVP. M11 incorpora recibos digitales internos no fiscales, emitidos automáticamente al confirmar un pago o aprobar la conciliación de una transferencia:

- correlativo global `REC-########` reservado transaccionalmente;
- snapshot histórico, PDF local-first y hash SHA-256;
- historial y descarga para Admin/Tesorería o alumno propietario;
- regeneración idempotente sobre el mismo número;
- almacenamiento Docker durable y backup coordinado con PostgreSQL.

Documentación operativa y de alcance:

- [Roadmap M4–M11](docs/ROADMAP_M4_M11.md)
- [Guía funcional M4–M11 para frontend](docs/GUIA_FUNCIONAL_M4_M11_FRONTEND.md)
- [Matriz de cobertura de API](docs/API_COVERAGE_MATRIX.md)
- [Cobertura de calidad y arquitectura](docs/COBERTURA_CALIDAD_ARQUITECTURA.md)
- [Backup y restauración de recibos](docs/BACKUP_RECEIPTS.md)
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
    "DefaultConnection": "Host=localhost;Port=5432;Database=AcademiaDigital;Username=postgres;Password=Admin1234!;"
  },
  "Jwt": {
    "SecretKey": "dev_secret_key_123_academia_digital",
    "ExpirationDays": "7"
  }
}
```

Si tu instancia de PostgreSQL usa otro usuario, contraseña o puerto, editá la cadena de conexión en ese archivo.

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

# Correr todos los tests .NET
dotnet test

# Ver migraciones aplicadas
dotnet ef migrations list \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

### Regresión API M1–M11

La suite integrada está en `tests/api-regression` y requiere Docker. El slice específico de recibos se ejecuta con `npm run test:api:m11`; la suite completa usa `npm run test:api`.

```bash
cd tests/api-regression
npm install
npm run e2e:up
npm run typecheck
npm run test:migration
npm run test:api:m11
npm run test:api
npm run allure:generate
npm run e2e:down
```

El último baseline validado es 291/291 unitarios, ArchUnitNET 8/8, migración M1–M11 y API Regression 34/34. Swagger expone 196 operaciones, incluidas las 5 de recibos.

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
dotnet add src/AcademiaDigital.API package Microsoft.EntityFrameworkCore.Design --version "8.0.11"
```

---

## Flujo esperado para desarrollo local

1. Asegurarse de tener PostgreSQL corriendo y accesible en `localhost:5432`
2. Ejecutar las migraciones para crear las tablas (solo la primera vez)
3. Correr `dotnet run --project src/AcademiaDigital.API`
4. Levantar el frontend Angular (ver [../frontend-angular/README.md](../frontend-angular/README.md))
