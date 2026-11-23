# Changelog

Cambios relevantes de AcademiaDigital por versión. El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el número de versión, [versionado semántico](https://semver.org/lang/es/). Ver [VERSIONADO.md](VERSIONADO.md).

## [Sin publicar]

## [1.17.0] - 2026-11-23

### Agregado

- **Tour guiado para la demo (alumno)**: las cuentas con correo `@demo.com` ven un recorrido interactivo que resalta cada parte del sistema, oscurece el resto y explica para qué sirve, con Anterior/Siguiente, contador de pasos y botón para cerrar. El tour del alumno tiene 15 pasos (inicio, menú, accesos rápidos, plan de estudios, requisitos de ingreso, prematrícula, inscripción a materias, mis inscripciones, mesas de examen, calificaciones, asistencias, historial académico, certificados y calendario) y navega solo entre las pantallas.
  - Arranca solo la primera vez que entra una cuenta de demo; después se relanza con el botón "Ver tour" de la barra superior o con `?tour=student` en la URL.
  - Se maneja con el teclado (flechas para avanzar o retroceder, Esc para cerrar). Si un elemento no aparece (por ejemplo, porque no hay datos), ese paso se saltea.
  - En el celular, el paso del menú señala el botón que lo abre.
  - Las demás cuentas no ven nada. Los pasos de cada rol están en archivos de configuración propios (`core/tour/tours/`), para sumar después los de docente y administración.
  - Usa la librería `ngx-ui-tour-md-menu` 16.0.0 (compatible con Angular 21; la 17 ya pide Angular 22).

## [1.16.0] - 2026-11-22

Escuelas, cupos libres en Comisiones, inscripción ya enviada visible para el alumno y mejoras en Asignar materia. También corrige el menú de Administración de la 1.15.0.

### Agregado

- **Agregar escuelas**: ítem nuevo del menú de Administración con su pantalla para crear, editar y dar de baja escuelas, y los pasos para usarlas (asignar cada división a su escuela y elegirla en Asignar materia). La sección de escuelas sigue también en Gestión de Carreras.
- **Inscripción a materias**: si el alumno ya se inscribió en el período abierto, al entrar ve esa inscripción (fecha, turno y materias) en lugar del formulario, con el aviso de que no puede volver a inscribirse en ese período. El botón "Dar de baja la inscripción" (con confirmación) la quita y vuelve a mostrar el formulario para inscribirse de nuevo.
- **Asignar materia**: cada año de la carrera tiene un fondo de color distinto (1.º celeste, 2.º verde, 3.º amarillo). Al pasar el mouse por un docente se ven las materias que tiene asignadas (con su división) en la carrera, ciclo y escuela elegidos, en lugar del legajo y el correo.
- **Comisiones**: la columna Estado muestra los cupos libres de cada comisión con docente ("29 cupos libres", o "Sin cupos" si está llena) en lugar de "Ocupado". Las comisiones sin docente siguen diciendo "Vacante". Los inscriptos se cuentan igual que en el desplegable de alumnos de la comisión.

### Cambiado

- **Cursos**: lo que se llamaba "División" (el grupo de alumnos de un año, turno y escuela) ahora se llama "Curso" en todas las pantallas, menús y mensajes, como en las planillas del instituto: "Divisiones" pasa a "Cursos", "Asignar división a alumnos" a "Asignar curso a alumnos" y el filtro "División" de Calificaciones y Asistencia a "Curso". La comisión sigue siendo una materia dictada a un curso, con su docente. Los mensajes de error de los cursos que decían "comisión" por herencia ahora dicen "curso". Solo cambian los textos; la base de datos no.
- **Escuelas**: lo que se llamaba "Sede" ahora se llama "Escuela" en todas las pantallas y mensajes (Divisiones, Asignar materia, Gestión de Carreras y el filtro de planillas, que ahora muestra las escuelas reales en lugar de valores de ejemplo).

### Corregido

- **Menú de Administración**: la 1.15.0 había quitado ítems ("Asignaciones" y "Asignar división a alumnos"), había renombrado otros ("Gestión de Inscripciones", "Gestión de Usuarios", "Gestión de Carreras", "Acciones masivas", "Requisitos de ingreso", entre otros) y había reemplazado los desplegables por secciones fijas. Vuelven todos los ítems con su nombre de antes, agrupados en desplegables: Admisión (Aspirantes, Requisitos de ingreso, Gestión de Inscripciones), Alumnos (Alumnos, Asignar división a alumnos, Analíticos, Acciones masivas), Docentes (Listado de docentes, Comisiones, Divisiones, Asignar materia, Asignaciones), Registros Académicos, Certificados y Mesa de examen. El desplegable de la página actual se abre solo. Los títulos de las páginas vuelven a ser los de antes.
- **Acciones masivas**: vuelve a mostrar el egreso y la inscripción por división en la misma pantalla. Desde el botón "Inscribir en materias" de Divisiones sigue abriendo solo la inscripción, con la carrera elegida.
- **Gestión de Inscripciones**: vuelven el título y el botón "Activar inscripción". Se mantienen las mejoras de la 1.15.0: confirmación al cerrar, "Eliminar período" en un menú aparte y estado como etiqueta.

## [1.15.0] - 2026-11-22

### Cambiado

- **Menú de Administración**: reorganizado según el recorrido del alumno, con secciones de título fijo: Admisión, Personas, Cursado, Exámenes, Trámites y egreso, Configuración y Herramientas. Cada ítem tiene un ícono distinto. Las URL y los permisos no cambiaron; los roles de gestión con permisos recortados ven solo las secciones que les corresponden. Los menús de alumno y docente no cambiaron.
- **Nombres**: "Gestión de Inscripciones" pasa a "Períodos de inscripción"; "Requisitos de ingreso" (bandeja de revisión) a "Documentación de ingreso"; "Listado de docentes" a "Docentes"; "Asignar materia" a "Asignación docente"; "Armar mesas automáticas" a "Mesas de examen"; "Formulario de inscripción" a "Formulario de mesas"; "Peticiones" a "Solicitudes de certificados"; "Acciones masivas" a "Egresos"; "Gestión de Carreras" a "Carreras"; "Reportes y Excel" a "Reportes"; "Gestión de Usuarios" a "Usuarios y permisos"; "Calendario" a "Calendario académico". El título de cada página coincide con su nombre en el menú; en Calendario y Certificados, que comparten pantalla con alumno y docente, el título nuevo lo ve solo el personal.
- **Divisiones**: botones "Asignar alumnos" (la pantalla que antes era "Asignar división a alumnos") e "Inscribir en materias" (inscribe una división en las materias de su año, con la carrera ya elegida). "Asignaciones" sale del menú de Administración: repetía en modo lectura lo que muestran Comisiones y Plan de estudios (el docente la sigue viendo como "Mis asignaciones").
- **Egresos**: la pantalla muestra solo el egreso; la inscripción por división se abre desde Divisiones.
- **Períodos de inscripción**: el botón principal dice "Abrir período de inscripción"; "Cerrar" pasa a "Cerrar inscripción" (con confirmación) y "Activar" a "Reabrir inscripción"; "Eliminar período" se movió a un menú aparte (⋮), lejos de "Cerrar inscripción"; los botones de ícono tienen texto de ayuda ("Estadísticas", "Editar"); el estado ("Abierto"/"Cerrado") es una etiqueta que ya no parece botón, y el contador de "Ver inscriptos" es neutro en vez de rojo.

## [1.14.0] - 2026-11-22

### Agregado

- **Sedes**: en Gestión de Carreras hay una sección "Sedes" para dar de alta, editar y dar de baja las escuelas del instituto (nombre y dirección). Cada división se asigna a una sede desde Docentes → Divisiones, y la tabla de divisiones muestra la sede. Una sede con divisiones activas no se puede dar de baja.
- **Asignar materia**: desplegable "Sede" antes de la carrera. Al elegir una sede se ven solo sus divisiones y comisiones; "Todas las sedes" muestra todo. Las divisiones creadas antes de las sedes quedan "Sin sede" y se ven con "Todas las sedes".
- **Base de datos**: migración `AddCampuses` (tabla de sedes y columna opcional de sede en las divisiones). Solo agrega; no modifica datos existentes.

### Cambiado

- **Calificaciones**: el docente carga y corrige notas en cualquier estado de la planilla (borrador, enviada, aprobada o publicada) y la planilla conserva su estado; cada cambio queda en el historial de revisiones. Solo una planilla cerrada queda bloqueada (Secretaría puede reabrirla).
- **Calificaciones**: el botón de envío está siempre visible mientras la planilla no esté cerrada; si ya se envió, dice "Reenviar a Secretaría" y actualiza la fecha del último envío.
- **Calificaciones**: se puede enviar la planilla con instancias sin nota (envío parcial). La nota completa (o ausente) en todas las instancias regulares se exige recién al cerrar.
- **Calificaciones**: vaciar una celda que tenía nota y guardar borra esa nota.
- **Cargar asistencia**: es una matriz con los alumnos de la materia en la primera columna (se ven aunque todavía no haya días cargados) y los días de clase del mes como columnas, con el día de la semana. Cada celda es un botón: un toque rota entre Presente, Ausente y Tarde, con colores. Cada día tiene "Todos presentes" para marcar P a los que faltan cargar.
- **Cargar asistencia**: botón "Hoy" para agregar la clase del día con un toque ("Nuevo día" queda para otras fechas o clases por horas) y "Guardar" siempre visible arriba, con la cantidad de cambios sin guardar. Se guardan los cambios de cualquier mes.
- **Asignar materia** (Administración → Docentes): pantalla nueva con arrastrar y soltar. Se elige la carrera y el ciclo; los docentes aparecen como píldoras (con la cantidad de comisiones que ya tienen) y las materias de todos los años como cuadros, cada uno con un desplegable de sus comisiones. Se sueltan los docentes sobre las materias, al tocar una materia se elige la división y el cargo de cada uno, y nada se escribe en la base hasta tocar "Guardar cambios" ("Descartar" deshace lo movido). Funciona con mouse y en pantallas táctiles.
- **Comisiones** (Administración → Docentes): cada fila se despliega con la flecha y muestra los alumnos inscriptos en esa comisión (nombre, legajo y DNI) y cuántos son frente al cupo.
- **Cargar asistencia**: el mes se muestra en castellano ("Noviembre 2026"); antes el selector del navegador lo mostraba en inglés.
- **Inscripción a materias**: la carrera del alumno queda elegida al entrar (la de su plan de estudios), sin tener que buscarla en el desplegable. Después de inscribirse, la carrera se conserva y las materias pedidas aparecen como inscriptas.
- **Inscripción a materias**: el botón "Solicitar inscripción" flota abajo de la pantalla mientras se recorre la lista y muestra cuántas materias hay tildadas, por ejemplo "Solicitar inscripción (3)".
- **Archivos**: el tamaño máximo de los documentos que se suben (inscripción y legajo docente) pasa de 10 MB a 5 MB: alcanza para un PDF escaneado o una foto de celular y cuida la memoria del servidor.

### Corregido

- **Calificaciones**: al enviar la planilla, las celdas vacías (incluidas recuperaciones e IEFI) se completaban con 0, y los alumnos quedaban libres por error. Ya no se completa nada.

- **Archivos**: en producción no se podía subir ningún archivo de más de 1 MB (el nginx del frontend usaba su límite por defecto y respondía 413), aunque la pantalla aceptaba hasta 10 MB. Ahora nginx admite hasta 6 MB (el archivo más los datos del formulario) y la importación de planes de estudio por CSV tiene el mismo tope.

- **Calendario**: el encabezado mostraba "11 Noviembre 2026" (número de mes delante del nombre) y parecía una fecha. Ahora dice "Noviembre 2026"; las flechas siguen pasando al mes anterior y al siguiente.

## [1.13.0] - 2026-11-22

### Agregado

- **Perfil**: todos los roles (también Secretaría, Coordinación, Preceptoría, Cooperadora y Admin) cargan sus datos de contacto, con lo que piden las planillas del instituto: tipo de documento, otro teléfono (fijo o celular), domicilio, barrio, ciudad o pueblo, departamento, provincia, país, código postal, contacto de emergencia y obra social.
- **Reportes**: el Excel de alumnos suma las columnas Tipo doc., Otro teléfono, Barrio, Departamento y País.

### Cambiado

- **Perfil**: el legajo solo se muestra al alumno. El docente y el personal ya no ven ese campo; el legajo docente lo sigue viendo Administración en Docentes.
- **Datos de contacto**: se guardan en la cuenta del usuario y no en la ficha de alumno o docente. La migración `MoveContactDataToUsers` copia lo ya cargado antes de borrar las columnas viejas.

## [1.12.1] - 2026-11-22

### Cambiado

- **Requisitos de ingreso**: elegir un archivo ya no lo envía. Se muestra una vista previa (PDF o imagen, con nombre y tamaño) y se envía recién al tocar "Enviar"; "Cancelar" lo descarta. El documento ya enviado se puede ver en pantalla con "Ver", además de descargarlo.

### Corregido

- **Inicio de sesión**: la tarjeta del formulario se había ensanchado por el texto de "¿Es ingresante?"; vuelve a su ancho anterior.

## [1.12.0] - 2026-11-22

Cambios grandes de la reunión con Secretaría y login con Google.

### Agregado

- **Iniciar sesión con Google**: solo para cuentas que ya existen con ese correo (aprobadas y activas); no crea cuentas. Se activa configurando `Authentication:Google:ClientId`.
- **Reportes y Excel**: listados de alumnos, egresados, libres, bajas y aspirantes con todas sus columnas, filtrados por carrera y fechas.
- **Importar notas desde Excel** en la planilla de calificaciones (se cargan para revisar antes de guardar).
- **Prematriculación**: botón al sitio del Ministerio en el inicio de sesión y en Requisitos de ingreso, con la explicación de que sin prematrícula el instituto no puede matricular a un ingresante (https://www.cba.gov.ar/gestion-estudiantes/, configurable en `environment.preEnrollmentUrl`).
- **Bucket propio para los documentos de los alumnos** (`itsc/student-documents`): privado, cifrado y versionado, con una carpeta por alumno (`DNI_Apellido_Nombre/REQUISITO/`) y una política de solo lectura para descargarlos.
- **Acciones masivas**: egresar varios alumnos con fecha y acto administrativo; inscribir a toda una división en las materias de su año y cuatrimestre.
- **Analíticos**: datos personales y académicos con todas las materias del plan, libro matriz y folio del alumno y libro/folio del acta de cada mesa; vista para imprimir o guardar como PDF (formato provisorio).
- **Mesas de examen automáticas**: propuesta de fechas en días hábiles sin feriados, sin dos materias del mismo año el mismo día y con correlativas antes; tribunal sugerido; se ajusta y se confirma.
- **Roles de gestión**: Secretaría, Coordinación, Preceptoría y Cooperadora, con permisos por área (propuesta a validar con Dirección).

### Cambiado

- **Esquema de evaluación**: mínimo de 4 instancias en materias anuales y 2 en cuatrimestrales; formato sugerido según la materia; recuperatorios habilitados solo para quien desaprobó o faltó; condición y promedio en el panel del alumno.

## [1.11.0] - 2026-11-21

Correcciones y mejoras que surgieron de las pruebas por rol (docente, estudiante y administrador) de la versión 1.10.0.

### Agregado

- **Requisitos de ingreso**: el alumno sube el analítico y los demás documentos (PDF, JPG o PNG de hasta 10 MB) y ve si están en revisión, aprobados o rechazados (con el motivo). Administración tiene una bandeja para descargarlos, aprobarlos o rechazarlos. Se crea el requisito "Analítico de nivel secundario" si la institución no tenía ninguno. El enlace a la prematriculación del ministerio se muestra cuando se configura su dirección (`environment.preEnrollmentUrl`).
- **Datos de contacto en el perfil** (alumnos y docentes): domicilio, contacto de emergencia y obra social.
- **Editar el formato de la planilla** en borrador: nombre, nota máxima, recuperación y **fecha** de cada instancia; agregar y quitar instancias (las que no tienen notas).
- **Ausente** en las notas: se escribe "A" y cuenta como nota 0 para la condición.
- Edición de eventos propios en el calendario.
- Al entrar a Calificaciones y Asistencia, el docente ya tiene elegidas su carrera y su primera materia.
- Pipeline de GitHub Actions `Tests unitarios`: verificación de versión, tests del backend (dominio, aplicación y arquitectura), compilación de Finance, tests del frontend y build de producción en cada push.

### Cambiado

- La planilla ya no pide "Peso (%)": el 100% se reparte en partes iguales entre las instancias regulares.
- Las notas se escriben directamente en borrador (sin tocar el lápiz); el lápiz ahora edita el formato. Desplegable de notas, decimales con coma y celdas inválidas marcadas en rojo.
- Las materias se muestran sin el código interno y con su cuatrimestre o "Anual".
- Asistencia de Administración: Carrera, Año de la carrera y Turno filtran de verdad (el filtro Sede tenía valores fijos y no hay sedes cargadas en el sistema). Selector de mes y año, y leyenda P/T/A/J.
- Textos: "Plan de estudios" en lugar de "Carreras" para alumnos y docentes, "Listado de docentes", "Materias del plan", "Año de la carrera" con ayuda, "Cargar calificaciones", "Algún carácter especial" y la consigna de Mesa de examen.
- Los errores de validación muestran el detalle del campo en lugar de "Se produjo uno o más errores de validación".
- Se quita el workflow `backend.yml`, que aplicaba migraciones a una base de Azure que ya no se usa.

### Corregido

- **La página se declaraba en inglés** (`lang="en"`) y Chrome la traducía: de ahí salían "Algún personaje especial", "Área de código" y "los solicitudes". Ahora se declara en español y sin traducción automática.
- Una cuenta pasada a Profesor sin perfil docente veía las pantallas de docente con "Perfil de docente no encontrado" (así se veía la cuenta de prueba "estudiante"). Ahora el perfil se crea al cambiar el rol, y pasar a Alumno sin ficha se rechaza con un mensaje.
- El docente no podía descargar su constancia de actividad aprobada ("No tenés permiso").
- Calendario: al abrir "Nuevo evento" aparecía un error técnico de autenticación; ahora responde con un mensaje legible.
- "Próximos eventos" y el selector de materias del calendario se quedaban cargando.
- Asistencia: dos comisiones de la misma materia (por ejemplo, 1.er y 2.º cuatrimestre) compartían los días cargados; el docente no veía sus materias al elegir la carrera; al cargar un día anterior al inicio de la asignación decía "no es su materia" (ahora indica la fecha de inicio); no se validaba la fecha escrita a mano.
- La constancia de inscripción salía con caracteres rotos ("InscripciÃ³n", "MaÃ±ana"); los CSV ahora abren bien en Excel.
- Inscripción a materias: decía "Correlativa no regularizada" para materias ya inscriptas o aprobadas.
- Calificaciones: aceptaba notas fuera de rango sin avisar; el panel del alumno no cambiaba al escribir en otra fila; el lápiz se veía activo con la planilla enviada; los filtros se rompían en el celular.
- Perfil: el teléfono aceptaba cualquier cantidad de dígitos y el CUIL aceptaba letras y mostraba formatos distintos (validado también en el servidor).
- La pestaña y el panel de Plan de estudios y Mesa de examen no coincidían con Figma.
- 10 tests del frontend que fallaban (desactualizados respecto del código).

## [1.10.1] - 2026-11-20

Pantallas del docente: "Mis asignaciones" y "Alumnos".

### Agregado

- **Alumnos de una comisión sin planilla de notas**: endpoint `GET /api/v1/gradebooks/sections/{sectionId}/students` (Admin o el docente asignado a la comisión).
- Las asignaciones del docente (`TeacherAssignmentDto`) informan `careerId`, la carrera de la división de la comisión.

### Corregido

- **"Alumnos" no mostraba las materias recién asignadas al docente**: el desplegable solo listaba planillas de notas, y una materia asignada todavía no tiene planilla. Ahora también lista las comisiones asignadas (marcadas "sin planilla") y muestra sus alumnos inscriptos; la condición aparece cuando se cargan las notas. Si el docente tiene una sola materia, se abre directamente.
- **"Mis asignaciones"**: el filtro de carrera va primero y "Buscar por materia" después. Al entrar, el docente ya ve seleccionada la carrera de sus comisiones vigentes.

## [1.10.0] - 2026-11-20

Asignaciones de a varios y arreglos para celular.

### Agregado

- **Asignar división a varios alumnos a la vez** (Docentes → "Asignar división a alumnos"): se eligen carrera y división y se marcan los alumnos de una lista con buscador (nombre, legajo o DNI), "Solo sin división" y "Seleccionar todos". Cada alumno muestra su división actual. El plan de estudios, el año académico y el año del plan los toma el servidor. Endpoint `POST /api/v1/careers/{careerId}/divisions/{id}/students` (hasta 300 alumnos). Si un alumno falla, los demás se asignan igual y la pantalla lista los que no se pudieron asignar y por qué.
- **Asignar materias a un docente en un paso**, desde **Docentes → "Asignar materia"** (se elige el docente con un buscador y se ven sus materias actuales) o desde el legajo del docente ("Asignar materias"): se eligen carrera, división y cargo, y se marcan las materias del año de la división ("Ver otros años" muestra el resto del plan). Ya no hace falta crear antes la comisión: si existe una vacante se usa, y si no existe se crea con el cuatrimestre (o anual) del plan vigente. Cada materia indica si tiene comisión vacante, si la comisión se crea, si ya es del docente o si la tiene otro docente. Endpoint `POST /api/v1/teachers/{id}/assignments/bulk`.

### Corregido

- **Celular en horizontal:** no se podía desplazar la página hasta el final porque el alto usaba `100vh`, que en Android incluye la barra del navegador. Ahora usa `100dvh`.
- **"Activar inscripción"** se superponía al título "Gestión de Inscripciones" en Android: el encabezado ahora pasa el botón abajo cuando no entra.
- **Diálogos más anchos que la pantalla en el celular** (13 diálogos con ancho mínimo fijo): ahora el ancho mínimo nunca supera el de la pantalla.
- **Etiquetas cortadas en los diálogos del celular** ("Carre" en vez de "Carrera"): un estilo global pisaba el ancho que Material le da a la etiqueta flotante.
- Campos de filtro de "Condición de alumnos" y "Comisiones" que desbordaban en pantallas chicas, y la ayuda del legajo docente que se superponía en el formulario de docente.
- Las comisiones creadas para materias anuales ahora quedan marcadas como anuales (antes nunca se guardaba).

## [1.9.0] - 2026-11-15

### Agregado

- **Animaciones en la landing** (Lottie): educación en la portada, indicadores de gestión en "Cómo empezar" (sus violetas pasados al azul de la landing) y un avión de papel sobre el formulario, recoloreado con el azul metálico (el ala lleva un degradado). Provienen del proyecto `gestion-academica-frontend`.
- **Ícono de la pestaña (favicon):** el librito de Academia Digital en blanco sobre el azul metálico de la landing (`favicon.svg`, con versiones `.ico` y `apple-touch-icon.png`). Reemplaza el ícono de Angular que se veía en producción. **El ITSC muestra su propio logo** (la "it" en su azul, `favicon-itsc.svg`): la aplicación cambia el ícono al reconocer la institución por defecto. La landing, el panel y las demás instituciones usan el de Academia Digital. El librito es el ícono `auto_stories` de Material Symbols (Google, Apache 2.0).
- El reproductor (`lottie-web` 5.13.0, versión liviana solo SVG) se descarga recién cuando la animación entra en pantalla, en un fragmento aparte de 42 KB comprimido; no afecta al resto de la aplicación. Las animaciones se pausan fuera de pantalla y respetan "reducir movimiento" del sistema.

## [1.8.0] - 2026-11-15

Landing comercial y pedidos de plataforma.

### Agregado

- **Landing en `academiadigital.<dominio>`** (en local, `/landing`): azul metálico, beneficios, "Cómo empezar", "Instituciones que confían en nosotros" (ITSC) y el formulario **"Pide tu plataforma"**, con el subdominio deseado y una vista previa (`<slug>.dryvn.org`).
- **Pedidos de plataforma** (`PlatformRequests`, esquema `control`): `POST /api/v1/platform/requests` es público (5 por hora por IP, trampa para bots, subdominio validado y no tomado). Listarlos (`GET`) y revisarlos (`PATCH`) requiere la clave de plataforma. Cada pedido avisa por correo a `Platform:NotifyEmail`.
- **Panel en `admin.<dominio>`** (`/platform`): lista de pedidos con "Crear institución", que precarga subdominio, dominio `<slug>.dryvn.org` y admin inicial, y aprueba el pedido al crearla. También permite "Rechazar".
- La raíz del sitio depende del dominio: `academiadigital.*` → landing, `admin.*` → panel, cualquier otro → login de la institución.
- Infra: certificado comodín `*.dryvn.org` (unidad `itsc/wildcard-certificate`, validado en Cloudflare) para el ALB, y `Platform__NotifyEmail` en Beanstalk.

## [1.7.0] - 2026-11-15

Multitenant con base compartida. Ver [MULTITENANT_BASE_COMPARTIDA.md](MULTITENANT_BASE_COMPARTIDA.md).

### Cambiado

- **Todas las instituciones comparten la base `academiadigital`, cada una en su propio esquema.** ITSC sigue en `public` (no se migran datos), el catálogo de instituciones pasa de la base `academiadigital_control` al esquema `control`, y una institución nueva (`usb`) se crea como esquema `usb`. `Tenant.DatabaseName` pasa a `Tenant.SchemaName` (migración `TenantSchemaPerInstitution`).
- Cada esquema lleva su propio historial de migraciones.
- Nuevos identificadores reservados para instituciones: `public`, `academiadigital`, `finance-*` y `pg-*`.

### Corregido

- **El arranque ya no borra la base** si encuentra tablas sin historial de migraciones: registra un error crítico y se detiene. Antes ejecutaba `EnsureDeletedAsync`, que con la base compartida eliminaría todas las instituciones.

## [1.6.0] - 2026-11-15

Alta de usuarios controlada por la institución.

### Agregado

- **Sección "Aspirantes"** (menú del Admin): cuentas del registro público pendientes de alta, con filtro por rol (Alumnos/Profesores), búsqueda por nombre, email o DNI, fecha de registro y carrera. Botón "Dar de alta" por fila, casillas con "Seleccionar todos" y alta en masa. La pestaña "Dados de alta (últimos 7 días)" muestra las altas recientes.
- **Dominio de correo docente por institución** (`TeacherEmailDomain`): quien se registra con un email de ese dominio (en ITSC, `itscordoba.edu.ar`) queda como Profesor con su perfil docente, sin elegir carrera. Al darlo de alta, el perfil queda activo para asignarle materias y divisiones. Se configura al crear la institución en `/platform` y, para ITSC, con `Tenancy__DefaultTeacherEmailDomain`.
- API: `GET /api/v1/admin/applicants` y `POST /api/v1/admin/applicants/approve`. `GET /api/v1/tenant/current` informa el dominio docente.
- Clave de la página `/platform` en SSM (`Platform__AdminKey`, Terraform).

### Cambiado

- **El registro público ya no habilita la cuenta**: queda pendiente y el login responde 403 "Tu cuenta está pendiente de aprobación" hasta que un Admin la da de alta. Los usuarios existentes, los creados por un Admin, la plataforma, DevTools y los seeds quedan aprobados.
- Gestión de Usuarios muestra solo cuentas dadas de alta.
- El menú "Registros" pasa a llamarse "Registros Académicos".
- Migraciones: `AddUserRegistrationApproval` (base de cada institución) y `AddTenantTeacherEmailDomain` (base de control).

## [1.5.0] - 2026-11-15

### Agregado

- **Versión visible**: la barra lateral muestra al pie la versión publicada ("Versión 1.5.0"). Sale de `frontend-angular/src/environments/version.ts`, que `tools/version.sh` actualiza y verifica junto con el resto.
- **Acceso a Google Classroom** en el menú de usuario: abre `https://classroom.google.com/` en una pestaña nueva.

### Cambiado

- El menú de usuario ya no muestra "Mis Mensajes"; su lugar lo ocupa "Classroom". La ruta `/app/messages` sigue existiendo.

## [1.4.1] - 2026-11-15

### Corregido

- **El correo de recuperación no se enviaba en producción.** El rol de la instancia solo podía usar la identidad SES del dominio remitente; con la cuenta en sandbox, SES también exige permiso sobre la identidad del destinatario y respondía *AccessDenied* (el backend lo registraba y la pantalla igual decía "revisa tu correo"). El permiso `SendEmail` ahora cubre `identity/*` de la cuenta y región (Terraform, unidad `app`).
- **El encabezado seguía consultando `/api/v1/devtools/time` cada 30 segundos** (403 en producción). El arreglo de 1.4.0 comprobaba si el error era un `HttpErrorResponse`, pero el `ErrorInterceptor` lo reemplaza por un `ApiError`; ahora se lee el `status` del error. Con la función deshabilitada se hace una sola consulta por carga de página. Test nuevo: `time-travel.service.spec.ts`, con el interceptor real.

## [1.4.0] - 2026-11-15

Recuperación de contraseña por correo (Amazon SES).

### Agregado

- **Correo de recuperación**: el pedido de "¿Olvidaste tu contraseña?" envía un enlace a `/auth/reset-password?token=...` desde `no-reply@itsc.dryvn.org` por Amazon SES, con el rol de la instancia y sin claves. En desarrollo el correo se escribe en el log (`Email:Provider=Log`).
- El enlace usa el dominio de la institución que hizo el pedido (multitenant).
- Límite de 5 pedidos de recuperación por IP cada 15 minutos.

### Cambiado

- **El token de recuperación ya no vuelve en la respuesta** de `POST /api/v1/users/forgot-password`: la respuesta es siempre la misma, exista o no la cuenta. Si se informa el DNI, tiene que coincidir.
- El token queda atado a la institución y a la contraseña actual: no sirve en otra institución ni se puede usar dos veces. Al cambiar la contraseña se cierran las sesiones abiertas.
- Frontend: se eliminó el paso del código de 6 dígitos (el código válido era siempre `000000`); el enlace del correo abre directamente el formulario de contraseña nueva.

### Corregido

- Un token de recuperación inválido o vencido respondía 500; ahora responde 400.
- Al abrir el perfil, Admin y Profesor veían "El usuario actual no es un alumno.": la pantalla pedía la ficha de estudiante a cualquier rol. Ahora solo la pide para Alumno.
- El encabezado del panel consultaba `/api/v1/devtools/time` cada 30 segundos y en producción mostraba "No tenés permisos para realizar esta acción". Ahora, si la función está deshabilitada, consulta una sola vez y sin alerta.

## [1.3.0] - 2026-11-15

Multitenant: una base de datos por institución sobre la misma plataforma.

### Agregado

- **Base de control** (`academiadigital_control`) con el catálogo de instituciones y sus dominios. El ITSC queda como institución por defecto sobre su base actual, sin mover datos.
- **Resolución de la institución por dominio**: el frontend envía el dominio del navegador en `X-Tenant` (o se toma de `Origin`); el backend elige la base de esa institución en cada request. Un dominio no registrado responde 404.
- **Alta de instituciones** desde la pantalla `/platform` y la API `api/v1/platform/tenants`, protegidas con la clave de operador `X-Platform-Key` (`Platform__AdminKey`). Sin clave configurada la API de plataforma queda deshabilitada. El alta crea y migra la base de la institución y su usuario administrador.
- **CORS dinámico** para los dominios registrados, sin reiniciar.
- Archivos y certificados separados por institución.

### Cambiado

- Las sesiones y los usuarios viven en la base de cada institución: un usuario o un token de una institución no sirven en otra.
- El microservicio Finance solo atiende a la institución por defecto.

### Corregido

- `itsc.dryvn.org` registrado como dominio de la institución por defecto: sin él, en producción todas las llamadas a la API respondían 404.
- El identificador `finance` queda reservado: su base (`academiadigital_finance`) es la del microservicio Finance.

## [1.2.0] - 2026-11-14

Archivos en S3: la plataforma puede correr en varias instancias.

### Agregado

- **`S3FileStorage`** en el monolito y en Finance: acuerdos, certificados y recibos se guardan en el bucket de archivos (`Storage:Provider=S3`), con el rol de la instancia y sin claves. Cada servicio usa su prefijo (`backend/`, `finance/`). Las claves lógicas guardadas en la base no cambian.
- **IMDSv2 con hop limit 2** por defecto para las instancias nuevas de la cuenta (`infra/live/shared/ec2-metadata-defaults`): sin esto, los contenedores de Beanstalk no obtienen las credenciales del rol.

### Cambiado

- El adaptador de almacenamiento se elige por configuración. Por defecto sigue siendo el disco local (desarrollo y docker-compose); Beanstalk usa S3.
- Validación de claves de almacenamiento: se rechazan segmentos vacíos, `.` y `..`.

### Corregido

- Con más de una instancia, un PDF generado en una no existía en la otra y se perdía al reemplazarla.

## [1.1.0] - 2026-11-14

Observabilidad autogestionada: quién entra, desde dónde, qué errores hay y cómo están las instancias. En producción se guardan **solo errores y accesos**.

### Agregado

- **Logs estructurados en JSON** en backend y Finance fuera de Development.
- **Solo errores en producción:** `Observability__MinimumLevel=Error` sube el nivel mínimo de todas las categorías sin recompilar; Info y Warning no se escriben. Alloy aplica el mismo filtro como segunda barrera y descarta el access log de nginx.
- **Auditoría de accesos** (`AuditLog`, categoría propia que queda activa aunque el resto esté en Error): `login_success` (usuario, rol, IP, user agent), `login_failed` (estado e IP, sin email) y `logout`. Se desactiva con `Observability__AuditEnabled=false`.
- **Línea por request** (`Event = http_request`: método, ruta sin query string, estado, duración, IP, usuario, rol); en producción solo se guardan las de estado 5xx.
- **Errores del frontend:** `ErrorHandler` global de Angular que reporta los errores de JavaScript a `POST /api/v1/client-errors` (anónimo, limitado por IP, sin query string ni datos de formularios).
- **Monitoreo en AWS** (`infra/modules/monitoring`): EC2 arm64 con Grafana, Loki (logs en S3, retención de 90 días) y Prometheus (métricas, 30 días), sin puertos abiertos a internet; Grafana se abre con Session Manager. Dashboard "Accesos, errores e instancias" versionado en el repositorio.
- **Agente Grafana Alloy** en cada instancia de Beanstalk: logs filtrados a Loki y métricas del host a Prometheus.

### Cambiado

- **IP real del cliente detrás del ALB:** con `ReverseProxy__TrustForwardedHeaders=true` (solo en Beanstalk) el backend toma la IP de `X-Forwarded-For`. Corrige también el rate limiter de admisiones, que veía la IP del proxy para todos.
- **`ExceptionMiddleware`** registra como Error solo las excepciones que terminan en 5xx; las de negocio esperadas (credenciales inválidas, conflictos, no encontrado) pasan a Information y no llenan el log de errores.
- **Swagger y Scalar apagados en producción** (`Swagger__Enabled=false`) en el monolito y en Finance; siguen activos en desarrollo y en el docker-compose local.
- Logs de Entity Framework en `Warning` por defecto: las consultas SQL dejan de escribirse en el log.

## [1.0.0] - 2026-11-14

Primera versión de producción de la plataforma.

### Agregado

- **Frontend (Angular 21):** módulos de autenticación (inicio de sesión, registro, recuperación de contraseña), dashboard por rol (alumno, profesor, administración), carreras y materias, inscripciones a cursado y a mesas de examen, calificaciones, asistencia, historial académico, certificados, calendario, mensajes, perfil, docentes y administración.
- **Acceso rápido a Google Classroom** desde el dashboard para los tres roles.
- **Backend (.NET 8):** API del monolito para usuarios, admisiones, carreras, planes de estudio, correlatividades, comisiones y divisiones, inscripciones y rematriculaciones, libretas de calificaciones, mesas de examen, asistencia, calendario, certificados y docentes, sobre PostgreSQL.
- **Microservicio Finance (.NET 8):** servicio separado con su propia base de datos, integrado con el monolito por HTTP.
- **Infraestructura en AWS con Terragrunt y Terraform:** VPC propia, RDS PostgreSQL, Elastic Beanstalk con Docker sobre arm64, certificado HTTPS de ACM, correo con SES, archivos en S3, horario de encendido y apagado, presupuesto con alertas.
- **CI/CD:** CodePipeline y CodeBuild arm64 dentro de la cuenta de AWS, conectados a GitHub sin credenciales guardadas.
- **Versionado semántico** con una versión única para frontend, backend y Finance (`VERSION`, `tools/version.sh`).
- Plan de pruebas de sistema con 75 casos (`PRUEBAS_SISTEMA.md`).

[Sin publicar]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/sergioortiz17/AcademiaDigital/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/sergioortiz17/AcademiaDigital/releases/tag/v1.0.0
