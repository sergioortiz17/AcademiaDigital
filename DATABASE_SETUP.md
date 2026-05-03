# AcademiaDigital — Levantar SQL Server localmente (sin Docker completo)

El proyecto usa **SQL Server 2022** como base de datos. Esta guía explica cómo dejarlo corriendo en tu máquina local para poder desarrollar sin necesitar levantar todo el `docker-compose`.

---

## Opciones disponibles

| Opción | Requisito | Recomendado para |
|--------|-----------|-----------------|
| [A) Docker solo para SQL Server](#opción-a-docker-solo-para-sql-server-recomendado) | Docker instalado | La mayoría de los casos — más simple y limpio |
| [B) SQL Server nativo en Linux](#opción-b-sql-server-nativo-en-linux-ubuntu--debian) | Ubuntu/Debian 20.04+ | Si no querés usar Docker en absoluto |
| [C) SQL Server nativo en Windows](#opción-c-sql-server-nativo-en-windows) | Windows 10/11 | Desarrolladores en Windows |

---

## Opción A: Docker solo para SQL Server (recomendado)

Levantás **únicamente el contenedor de la base de datos**, sin el backend ni el frontend. Docker no tiene que estar en "Docker Desktop", alcanza con tener el daemon corriendo.

### 1. Levantar el contenedor

```bash
docker run -d \
  --name sqlserver \
  -e ACCEPT_EULA=Y \
  -e SA_PASSWORD=Admin1234! \
  -e MSSQL_PID=Developer \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

> La primera vez descarga la imagen (~1.5 GB), puede tardar unos minutos.

### 2. Verificar que está corriendo

```bash
docker ps --filter name=sqlserver
```

Esperá ~20-30 segundos hasta que el estado sea `healthy` o simplemente `Up`. Podés revisar los logs con:

```bash
docker logs sqlserver
```

Cuando veas la línea `SQL Server is now ready for client connections`, está listo.

### 3. Comandos útiles del contenedor

```bash
# Detener sin borrar datos
docker stop sqlserver

# Volver a iniciar
docker start sqlserver

# Borrar el contenedor (los datos se pierden)
docker rm -f sqlserver

# Ver logs en tiempo real
docker logs -f sqlserver
```

> Los datos **no persisten** si hacés `docker rm`. Para guardarlos entre sesiones usá un volumen:
> ```bash
> docker run -d \
>   --name sqlserver \
>   -e ACCEPT_EULA=Y \
>   -e SA_PASSWORD=Admin1234! \
>   -e MSSQL_PID=Developer \
>   -p 1433:1433 \
>   -v sqlserver_data:/var/opt/mssql \
>   mcr.microsoft.com/mssql/server:2022-latest
> ```

---

## Opción B: SQL Server nativo en Linux (Ubuntu / Debian)

Si no querés usar Docker, podés instalar SQL Server for Linux directamente.

### 1. Agregar el repositorio de Microsoft

**Ubuntu 22.04:**

```bash
# Importar la clave GPG
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | \
  sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg

# Agregar el repositorio de SQL Server 2022
curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | \
  sudo tee /etc/apt/sources.list.d/mssql-server-2022.list

sudo apt-get update
```

**Ubuntu 24.04:**

> SQL Server 2022 no tiene paquete nativo para Ubuntu 24.04. El workaround es usar el repositorio de 22.04, cuyos binarios corren sin problema en 24.04.

```bash
# Importar la clave GPG
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | \
  sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg

# Usar el repositorio de Ubuntu 22.04 (workaround para 24.04)
curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | \
  sudo tee /etc/apt/sources.list.d/mssql-server-2022.list

sudo apt-get update
```

### 2. Instalar SQL Server

```bash
sudo apt-get install -y mssql-server
```

### 3. Configurar SQL Server

```bash
sudo /opt/mssql/bin/mssql-conf setup
```

El asistente te va a pedir:
1. Elegir la edición → ingresá `2` (Developer — gratis para desarrollo)
2. Aceptar el EULA → `Yes`
3. Ingresar y confirmar la contraseña del usuario `sa` → usá `Admin1234!` para que coincida con el `appsettings.Development.json`

### 4. Verificar que el servicio esté corriendo

```bash
systemctl status mssql-server
```

Si no está corriendo:

```bash
sudo systemctl start mssql-server
sudo systemctl enable mssql-server  # para que arranque solo al iniciar el sistema
```

---

## Opción C: SQL Server nativo en Windows

### 1. Descargar SQL Server 2022 Developer Edition

Descargá el instalador desde la página oficial de Microsoft:

**https://www.microsoft.com/es-es/sql-server/sql-server-downloads**

Elegí la edición **Developer** (gratuita para desarrollo y testing).

### 2. Instalar SQL Server

1. Ejecutá el instalador descargado
2. Elegí el tipo de instalación **Básica** (suficiente para desarrollo local)
3. Aceptá el EULA y seguí los pasos hasta que finalice
4. Al terminar, anotá la cadena de conexión que muestra el instalador (la vamos a ignorar — usaremos autenticación SQL)

### 3. Habilitar autenticación SQL y el usuario `sa`

Por defecto SQL Server en Windows viene con autenticación de Windows únicamente. Hay que habilitar el modo mixto y activar el usuario `sa`.

**Con SQL Server Management Studio (SSMS):**

1. Descargá e instalá [SSMS](https://aka.ms/ssmsfullsetup) si no lo tenés
2. Conectate al servidor con autenticación de Windows
3. Click derecho sobre el servidor → **Properties** → **Security**
4. Cambiá a **SQL Server and Windows Authentication mode** → **OK**
5. En **Object Explorer**: Security → Logins → `sa` → click derecho → **Properties**
   - **Status** → Login: **Enabled**
   - **General** → ingresá la contraseña: `Admin1234!`
6. Reiniciá el servicio SQL Server desde **SQL Server Configuration Manager**

**Con PowerShell (alternativa sin SSMS):**

```powershell
# Habilitar modo mixto
$server = "localhost"
Import-Module SqlServer
$s = New-Object Microsoft.SqlServer.Management.Smo.Server $server
$s.Settings.LoginMode = [Microsoft.SqlServer.Management.SMO.ServerLoginMode]::Mixed
$s.Alter()

# Reiniciar el servicio
Restart-Service -Name MSSQLSERVER
```

### 4. Habilitar el protocolo TCP/IP en el puerto 1433

1. Abrí **SQL Server Configuration Manager** (buscalo en el menú Inicio)
2. Expandí **SQL Server Network Configuration** → **Protocols for MSSQLSERVER**
3. Click derecho en **TCP/IP** → **Enable**
4. Click derecho en **TCP/IP** → **Properties** → pestaña **IP Addresses**
5. En **IPAll**: `TCP Port` = `1433`, `TCP Dynamic Ports` = (vacío)
6. **OK** → reiniciá el servicio desde **SQL Server Services** → click derecho en **SQL Server (MSSQLSERVER)** → **Restart**

### 5. Abrir el firewall (si es necesario)

```powershell
# Ejecutar como Administrador
New-NetFirewallRule -DisplayName "SQL Server 1433" `
  -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
```

### 6. Verificar la conexión

Abrí PowerShell o CMD y ejecutá:

```powershell
sqlcmd -S localhost,1433 -U sa -P Admin1234! -Q "SELECT @@VERSION" -C
```

> Si `sqlcmd` no está en el PATH, buscalo en `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\...\Tools\Binn\`.

---

## Verificar la conexión a SQL Server

Una vez que tenés SQL Server corriendo (por cualquiera de las tres opciones), podés verificar la conexión con `sqlcmd`.

### Instalar sqlcmd (si no lo tenés)

```bash
# Opción A: desde el paquete de herramientas de Microsoft
sudo apt-get install -y mssql-tools18 unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
source ~/.bashrc

# Opción B (más simple): instalar sqlcmd standalone
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
sudo curl -o /etc/apt/sources.list.d/microsoft.list \
  https://packages.microsoft.com/config/ubuntu/22.04/prod.list
sudo apt-get update
sudo apt-get install -y sqlcmd
```

### Probar la conexión

```bash
sqlcmd -S localhost,1433 -U sa -P Admin1234! -Q "SELECT @@VERSION" -C
```

Si devuelve la versión de SQL Server, la conexión funciona correctamente.

---

## Crear la base de datos con EF Core

Una vez que SQL Server está corriendo, desde la carpeta `backend-dotnet/` ejecutá:

### Primera vez (crear las tablas)

```bash
cd backend-dotnet

# Instalar la herramienta EF en la versión correcta para .NET 8
dotnet tool install --global dotnet-ef --version "8.0.13"
export PATH="$PATH:$HOME/.dotnet/tools"

# Crear la migración inicial (si no existe la carpeta Migrations/)
dotnet ef migrations add InitialCreate \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API

# Aplicar la migración — crea la DB "AcademiaDigital" y todas las tablas
dotnet ef database update \
  --project src/AcademiaDigital.Infrastructure \
  --startup-project src/AcademiaDigital.API
```

### Verificar que las tablas se crearon

```bash
sqlcmd -S localhost,1433 -U sa -P Admin1234! -C \
  -Q "USE AcademiaDigital; SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"
```

---

## Connection string usada en desarrollo

El archivo [backend-dotnet/src/AcademiaDigital.API/appsettings.Development.json](backend-dotnet/src/AcademiaDigital.API/appsettings.Development.json) ya tiene configurado:

```
Server=localhost,1433;Database=AcademiaDigital;User Id=sa;Password=Admin1234!;TrustServerCertificate=True;
```

No hace falta modificar nada si usaste `Admin1234!` como contraseña al configurar SQL Server.

---

## Troubleshooting

### Error: `Cannot open server 'localhost'`

- **Linux (A):** verificá que el contenedor esté corriendo: `docker ps --filter name=sqlserver`
- **Linux (B):** verificá el servicio: `systemctl status mssql-server`
- **Windows (C):** abrí **SQL Server Configuration Manager** y confirmá que el servicio esté en estado *Running* y que TCP/IP esté habilitado en el puerto 1433
- Verificá que el puerto 1433 esté escuchando:
  - Linux: `ss -tlnp | grep 1433`
  - Windows (PowerShell): `netstat -an | findstr 1433`

### Error: `Login failed for user 'sa'`

- Asegurate de usar exactamente `Admin1234!` como contraseña (mayúscula, número y símbolo son obligatorios por política de SQL Server)
- **Linux (B):** revisá si el usuario `sa` está habilitado ejecutando `sudo /opt/mssql/bin/mssql-conf setup` nuevamente
- **Windows (C):** confirmá que el modo de autenticación sea *SQL Server and Windows Authentication mode* (paso 3 de la opción C) y que el login `sa` esté en estado *Enabled*

### Error al correr `dotnet ef`

- Verificá que tenés instalada la herramienta: `dotnet ef --version`
- Si da "command not found", instalala y agregá las tools al PATH:
  ```bash
  dotnet tool install --global dotnet-ef --version "8.0.13"
  export PATH="$PATH:$HOME/.dotnet/tools"
  ```
  - **Windows (PowerShell):** `$env:PATH += ";$env:USERPROFILE\.dotnet\tools"`

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

### SQL Server consume demasiada RAM

SQL Server necesita mínimo ~2 GB de RAM. Si tu máquina tiene poco:
- **Linux:**
  ```bash
  sudo /opt/mssql/bin/mssql-conf set memory.memorylimitmb 1500
  sudo systemctl restart mssql-server
  ```
- **Windows:** en **SQL Server Management Studio** → click derecho en el servidor → **Properties** → **Memory** → ajustá el *Maximum server memory (MB)*

---

## Flujo completo de desarrollo local

```
1. SQL Server corriendo (Docker o nativo)
        ↓
2. dotnet ef database update  ← solo la primera vez
        ↓
3. dotnet run --project src/AcademiaDigital.API
        ↓
4. npm start (en frontend-angular/)
        ↓
5. http://localhost:4200
```

Ver también:
- [backend-dotnet/README.md](backend-dotnet/README.md) — cómo correr la API
- [frontend-angular/README.md](frontend-angular/README.md) — cómo correr el frontend
