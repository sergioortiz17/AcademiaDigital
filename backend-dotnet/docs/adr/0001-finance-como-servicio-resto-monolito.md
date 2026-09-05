# ADR 0001 — Finance como microservicio; el resto como monolito modular

- **Estado:** Aceptada
- **Fecha:** 2026-09-05
- **Contexto de decisión:** el equipo (tesis + instituto)
- **Aplica a:** AcademiaDigital (backend .NET 8, PostgreSQL 16, Angular)

> Los ADR documentan *por qué* se tomó una decisión de arquitectura, para que las
> próximas cohortes de mantenedores entiendan el razonamiento sin reconstruirlo de
> memoria. No se editan una vez aceptados: si la decisión cambia, se escribe un ADR
> nuevo que reemplace a este.

## Contexto

AcademiaDigital pasa de ser un proyecto de tesis a la **plataforma productiva real** del
instituto: tráfico real y **mantenida a futuro por cohortes rotativas** de estudiantes de
la carrera (alta rotación de mantenedores, conocimiento que se pierde entre cohortes).

Se evaluó separar el sistema en microservicios vs. mantenerlo como un solo despliegue.
Factores concretos del contexto:

- **Mantenedores rotativos:** cada cohorte aprende el sistema casi de cero. La complejidad
  operativa de N microservicios (despliegues coordinados, versionado de contratos,
  observabilidad distribuida, transacciones distribuidas, debugging entre servicios) es
  cara de transferir y fácil de romper con equipos que rotan.
- **Escala real pero acotada:** un instituto, no una plataforma de millones de usuarios.
  No hay una necesidad de escalado independiente que justifique el costo operativo.
- **Infra:** corre en una sola máquina de OCI. No hay aislamiento físico entre servicios
  "gratis"; separar agrega saltos de red dentro de la misma máquina.

## Decisión

1. **El sistema es un MONOLITO MODULAR.** Un solo backend .NET desplegable, organizado en
   módulos con límites claros (Academic, Students/Enrollments, Teachers/Attendance/Grades,
   Admissions, Certificates). La modularidad se cuida a nivel de código (ver ADR de seams /
   `docs/architecture/seams-map.md`), no de despliegue.

2. **Finance se extrae como el ÚNICO microservicio.** Es la excepción justificada:
   - **Acoplamiento genuinamente bajo:** Finance solo necesita ids (`StudentId`, `CareerId`,
     `UserId`) del resto del sistema — nada de la estructura académica (materias, planes,
     correlatividades). Verificado en código: hoy el resto del sistema **no** consume
     Finance (el enrollment no toca deuda); el único acoplamiento son navegaciones EF que
     Finance tiene *hacia* Student/User/Career, que se cortan a ids.
   - **Razón de negocio propia:** compliance fiscal (AFIP) — facturación electrónica, CAE,
     secuencias de recibos. Es un dominio con su propio ciclo de vida regulatorio, que se
     beneficia de estar aislado (auditoría, despliegue independiente ante cambios de AFIP).
   - **Valor demostrativo:** es el caso de estudio real de microservicio para la tesis.

3. **Finance NUNCA bloquea el flujo académico.** La inscripción/matriculación de un alumno
   **no depende** de Finance y no puede ser frenada por él. La única llamada
   monolito→Finance (generar la deuda al matricular) es **fire-and-forget y tolerante a
   error**: si Finance está caído, la inscripción se completa igual y la deuda se puede
   generar/reconciliar después. Finance es un sistema de **registro y consulta**, no un
   gate. (Decisión de negocio explícita del instituto.)

## Aislamiento de datos

- Finance usa un **schema Postgres separado (`finance`) en la misma instancia**, con su
  **propio rol** (`finance_user`) restringido a ese schema. Esto da aislamiento **lógico
  real**: no puede existir una FK cruzando entre el schema del monolito y el de Finance.
- No es una instancia física separada (no se justifica en una sola máquina de OCI), pero
  el diseño permite migrar a instancia separada **cambiando solo la connection string**,
  sin tocar código, porque no hay dependencias cruzadas a nivel de datos.
- Finance tiene su propio `FinanceDbContext` y sus propias migraciones EF.

## Comunicación

- **monolito → Finance (HTTP):** `POST /api/v1/finance/debts/generate` al matricular
  (registro contable, fire-and-forget). Consultas informativas de deuda para pantallas.
- **Finance → monolito (HTTP):** solo *display-names* (nombre de alumno/usuario/carrera
  para mostrar junto a una deuda o recibo). Finance **cachea** localmente estos nombres
  (TTL corto) y **degrada a mostrar el id** si el monolito no responde. Nunca hace JOIN ni
  lógica sobre datos del monolito.

## Consecuencias

**Positivas**
- El grueso del sistema queda simple de entender y operar para mantenedores rotativos.
- Finance queda como ejemplo real y aislado de microservicio, con contrato explícito.
- El aislamiento lógico (schema+rol) previene el anti-patrón de "microservicio que
  comparte base", que es la causa más común de acoplamiento oculto.

**Negativas / costos aceptados**
- Un segundo despliegue (contenedor + schema) que operar y monitorear.
- Los nombres para mostrar en Finance requieren una llamada HTTP (mitigado con caché +
  degradado a id).
- Consistencia eventual entre "se matriculó" y "se generó la deuda" (aceptable: la deuda
  es un registro contable, no un prerrequisito del acto académico).

**Neutrales**
- Si en el futuro el instituto crece y otro módulo necesita separarse, el mapa de seams
  (`docs/architecture/seams-map.md`) indica dónde el acoplamiento actual haría cara esa
  extracción, para hacerla quirúrgica y no como reescritura.

## Alternativas consideradas

- **Todo microservicios:** descartado por costo operativo desproporcionado para el tamaño
  del instituto y la rotación de mantenedores.
- **Todo monolito (Finance incluido):** descartado porque Finance tiene razón de negocio
  (compliance AFIP) y bajo acoplamiento que justifican aislarlo, y porque es el caso de
  estudio de la tesis.
- **Finance en la misma instancia pero mismo schema:** descartado — no da aislamiento real
  (permitiría FKs cruzadas y acoplamiento oculto por la base compartida).
