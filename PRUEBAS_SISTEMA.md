# Pruebas de sistema — AcademiaDigital

## Objetivo

Verificar que AcademiaDigital, desplegado de forma integrada (frontend Angular, API .NET,
base de datos PostgreSQL y microservicio Finance), satisface los requisitos funcionales
definidos para los roles de alumno, profesor y administrador, respeta las reglas académicas
de la institución y protege la información según el rol de cada usuario.

Las pruebas de sistema verifican el sistema integrado completo contra sus requisitos
especificados, como caja negra ([glosario ISTQB, "system testing"](https://glossary.istqb.org/)).
Complementan a las pruebas unitarias y de arquitectura existentes en `backend-dotnet/tests/`,
que validan las reglas de negocio de forma aislada pero no la integración entre capas ni
entre roles.

### Qué se busca validar

1. Que cada actor puede completar su trabajo de principio a fin.
2. Que las reglas de negocio (correlatividades, cupos, condición, emisión de certificados) se
   cumplen en el sistema real.
3. Que los datos fluyen correctamente entre roles y módulos (por ejemplo, la nota que carga el
   profesor se refleja en el historial del alumno).
4. Que la seguridad y el aislamiento por rol funcionan.
5. Que el sistema maneja correctamente los errores y la caída de servicios dependientes.
6. Atributos de calidad no funcionales: rendimiento, seguridad, accesibilidad y compatibilidad.

## Convenciones

- **Vía UI:** la prueba se ejecuta por el navegador.
- **Vía API:** el módulo existe en el backend pero no tiene pantalla en Angular (admisiones,
  mesas de examen y finanzas). Se considera prueba de sistema porque se ejecuta contra el
  sistema desplegado completo.
- **Prioridad Alta:** un fallo rompe un flujo central o compromete la seguridad.
- **Resultado esperado:** deducido del código fuente. Debe contrastarse con el documento de
  requisitos de la tesis.

## Ambiente de prueba

- Sistema levantado con `docker compose up --build -d` y
  `docker compose -f docker-compose.finance.yml up --build -d`.
- Datos iniciales cargados con los scripts de [seeds/](seeds/README.md), en el orden indicado.
- Herramienta principal: [Playwright](https://playwright.dev/docs/intro).
- Las credenciales de prueba se leen de variables de entorno y no se escriben en los casos.

## Casos de prueba

### Autenticación (7)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-01 | Alumno, profesor, admin | Inicio de sesión válido con cada rol | Llega a `/app/dashboard/default` con el menú de su rol | UI | Alta |
| SIS-02 | Cualquiera | Inicio de sesión con credenciales inválidas | Muestra un error y no sale de `/auth/signin` | UI | Alta |
| SIS-03 | Visitante | Registro de un usuario nuevo | La cuenta se crea y puede iniciar sesión | UI | Media |
| SIS-04 | Cualquiera | Olvidé mi contraseña / restablecerla | Entra con la contraseña nueva; la anterior ya no sirve | UI | Media |
| SIS-05 | Cualquiera | Cambio de contraseña desde el perfil | La contraseña anterior deja de funcionar | UI | Media |
| SIS-06 | Cualquiera | Cerrar sesión | No vuelve a `/app` ni con el botón atrás ni escribiendo la URL | UI | Alta |
| SIS-07 | Visitante | Entrar a `/app/...` sin sesión | Redirige al login | UI | Alta |

### Autorización y aislamiento (3)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-08 | Alumno | Abrir `/app/admin` escribiendo la URL | Acceso bloqueado | UI | Alta |
| SIS-09 | Alumno | Pedir certificados, deuda o asistencia de otro alumno | Respuesta 403 o 404, sin datos ajenos | API | Alta |
| SIS-10 | Profesor | Ver y editar planillas, asistencia y calendario | Solo ve las comisiones que tiene asignadas | UI | Alta |

### Administración académica (9)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-11 | Admin | Crear una carrera | Aparece en Gestión de carreras | UI | Alta |
| SIS-12 | Admin | Importar un plan de estudios | Se cargan las materias y las correlativas | UI | Media |
| SIS-13 | Admin | Comparar dos versiones de un plan | Muestra las diferencias correctas | UI | Baja |
| SIS-14 | Admin | Cargar una correlatividad que forma un ciclo | El sistema la rechaza | UI | Media |
| SIS-15 | Admin | Crear comisiones y asignarles alumnos | El alumno queda en esa comisión | UI | Alta |
| SIS-16 | Admin | Alta de docente con legajo y documentos | El docente queda registrado con su documentación | UI | Media |
| SIS-17 | Admin | Crear un cargo docente y asignar el docente a una comisión | El profesor ve esa comisión al iniciar sesión | UI | Alta |
| SIS-18 | Admin | Crear un usuario con un rol, o cambiarle el rol | Sus accesos corresponden al rol nuevo | UI | Media |
| SIS-19 | Admin | Configurar reglas de aprobación | La condición de los alumnos se calcula con esas reglas | UI | Media |

### Períodos e inscripción (11)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-20 | Admin | Crear un período, fijar cupos y activarlo | El alumno ve el período activo | UI | Alta |
| SIS-21 | Alumno | Ver las materias disponibles para inscribirse | Solo aparecen las que habilitan sus correlativas | UI | Alta |
| SIS-22 | Alumno | Inscribirse a una materia habilitada | Aparece en Mis inscripciones y en Inscriptos del admin | UI | Alta |
| SIS-23 | Alumno | Inscribirse sin tener las correlativas aprobadas | La inscripción se rechaza | UI/API | Alta |
| SIS-24 | Alumno | Inscribirse cuando el cupo está lleno | Rechazo, o queda en espera por orden de llegada | UI | Alta |
| SIS-25 | Alumno | Cancelar su propia inscripción | Se libera el cupo | UI | Media |
| SIS-26 | Admin | Aprobar manualmente una inscripción | Cambia el estado y el alumno lo ve | UI | Media |
| SIS-27 | Admin | Cerrar el período | No se aceptan inscripciones nuevas | UI | Alta |
| SIS-28 | Admin | Ver el reporte de inscripción y la cobertura por comisión | Las cifras coinciden con las inscripciones hechas | UI | Media |
| SIS-29 | 2 alumnos | Inscribirse al último cupo al mismo tiempo | Entra uno solo | API | Alta |
| SIS-30 | Alumno, admin | Rematriculación | El alumno queda matriculado en el ciclo nuevo | UI/API | Media |

### Admisiones (5)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-31 | Postulante | Enviar una postulación con el formulario público | Queda registrada | API | Alta |
| SIS-32 | Postulante | Postular sin token antiabuso, o demasiadas veces seguidas | 403 sin token; 429 al pasar el límite | API | Media |
| SIS-33 | Admin | Revisar documentos y cambiar el estado de la postulación | Los estados cambian según las transiciones permitidas | API | Media |
| SIS-34 | Postulante | Postular con el cupo del formulario agotado | Rechazo | API | Media |
| SIS-35 | Postulante | Descargar el convenio | Se descarga un PDF válido | API | Baja |

### Asistencia (5)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-36 | Profesor, alumno | El profesor crea una sesión y marca presentes y ausentes | El alumno ve su asistencia en Mi asistencia | UI | Alta |
| SIS-37 | Profesor | Cerrar la sesión y después reabrirla | Cerrada no se puede editar; reabierta sí | UI | Media |
| SIS-38 | Alumno, profesor | Justificar una inasistencia | El resumen de asistencia se recalcula | UI | Media |
| SIS-39 | Admin | Revisar un alumno que superó el límite de faltas | Figura en riesgo o con la condición correspondiente | UI | Alta |
| SIS-40 | Profesor | Exportar una sesión | El archivo trae los datos correctos | UI | Baja |

### Calificaciones (5)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-41 | Profesor | Configurar las evaluaciones y cargar notas | Las notas quedan guardadas en la planilla | UI | Alta |
| SIS-42 | Profesor, admin, alumno | Planilla enviada, aprobada y publicada | El alumno ve las notas solo después de publicarlas | UI | Alta |
| SIS-43 | Profesor, admin | Cerrar la planilla y después reabrirla | Cerrada no se puede editar; reabierta sí | UI | Media |
| SIS-44 | Alumno | Consultar el historial después de que se publican las notas | La materia muestra el estado correcto | UI | Alta |
| SIS-45 | Profesor | Cargar una nota fuera de rango | Rechazo con un mensaje de validación | UI | Media |

### Mesas de examen (2)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-46 | Admin, profesor | Crear mesa, inscribir alumno, cargar resultados y publicar | El historial del alumno se actualiza | API | Alta |
| SIS-47 | Alumno | Inscribirse a una mesa sin estar habilitado | Rechazo | API | Media |

### Historial y condición (2)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-48 | Alumno | Ver el historial académico y el progreso | Estado real por materia y porcentaje de avance correcto | UI | Alta |
| SIS-49 | Admin | Ver la condición de los alumnos | Coincide con sus notas y su asistencia | UI | Media |

### Calendario (4)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-50 | Profesor, alumno | El profesor crea un evento para su comisión | Los alumnos de esa comisión lo ven | UI | Alta |
| SIS-51 | Alumno de otra comisión | Revisar el calendario | No ve ese evento | UI | Alta |
| SIS-52 | Profesor, alumno | Editar y borrar el evento | Los cambios se reflejan en el calendario del alumno | UI | Media |
| SIS-53 | Alumno | Ver los próximos eventos en el dashboard | Aparecen ordenados por fecha | UI | Baja |

### Certificados (5)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-54 | Alumno, admin | El alumno solicita un certificado | El admin lo ve como pendiente | UI | Alta |
| SIS-55 | Admin, alumno | Aprobar y emitir el certificado | El alumno descarga un PDF con formato institucional y datos correctos | UI | Alta |
| SIS-56 | Admin, alumno | Rechazar con un motivo | El alumno ve el rechazo y el motivo | UI | Media |
| SIS-57 | Admin | Emitir el mismo certificado dos veces al mismo tiempo | Se genera uno solo | API | Media |
| SIS-58 | Alumno | Descargar el certificado de otro alumno usando su identificador | Acceso denegado | API | Alta |

### Finanzas (8)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-59 | Admin, alumno | Configurar concepto, tarifa, beneficio y plan, y generar la deuda | El alumno consulta su deuda correcta | API | Alta |
| SIS-60 | Admin | Matricular a un alumno | Se genera su deuda en Finance | API | Alta |
| SIS-61 | Admin | Matricular con el servicio Finance apagado | La matriculación igual se completa | API | Alta |
| SIS-62 | Admin | Registrar y confirmar un pago | La deuda baja por el monto pagado | API | Alta |
| SIS-63 | Admin | Enviar el mismo pago dos veces | Se registra una sola vez | API | Alta |
| SIS-64 | Admin | Revertir un pago | La deuda vuelve a su valor anterior | API | Media |
| SIS-65 | Admin | Generar un recibo | Número correlativo, PDF descargable y hash válido | API | Media |
| SIS-66 | Alumno | Descargar su comprobante | Se descarga con formato institucional | UI | Media |

### Otros (2)

| ID | Actor | Caso | Resultado esperado | Vía | Prioridad |
|---|---|---|---|---|---|
| SIS-67 | Cualquiera | Editar los datos del perfil | Los cambios se mantienen al volver a iniciar sesión | UI | Baja |
| SIS-68 | Cualquiera | Mensajes | Pendiente: la pantalla no tiene endpoint en la API; confirmar si entra en el alcance | UI | Baja |

### No funcionales (7)

| ID | Atributo | Caso | Resultado esperado | Herramienta | Prioridad |
|---|---|---|---|---|---|
| SIS-69 | Rendimiento | Muchos alumnos inscribiéndose a la vez | Tiempo de respuesta p95 bajo el umbral definido (pendiente de definir) | [k6](https://grafana.com/docs/k6/latest/) | Media |
| SIS-70 | Seguridad | Escaneo pasivo del frontend y la API | Ninguna alerta de riesgo alto | [ZAP baseline](https://www.zaproxy.org/docs/docker/baseline-scan/) | Media |
| SIS-71 | Seguridad | Usar un token JWT vencido o alterado | Respuesta 401 | Playwright | Alta |
| SIS-72 | Accesibilidad | Revisar las pantallas principales | Ninguna violación crítica | [axe con Playwright](https://playwright.dev/docs/accessibility-testing) | Baja |
| SIS-73 | Compatibilidad | Los flujos de prioridad Alta en otros navegadores | Pasan en Chromium, Firefox y WebKit | [Proyectos de Playwright](https://playwright.dev/docs/test-projects) | Baja |
| SIS-74 | Usabilidad | Usar la app en ancho de celular | Se puede navegar y operar | Playwright, viewport móvil | Baja |
| SIS-75 | Confiabilidad | Reiniciar el backend y la base de datos | No se pierden datos | Docker Compose | Baja |

## Resumen

| Módulo | Casos |
|---|---|
| Autenticación | 7 |
| Autorización y aislamiento | 3 |
| Administración académica | 9 |
| Períodos e inscripción | 11 |
| Admisiones | 5 |
| Asistencia | 5 |
| Calificaciones | 5 |
| Mesas de examen | 2 |
| Historial y condición | 2 |
| Calendario | 4 |
| Certificados | 5 |
| Finanzas | 8 |
| Otros | 2 |
| No funcionales | 7 |
| **Total** | **75** |

| Prioridad | Casos |
|---|---|
| Alta | 36 |
| Media | 29 |
| Baja | 10 |

Si el tiempo disponible es limitado, los 36 casos de prioridad Alta cubren los flujos centrales
y la seguridad.

## Pendientes

- Contrastar los resultados esperados con el documento de requisitos de la tesis.
- Definir el umbral de rendimiento para SIS-69.
- Confirmar si el módulo de Mensajes (SIS-68) entra en el alcance.
- La suite de regresión de API en `backend-dotnet/tests/api-regression` todavía está
  configurada para SQL Server, pero el backend ahora usa PostgreSQL. Hay que adaptarla antes
  de reutilizarla para los casos vía API.
