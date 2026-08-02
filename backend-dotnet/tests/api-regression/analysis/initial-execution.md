# Primera ejecución real

## Validación posterior al refactor multi-carrera

Ambiente recreado desde cero dos veces: API `http://localhost:8010`, base `AcademiaDigitalE2E`.

```text
Tests: 15
Passed: 15
Failed: 0
Blocked: 0
Ejecuciones consecutivas previas: 14/14 passed; ejecución final con concurrencia: 15/15 passed
Build .NET Release: OK, 0 warnings
TypeScript: OK
```

La regresión cubre rollback del alta incompatible/incompleta/inexistente, registro sin User huérfano, alta combinada sin duplicar plan, concurrencia sobre el mismo User, dos carreras con el mismo legajo, dos asignaciones vigentes, progreso/elegibilidad por carrera, inscripción en la segunda carrera y autorización de consultas académicas.

El script `npm run test:migration` valida el backfill desde la migración anterior y el aborto diagnóstico cuando hay varios Students para un User.

## Incremento P1: documentos, becas y campos personalizados

```text
Tests totales: 19
Passed: 19
Failed: 0
Blocked: 0
P0: 15/15 passed
P1: 4/4 passed
Build .NET Release: OK, 0 warnings
TypeScript: OK
```

El defecto de `GET /student-custom-fields` fue corregido: las definiciones desactivadas ya no se listan ni se obtienen por ID. Documentos —incluyendo pendientes multi-carrera, reemplazo, revisión y baja lógica—, becas, campos personalizados y autorización P1 pasan completos.

## Línea base anterior al refactor

Ambiente: Docker descartable, API `http://localhost:8010`, base `AcademiaDigitalE2E`.

## Resultado

```text
Tests: 11
Passed: 9
Failed: 2
Blocked: 0
TypeScript: OK
Allure HTML: generado correctamente
```

## Fallos que representan defectos del backend

1. `StudentAcademicController` permite ejecutar `GET /students/{id}/eligible-courses` sin sesión. Para un ID inexistente devuelve 404 cuando la política documentada exige 401.
2. `POST /students` con plan de la carrera seleccionada y comisión de otra carrera devuelve 409, pero deja creado el Student. La operación no es atómica.

## Ajustes de contrato incorporados durante la ejecución

- El perfil serializa `role` como número (`Admin = 3`).
- El driver SQL entrega columnas `bigint` como string; el fixture normaliza `studentId` a number antes de compararlo con respuestas JSON.

Todos los escenarios ejecutaron cleanup en `finally`. Los resultados Allure incluyen attachments redactados para cada operación HTTP.

## Ejecución de población sobre desarrollo Docker

Destino: API `http://localhost:5073`, SQL Server `localhost:1433`, base `AcademiaDigital`, con `E2E_PRESERVE_DATA=true`.

```text
Tests de flujos/consultas: 3
Passed: 3
Failed: 0
TypeScript: OK
Allure HTML: generado correctamente
```

Datos de automatización persistidos al finalizar:

```json
{
  "careers": 2,
  "courses": 4,
  "studyPlans": 2,
  "commissions": 3,
  "users": 2,
  "students": 2,
  "enrollments": 1
}
```

La auditoría de los artefactos encontró 104 requests/responses adjuntos y cero coincidencias de credenciales o tokens sin redactar.
