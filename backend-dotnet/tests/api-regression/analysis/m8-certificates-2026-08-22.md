# Evidencia M8 — Certificados y constancias

Fecha de ejecución: 2026-08-22.

## Resultado

M8 queda cerrado como MVP. Las tres rutas heredadas se conservaron y se agregaron revisión, emisión, historial y descarga sin modificar el frontend.

- Slice M8: **1/1**.
- Regresión completa: **31/31**.
- Domain Unit Tests: **161/161**.
- Application Unit Tests: **75/75**.
- ArchUnitNET: **8/8**.
- TypeScript/Zod: typecheck correcto.
- Migración/backfill M1–M8: correcta desde el corte histórico.
- Modelo EF: sin cambios pendientes.
- Build/publish Release: 0 warnings y 0 errores, validado localmente y en la imagen Docker.
- Allure: generado desde la regresión completa.

## Alcance cubierto

- Siete tipos canónicos: alumno regular, matrícula, materias aprobadas, situación académica, analítico, estado académico general y permiso de examen.
- Compatibilidad con los nombres en español ya enviados por Angular.
- Selección segura de `StudentCareer`, elegibilidad por tipo y rechazo de solicitud activa duplicada.
- Aprobación o rechazo Admin con actor, fecha y motivo.
- Correlativo global `CERT-00000001`, snapshot académico inmutable, emisor y fecha.
- PDF por tipo mediante `ICertificatePdfGenerator`, guardado por clave lógica y SHA-256.
- Emisión reintentable: una falla conserva la misma reserva y nunca crea otro número para la solicitud.
- Historial por alumno y descarga exclusiva de Admin o propietario, con verificación de integridad del archivo.
- Backfill de solicitudes heredadas: normalización de tipo, vínculo a carrera y cierre de duplicados activos anteriores.

## Concurrencia y defecto detectado

La primera ejecución simultánea de dos emisiones reveló un deadlock SQL: cada transacción bloqueaba primero una solicitud distinta y luego competía por la secuencia. Se corrigió fijando `CertificateSequences(1)` como raíz global del orden de locks antes de tocar solicitud y datos académicos. La misma carrera concurrente pasó después y produjo dos números únicos y consecutivos.

## Operaciones verificadas

- `GET /api/v1/certificates/my`.
- `POST /api/v1/certificates/request`.
- `GET /api/v1/certificates/all`.
- `POST /api/v1/certificates/{id}/approve`.
- `POST /api/v1/certificates/{id}/reject`.
- `POST /api/v1/certificates/{id}/issue`.
- `GET /api/v1/certificates/issued/me`.
- `GET /api/v1/certificates/students/{studentId}/history`.
- `GET /api/v1/certificates/issued/{publicId}/download`.

Swagger expone **171 operaciones**; **142** tienen aserción API directa, 5 se usan sólo en setup/cleanup y 24 quedan sin cobertura directa.
