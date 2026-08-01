# AcademiaDigital API Regression

Suite P0 de regresión API con Playwright Test, TypeScript, Zod y Allure. Las pruebas crean sus propios datos, capturan cada intercambio HTTP y redactan secretos antes de adjuntarlos al reporte.

## Requisitos

- Node.js 20 o superior.
- Java 17 o superior para generar el reporte Allure.
- Docker Compose para el ambiente descartable recomendado.

No es necesario instalar navegadores: la suite usa solamente `APIRequestContext`.

## Inicio rápido

```powershell
Copy-Item .env.example .env
npm.cmd install
npm.cmd run e2e:reset
npm.cmd run test:migration
npm.cmd run test:api
npm.cmd run allure:generate
npm.cmd run allure:open
```

La API E2E queda en `http://localhost:8010` y SQL Server en `localhost:1434`. Este modo usa `AcademiaDigitalE2E` y limpia los datos de cada escenario.

## Base de desarrollo Docker y datos persistentes

Para ejecutar contra el backend local `http://localhost:5073` y la base Docker `AcademiaDigital` de `localhost:1433`:

```powershell
Copy-Item .env.development.example .env -Force
npm.cmd run test:api:populate
npm.cmd run allure:generate
```

`test:api:populate` ejecuta los flujos integrales y las consultas, pero conserva las carreras, planes, materias, comisiones, usuarios, estudiantes, asignaciones, períodos e inscripciones creadas. Los códigos, emails, DNI y legajos incluyen un identificador único para permitir ejecuciones repetidas.

Antes de ejecutarlo, la API de `5073` y `E2E_SQL_CONNECTION_STRING` deben apuntar a la misma base. El setup siembra un administrador y valida su autenticación por la API; si las conexiones no coinciden, aborta antes de los tests.

El uso de `AcademiaDigital` requiere dos confirmaciones explícitas:

```dotenv
E2E_ALLOW_DEVELOPMENT_DATABASE=true
E2E_DOCKER_MANAGED_DATABASE=true
```

No deben activarse contra una base compartida o no descartable.

Para recrear completamente la base Docker de desarrollo, desde la raíz del repositorio:

```powershell
docker compose down -v
docker compose up -d db
```

`down -v` elimina el volumen `sqlserver_data` del proyecto Docker y, por lo tanto, todos los datos de `AcademiaDigital`. Después se debe volver a iniciar el backend de `5073`; sus migraciones recrean el esquema.

## Comandos

```text
npm run test:api             Suite completa
npm run test:api:smoke       Smoke tests
npm run test:api:critical    Flujos P0
npm run test:api:regression  Regresión
npm run test:api:negative    Validaciones negativas
npm run test:api:auth        Autorización
npm run test:api:p1          Documentos, becas y campos personalizados
npm run test:api:populate    Flujos y consultas conservando los datos creados
npm run db:population-summary Contar datos de automatización conservados
npm run typecheck            Validación TypeScript
npm run test:migration       Backfill desde el esquema anterior y guard de User duplicado
npm run allure:generate      Generar HTML Allure
npm run allure:open          Abrir reporte
npm run e2e:up|down|reset    Administrar ambiente descartable
```

## Evidencia y seguridad

Cada llamada genera siete attachments JSON dentro de su step Allure. Authorization, cookies, passwords, tokens, secretos y API keys se reemplazan por `***REDACTED***`. Los tokens reales permanecen solo en memoria.

Los artefactos se escriben en `allure-results`, `allure-report`, `playwright-report`, `test-results` y `.artifacts`. El Swagger observado durante la ejecución se guarda en `.artifacts/swagger-academiaDigital.json`.

## Datos y cleanup

El administrador y los usuarios Alumno sin Student son fixtures SQL porque la API pública de registro crea ambas entidades juntas. En regresión aislada, cada escenario conserva sus IDs, intenta primero los DELETE de la API y luego elimina dependencias en orden inverso mediante SQL. El fallback requiere `E2E_ALLOW_DB_CLEANUP=true`.

Con `E2E_PRESERVE_DATA=true` no se ejecuta cleanup de escenarios. El teardown solamente revoca las sesiones del administrador de automatización.

La regresión de migración crea y elimina exclusivamente `AcademiaDigitalMigrationE2E` en el SQL Server local configurado. Comprueba el backfill de membresías, planes y asignaciones, y que la migración aborte si existe más de un Student para un mismo User.

El bloque `@p1` comprueba que los catálogos activos no incluyan bajas lógicas; una definición de custom field desactivada deja de aparecer en el listado y responde `404` por ID.
