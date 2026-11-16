# Multitenant: una base compartida, un esquema por institución

Implementado en la v1.7.0. Reemplaza el modelo de "una base por institución" de la v1.3.0.

## Modelo

```
base academiadigital
├── esquema control   catálogo de instituciones y dominios (antes: base academiadigital_control)
├── esquema public    ITSC (sin cambios: sus datos ya estaban acá)
└── esquema usb       cada institución nueva: las mismas tablas del monolito

base de Finance        sin cambios: solo la usa ITSC (ver más abajo)
```

Cada institución tiene **sus propias tablas** en su esquema. Por eso las consultas, los índices únicos (email, DNI, legajo), el SQL escrito a mano (`SELECT ... FOR UPDATE`) y la numeración de certificados funcionan igual que antes, y una consulta no puede ver datos de otra institución.

Se descartó la alternativa de poner una columna `TenantId` en las 58 tablas: implicaba un refactor grande y el riesgo de mezclar datos si alguna consulta se saltaba el filtro.

## Cómo funciona

- **Conexión.** PostgreSQL resuelve las tablas sin esquema (`"Users"`) con el `search_path`. `TenantConnectionStrings.For(schema)` arma la cadena de `DefaultConnection` con `Search Path=<esquema>`. Las migraciones del monolito no fijan esquema, así que crean las tablas en el esquema de la institución.
- **Historial de migraciones.** `UseSchemaNpgsql` fija explícitamente la tabla `__EFMigrationsHistory` en el mismo esquema. Así cada institución, y el catálogo, lleva su propio historial.
- **Catálogo.** `ControlDbContext` usa `Search Path=control` en la misma base. `ConnectionStrings:ControlConnection` sigue permitiendo apuntarlo a otro lado.
- **Esquemas.** `SchemaNames.Validate` admite solo `^[a-z][a-z0-9_]{0,62}$` y rechaza el prefijo `pg_`, porque un identificador SQL no se puede pasar como parámetro. `SchemaNames.EnsureCreatedAsync` crea la base, si no existe, y el esquema.
- **Alta de institución** (`/platform`): crea el esquema `<slug>` (con `-` → `_`), lo migra, crea el admin y, como último paso, lo registra en el catálogo. Si falla, se puede reintentar.
- **Nombres reservados:** `public`, `control`, `finance`, `finance-*`, `pg-*`, `platform`, `admin`, `api`, `www`, `academiadigital`, `postgres`, `template0`, `template1`.

## Finance

Queda **aparte y sin cambios**: su base, su esquema `finance` y sus migraciones. Mientras no distinga instituciones, solo lo usa la institución por defecto (ITSC). Para las demás, `HttpFinanceClient` omite la generación de deuda de matrícula, así que no se mezclan datos.

Cuando otra institución necesite pagos, la opción más simple es una base de Finance por institución (`academiadigital_finance_<slug>`). Su esquema interno no cambia; Finance solo elige la base según el header `X-Tenant` que le envíe el monolito.

## Seguridad al arrancar

Antes, si la migración encontraba tablas sin historial, el arranque **borraba la base completa** (`EnsureDeletedAsync`). Con la base compartida eso borraría todas las instituciones y el catálogo. Ahora se registra un error crítico y el arranque se detiene **sin borrar nada**.

DevTools (solo desarrollo local) mantiene su "reset", que borra la base completa. En local, con varias instituciones, el reset borra también el catálogo y las demás instituciones; al reiniciar el backend se vuelven a crear `control` e ITSC.

## Verificación (PostgreSQL 16 local)

Se reprodujo la actualización de producción. Primero se levantó `main` (v1.6.0) con ITSC, los seeds y la base `academiadigital_control`; después, la versión nueva sobre esa misma base:

- se creó el esquema `control` con ITSC en `public`; los 6 usuarios quedaron intactos y el login de ITSC responde 200;
- el alta de `usb` creó su esquema con las mismas 60 tablas que `public`, y su admin quedó solo en `usb`;
- el slug `public` se rechaza (400);
- el mismo email y DNI se registraron en ITSC (Alumno) y en `usb` (Profesor, por el dominio docente `usb.edu`), como dos personas distintas;
- el admin de ITSC no inicia sesión en `usb` y viceversa (401); el token de `usb` no sirve contra ITSC (401); cada admin ve solo sus aspirantes;
- un reinicio vuelve a migrar los tres esquemas sin errores, y cada uno tiene su propio historial.

## Despliegue

No hay cambios de Terraform. Hoy solo existe ITSC, que se queda en `public`.

1. Merge → el pipeline despliega → al arrancar se crea el esquema `control` y se registra ITSC (con `Tenancy__DefaultTeacherEmailDomain`).
2. Comprobar `https://itsc.dryvn.org/api/v1/tenant/current` y el login.
3. Más adelante, después de verificar, borrar a mano la base vieja `academiadigital_control`, que queda sin uso.

## Landing y alta de instituciones

Desde la v1.8.0: la landing (`academiadigital.dryvn.org`) recibe pedidos, y el panel (`admin.dryvn.org`) los convierte en instituciones con su subdominio. Ver la fase 7 de `infra/DESPLIEGUE-ITSC.md`.
