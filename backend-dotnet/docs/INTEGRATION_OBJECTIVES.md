# Integración progresiva M1–M17

Rama de integración: integracion-14-11. Cada objetivo se desarrolla en su subrama y se fusiona únicamente después de superar sus gates acumulativos.

| Orden | Subrama | Alcance | Estado |
|---:|---|---|---|
| 0 | obj/00-baseline-ci | Baseline, PostgreSQL, Compose, CI y trazabilidad | En curso |
| 1 | obj/01-identity-permissions | Identidad, siete roles, permisos y recuperación asistida | Pendiente |
| 2 | obj/02-academic-base | Brechas M2–M3 | Pendiente |
| 3 | obj/03-admissions | M4 completo | Pendiente |
| 4 | obj/04-academic-operations | Cierre M5–M8 | Pendiente |
| 5 | obj/05-finance-integration | Integración M9–M11 | Pendiente |
| 6 | obj/06-delinquency-expenses | M12–M13 | Pendiente |
| 7 | obj/07-portals-messaging | M14–M15 | Pendiente |
| 8 | obj/08-reports-audit | M16–M17 | Pendiente |
| 9 | obj/09-release-hardening | Upgrade, backup, seguridad y release | Pendiente |

## Gate por objetivo

1. git diff --check, build y pruebas específicas del subsistema.
2. Build Release del monolito y Finance, unitarios, arquitectura y suite Angular.
3. Migración PostgreSQL limpia y desde snapshot previo, regresión API y E2E positivo/401/403.
4. Compose completo y regresión acumulada antes de fusionar a integracion-14-11.

La fuente de verdad requisito por requisito es MVP_REQUIREMENTS_TRACEABILITY.csv. Un requisito solo cambia a Cumplido cuando las columnas de evidencia y prueba apuntan a implementaciones verificadas.
