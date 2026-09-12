# Mapa de seams (costuras) y riesgo de acoplamiento

> **Propósito.** El sistema es un **monolito modular** (ver
> [ADR 0001](../adr/0001-finance-como-servicio-resto-monolito.md)). Este documento **no**
> propone refactorizar ahora: es un **mapa de riesgo** para una futura cohorte que,
> si el instituto crece, necesite extraer otro módulo. Marca dónde el acoplamiento actual
> —principalmente navegaciones EF Core que cruzan módulos— haría cara esa extracción, y
> con qué criterio cortarlo (el mismo que se aplicó a Finance: **referencias por id +
> servicio/HTTP en vez de navegación/JOIN cuando el acoplamiento no es inherente al
> negocio**).

## Módulos actuales

| Módulo | Entidades núcleo |
|--------|------------------|
| **Academic** | Career, Course, CourseType, StudyPlan, StudyPlanCourse, CoursePrerequisite |
| **Identity** | User, ActiveSession, Administrative |
| **Students/Enrollments** | Student, StudentCareer, Enrollment, EnrollmentPeriod, StudentStudyPlan, StudentRematriculation, StudentManagement* |
| **Teachers** | Teacher, TeachingPosition, TeacherAssignment, TeacherDocument, Commission |
| **Attendance/Grades** | AttendanceSession/Record/Justification, Gradebook*, ExamTable* |
| **Admissions** | AdmissionForm, AdmissionApplication(+Document/StatusHistory), AdmissionAgreement, OutboxMessage |
| **Certificates** | CertificateRequest, CertificateIssuance, CertificateSequence |
| **Finance** | *(ya extraído a servicio — ver ADR 0001)* |

## Tipos de acoplamiento y su riesgo

### 1. `User` como columna de auditoría (RIESGO BAJO — no bloquea extracción)

Casi todos los módulos tienen navegaciones `CreatedByUser`, `ReviewedByUser`,
`ReopenedByUser`, `ClosedByUser`, etc. hacia `User` (quién hizo la acción). Ejemplos:
Attendance (`CreatedByUser`, `ClosedByUser`, `ReopenedByUser`, `UpdatedByUser`),
Gradebook/ExamTable (`CreatedByUser`, `SubmittedByUser`, `PublishedByUser`, …),
Admissions (`ReviewedByUser`, `ChangedByUser`), Certificates (`ReviewedByUser`,
`IssuedByUser`).

- **Por qué es bajo riesgo:** son metadatos de auditoría, no relaciones de negocio. Ya
  tienen el `...UserId` escalar al lado de la navegación.
- **Criterio de corte (si se extrae el módulo):** eliminar la navegación a `User`, dejar
  solo `...UserId` (igual que se hizo en Finance). El nombre del usuario, si hace falta
  mostrarlo, se pide por HTTP y se cachea. **Esto es mecánico y de bajo riesgo.**

### 2. Estructura académica compartida (RIESGO MEDIO)

`Course`, `StudyPlan`, `Career` son referenciados por varios módulos:
- Enrollment → Course, StudentCareer; EnrollmentPeriod → Career, StudyPlan
- Attendance/Gradebook/ExamTable → Course (+ Commission, TeachingPosition)
- AdmissionForm → Career; CoursePrerequisite → StudyPlan, Course

- **Por qué es medio:** estos módulos **consultan** la estructura académica de forma
  legítima (una planilla de notas ES de una materia). No es acoplamiento accidental, pero
  tampoco requiere una FK física si se separara.
- **Criterio de corte:** si se extrajera, por ejemplo, Attendance/Grades, se reemplazarían
  las navegaciones a `Course`/`Commission`/`TeachingPosition` por ids + un cliente de
  lectura al módulo Academic/Teachers. **Costo medio:** hay bastantes queries que hoy
  hacen `.Include(...)` sobre estas navegaciones y habría que rearmarlas.

### 3. Enrollment ↔ Attendance/Grades (RIESGO ALTO — el más caro de separar)

`AttendanceRecord.Enrollment`/`.Student`, `Gradebook`/`ExamTable` → `Enrollment`,
`GradeEntry`/`ExamResult` referencian `Enrollment`/`Student`. Attendance y Grades están
**fuertemente entrelazados con Enrollment**: una asistencia/nota es *de una inscripción a
una materia*.

- **Por qué es alto:** es acoplamiento **inherente al negocio** (no accidental). La nota y
  la asistencia no existen sin la inscripción. Separar Grades/Attendance de
  Students/Enrollments implicaría consistencia distribuida sobre datos que cambian juntos
  (cerrar una cursada, recalcular condición) — el tipo de cosa que microservicios hace
  cara.
- **Criterio:** **no separar** salvo necesidad extrema. Si alguna vez se hiciera, mantener
  Enrollment + Attendance + Grades **en el mismo servicio** (son un bounded context).

### 4. Admissions → Academic/Identity (RIESGO MEDIO-BAJO)

AdmissionForm → Career/Commission; AdmissionApplication → AdmissionForm; documentos →
User. Admissions es bastante autónomo (ya tiene su propio OutboxMessage para integración
asíncrona) y de los mejores candidatos a una futura extracción si hiciera falta.

- **Criterio de corte:** cortar `Career`/`Commission`/`User` a ids + lectura HTTP. El
  Outbox ya existente facilita la integración event-driven. **Costo medio-bajo.**

## Recomendaciones para nuevas features (para no empeorar los seams)

1. Al agregar una navegación EF **entre módulos distintos**, preguntarse: ¿es negocio
   inherente (mismo bounded context) o solo "quiero mostrar el nombre"? Si es lo segundo,
   dejar el `...Id` y resolver el nombre por servicio, no agregar navegación.
2. Nunca agregar una FK/navegación **hacia Finance** (ya es un servicio con otra base).
3. Preferir `...Id` + servicio de dominio sobre `.Include()` cruzando módulos cuando el
   dato es solo de presentación.
4. Mantener juntas las entidades del mismo bounded context (Enrollment+Attendance+Grades).

## Resumen de riesgo por candidato de extracción futura

| Candidato | Riesgo de extracción | Comentario |
|-----------|----------------------|------------|
| **Finance** | ✅ Ya hecho | Bajo acoplamiento real; ejemplo del criterio. |
| **Admissions** | 🟡 Medio-bajo | Autónomo, ya tiene Outbox. Mejor próximo candidato. |
| **Certificates** | 🟡 Medio | Depende de User + StudentCareer; acotado. |
| **Attendance/Grades** | 🔴 Alto | Inherente a Enrollment; NO separar de Students. |
| **Academic (Career/Course/Plan)** | 🔴 Alto | Es el núcleo referenciado por casi todos. |
| **Identity (User)** | 🔴 Alto | Referenciado por auditoría en todos lados. |
