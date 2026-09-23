# Análisis backend y estrategia P0

## Fuentes y estado inicial

Se contrastaron controladores, handlers, DTOs, configuraciones EF, migraciones, las guías `academic-endpoints-e2e-guide.md` y `student-management-module-3-e2e-guide.md`, y el Swagger dinámico `/swagger/v1/swagger.json`. No existe un `swagger-academiaDigital.json` versionado ni una suite Playwright previa.

La implementación es la fuente de verdad para contratos y estados. Las políticas de seguridad documentadas se conservan como expectativa, de modo que una exposición involuntaria produzca una prueba fallida.

## Estado después del refactor multi-carrera

- `User 1–1 Student` queda garantizado por índice único y por transacciones en registro/alta administrativa.
- El legajo, estado y `Student.CareerId` continúan siendo globales; `CareerId` representa la carrera principal estable.
- `StudentCareers` es la fuente de membresías activas y admite varias carreras por Student.
- Planes, asignaciones e inscripciones referencian `StudentCareerId`; los índices vigentes se aplican por membresía.
- Registro y `POST /students` crean todas sus relaciones en una única transacción o no persisten ninguna.
- `StudentAcademicController` exige Admin o propietario para lecturas y Admin para asignar plan.

## Relaciones y orden válido

```text
User (Alumno) 1──1 Student (legajo y carrera principal)
                       └── StudentCareer (una por carrera)
                           ├── StudentStudyPlan ── StudyPlan ── StudyPlanCourse ── Course
                           ├── StudentAcademicAssignment ── Commission
                           └── Enrollment ── EnrollmentPeriod + StudyPlanCourse + Course
```

Orden del fixture principal:

```text
Admin → Career → Courses → StudyPlan → StudyPlanCourses → activar plan
      → Commission → User sin Student → Student → StudentStudyPlan
      → StudentAcademicAssignment → EnrollmentPeriod → Enrollment
```

## Inventario P0 real

| Módulo | Métodos y rutas | Éxito real | Acceso implementado |
|---|---|---|---|
| Auth | `POST users/login`, `checkSession`, `logout`; `GET users/profile`; `POST users/register` | 200/201 | Login/register públicos; resto sesión activa |
| Careers | CRUD `/careers` | GET 200, POST 201, PUT/DELETE 204 | Público |
| Courses | CRUD `/careers/{careerId}/courses` | GET 200, POST 201, PUT/DELETE 204 | Público |
| StudyPlans | listar/crear/actualizar/activar/agrupado | 200/201/204 | Público |
| StudyPlanCourses | listar/agregar/actualizar/eliminar | 200/201/204 | Público |
| Commissions | CRUD `/careers/{careerId}/commissions` | 200/201/204 | Admin |
| Students | listar/crear/obtener/actualizar/baja; listar/agregar carreras | 200/201/204 | Listado y escritura Admin; lectura Admin o propietario |
| StudentAcademic | study-plan, eligible-courses, academic-progress | 204/200 | Escritura Admin; lectura Admin o propietario |
| StudentManagement | assignments, record, history | 200/201 | Escritura Admin; lectura Admin o propietario |
| Enrollments | períodos, inscripción, consultas y bajas | 200/201 | Cualquier sesión activa; sin diferenciación Admin |

## Contratos y reglas confirmadas

- `register` requiere una carrera activa, crea atómicamente User Alumno, Student y StudentCareer y devuelve `userID`.
- Los endpoints de login/perfil serializan `UserRole` numéricamente (`Alumno=1`, `Profesor=2`, `Admin=3`).
- Estados de Student: `Regular`, `Libre`, `Graduated`, `Withdrawn`.
- `GET /students` devuelve `{items,page,pageSize,total}`.
- Una asignación académica exige membresía activa y que plan/comisión/ciclo/año pertenezcan a la carrera indicada.
- Puede existir un `StudentStudyPlan` y una asignación actuales por cada StudentCareer.
- Activar un plan marca el elegido `Active` y todos los demás de la carrera `Archived`.
- El enrollment usa `StudyPlanCourseIds`, exige membresía en la carrera del período y que todas las materias pertenezcan al plan del período.
- Errores lanzados por middleware tienen `{success:false,msg}`; errores capturados en algunos controladores usan `ProblemDetails`.

## Inconsistencias y defectos a evidenciar

1. La guía académica antigua usa estado `Active` y omite `careerId` en register; el código actual usa `Regular` y exige carrera.
2. Careers, Courses, StudyPlans y StudyPlanCourses continúan públicos aunque Swagger aplica Bearer globalmente.
3. Enrollment ya verifica cupos concurrentes por turno, correlativas y elegibilidad; todavía no devuelve las advertencias de correlativas `Soft` en el contrato de alta.
4. Los turnos difieren entre módulos: inglés en Commission y español en Enrollment.
5. Swagger declara Bearer globalmente, pero no expresa los guards reales ni contratos completos de respuesta/error.
6. Las eliminaciones API no alcanzan para limpiar un flujo con asignaciones; el modo aislado requiere cleanup directo controlado. El modo de población omite cleanup para conservar el grafo académico generado.

## Priorización

- P0 implementado: auth, catálogo, comisión, alta Student, plan, asignación, enrollment, consultas, validaciones esenciales y autorización.
- P1 implementado en suite: requisitos/documentos, becas, custom fields tipados y autorización propia/cruzada.
- P1 pendiente: exponer advertencias `Soft`, completar estado/historial y CRUD restante.
- P2: Admin, certificados, calendario, concurrencia, carga y expiración firmada de JWT.

## Hallazgo P1 corregido

`DELETE /api/v1/student-custom-fields/{id}` cambia `IsActive` a `false`. `GetCustomFieldsAsync` filtra ahora por `IsActive`, por lo que el listado excluye la definición y la consulta por ID devuelve `404`; los valores históricos permanecen conservados pero no se exponen.
