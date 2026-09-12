# Evidencia M6 — asistencias

Fecha: 2026-08-22  
Entorno: API `http://localhost:8010`, SQL Server `localhost:1434`, base aislada `AcademiaDigitalE2E`.

## Alcance validado

- Sesiones por hora cátedra y día completo, vinculadas a materia, comisión, ciclo, semestre y cargo docente.
- Alta con `Idempotency-Key`, índice natural único de la oferta y bloqueo serializable para evitar sesiones duplicadas entre cargos.
- Roster derivado de inscripción activa y asignación académica del alumno a la comisión.
- Carga masiva mediante upsert único por sesión e inscripción; la misma carga repetida conserva un solo registro.
- Estados `Present`, `Late` y `Absent` en carga; `Justified` sólo mediante justificación administrativa auditada.
- Ponderación `Present = 1`, `Late = 0.5`, `Absent = 0`; justificadas fuera del denominador.
- Riesgo calculado contra `CourseApprovalRule.MinimumAttendancePercentage` y consulta propia del alumno sin aceptar `StudentId` del cliente.
- Aislamiento entre profesores basado en el historial real de `TeacherAssignment`, con permisos 401/403 efectivos.
- Cierre de planilla, bloqueo de edición después del cierre o de 48 horas y reapertura exclusivamente Admin con motivo append-only.
- Exportación CSV UTF-8 con BOM compatible con Excel y PDF válido mediante un puerto reemplazable.
- Migración `20260822223239_AddAttendanceModule`: cuatro tablas, 13 claves foráneas, constraints de unidades/horarios e índices únicos de sesión, registro y justificación vigente.
- Cleanup E2E actualizado para eliminar dependencias de asistencia antes de inscripciones, cargos, comisiones y usuarios.

## Resultados

```text
Domain Unit Tests:       133/133 passed
Application Unit Tests:   59/59 passed
Total Unit Tests:        192/192 passed
M6 API Regression:          1/1 passed
API Regression completa:  29/29 passed
Migration Regression:       passed
ArchUnitNET:                 8/8 passed
Build Release:       0 warnings / 0 errors
TypeScript typecheck:        passed
Swagger:              146 operaciones
M6 Swagger:            10 operaciones cubiertas
```

La ejecución integrada detectó dos defectos antes del cierre: el CSV no emitía físicamente el BOM aunque declaraba UTF-8 y `FindByUserIdAsync` no incluía la navegación `User` requerida para proyectar el resumen propio. Ambos fueron corregidos y la suite M6 y la regresión completa se reejecutaron satisfactoriamente.

Comandos de evidencia:

```powershell
dotnet test tests/AcademiaDigital.Domain.UnitTests/AcademiaDigital.Domain.UnitTests.csproj -c Release
dotnet test tests/AcademiaDigital.Application.UnitTests/AcademiaDigital.Application.UnitTests.csproj -c Release
dotnet test tests/AcademiaDigital.ArchitectureTests/AcademiaDigital.ArchitectureTests.csproj -c Debug
dotnet build AcademiaDigital.sln -c Release
dotnet ef migrations has-pending-model-changes --project src/AcademiaDigital.Infrastructure --startup-project src/AcademiaDigital.API
npm.cmd run typecheck
npm.cmd run test:migration
npm.cmd run test:api:m6
npm.cmd run test:api
npm.cmd run allure:generate
```
