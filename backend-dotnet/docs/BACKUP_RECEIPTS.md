# Backup y restauración de recibos digitales

Última actualización: 2026-11-14.

## Alcance

Un recibo M11 se compone de dos partes inseparables:

- PostgreSQL (`academiadigital_finance`): correlativo, vínculo al pago, snapshot, auditoría, estado, clave lógica y SHA-256.
- Volumen `finance_files`: PDF almacenado bajo `receipts/<año>/<publicId>/REC-########.pdf`.

El volumen `backend_files` conserva acuerdos y certificados del monolito. El compose monta ambos volúmenes en `/app/data/files`; bases y archivos son una unidad de backup. Nunca respaldar sólo los PDFs o sólo PostgreSQL.

## Frecuencia recomendada

- Backup completo diario de las bases PostgreSQL y de los volúmenes `backend_files` y `finance_files`.
- Retención mínima sugerida: 30 copias diarias y 12 mensuales, ajustable a la política institucional.
- Copia fuera del host Docker y cifrada en reposo.
- Prueba de restauración al menos una vez por mes.

## Backup consistente

Ejecutar desde `backend-dotnet/`. Crear primero una carpeta fuera del repositorio o ignorada por Git:

```powershell
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path (Resolve-Path ..) "backups/academia-$timestamp"
New-Item -ItemType Directory -Path $backupRoot -Force
```

Poner la aplicación en mantenimiento o detener temporalmente backend y Finance para obtener un corte consistente:

```powershell
docker compose stop backend finance
docker compose exec -T db pg_dump -U postgres -Fc -d AcademiaDigital -f /tmp/academia.dump
docker compose exec -T db pg_dump -U postgres -Fc -d academiadigital_finance -f /tmp/finance.dump
docker compose cp db:/tmp/academia.dump "$backupRoot/academia.dump"
docker compose cp db:/tmp/finance.dump "$backupRoot/finance.dump"
docker compose cp backend:/app/data/files "$backupRoot/backend-files"
docker compose cp finance:/app/data/files "$backupRoot/finance-files"
docker compose start finance backend
```

Si un contenedor detenido no admite `docker compose cp`, copiar el volumen mediante un contenedor auxiliar de backup aprobado por Operaciones. No iniciar las APIs hasta terminar todas las copias.

Verificar ambos dumps antes de archivarlos:

```powershell
docker compose exec -T db pg_restore --list /tmp/academia.dump
docker compose exec -T db pg_restore --list /tmp/finance.dump
```

Registrar fecha, ambiente, versión desplegada, tamaño y resultado de las verificaciones. Una prueba real de restauración sobre bases descartables sigue siendo obligatoria.

## Restauración

La restauración es una operación destructiva y debe ejecutarse sólo sobre el ambiente confirmado, con la API detenida y una copia preventiva del estado actual.

1. Detener `backend` y `finance`.
2. Restaurar `academia.dump` y `finance.dump` con `pg_restore` en sus bases PostgreSQL confirmadas.
3. Restaurar únicamente los volúmenes `backend_files` y `finance_files` confirmados para ese ambiente.
4. Iniciar primero `finance` y luego `backend`; cada API aplicará únicamente migraciones posteriores al backup.
5. Consultar varios recibos y descargar sus PDFs.
6. Recalcular SHA-256 y compararlo con `Receipts.sha256`.

No regenerar correlativos ni modificar `ReceiptSequences.last_value` durante una restauración. La secuencia restaurada y el ledger deben provenir del mismo dump de Finance.

## Verificación de integridad posterior

Para cada muestra restaurada:

- el número cumple `REC-########` y es único;
- el pago asociado existe;
- el snapshot conserva alumno, DNI, conceptos, monto, fecha, operador y medio;
- el archivo empieza con `%PDF-`;
- el SHA-256 del archivo coincide con la columna `sha256`;
- Admin y el alumno propietario pueden descargarlo, otro alumno recibe 403.

Si falta un archivo o el hash no coincide, no editar el ledger ni asignar otro número. Restaurar el archivo correcto desde backup; si el recibo quedó `Failed`, usar el endpoint Admin de reintento para regenerarlo con su mismo correlativo.
