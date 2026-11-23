import { IStepOption } from 'ngx-ui-tour-md-menu';

/**
 * Tour de Administración. Sigue el orden del menú: al entrar a cada pantalla, el desplegable que la
 * contiene se abre solo, así se ve en qué grupo está cada opción.
 */
export const ADMIN_TOUR: IStepOption[] = [
  {
    anchorId: 'dashboard.welcome',
    route: '/app/dashboard/default',
    title: 'Le damos la bienvenida',
    content: 'Este recorrido le muestra para qué sirve cada parte de la administración del sistema. Use '
      + '<b>Siguiente</b> y <b>Anterior</b>, o las flechas del teclado. Puede cerrarlo con la X o con Esc.'
  },
  {
    anchorId: 'nav.menu',
    title: 'Menú de administración',
    content: 'Las opciones están agrupadas en desplegables: Admisión, Alumnos, Docentes, Registros académicos, '
      + 'Certificados y Mesa de examen. Abajo están Reportes, Usuarios, Carreras, Escuelas y Reglas de aprobación.'
  },
  {
    anchorId: 'dashboard.quick-access',
    route: '/app/dashboard/default',
    title: 'Accesos rápidos',
    content: 'Atajos a las tareas más frecuentes de administración.'
  },
  {
    anchorId: 'admin.applicants',
    route: '/app/admin/applicants',
    title: 'Admisión · Aspirantes',
    content: 'Las personas que se registraron y esperan el alta. Desde aquí las aprueba para que puedan iniciar sesión.'
  },
  {
    anchorId: 'admin.student-documents',
    route: '/app/admin/student-documents',
    title: 'Admisión · Requisitos de ingreso',
    content: 'Los documentos que suben los alumnos (analítico y otros). Los descarga, los revisa y los aprueba o '
      + 'rechaza indicando el motivo.'
  },
  {
    anchorId: 'admin.enrollments',
    route: '/app/admin/enrollments',
    title: 'Admisión · Gestión de Inscripciones',
    content: 'Aquí abre y cierra los períodos de inscripción a materias de cada carrera, con los cupos por turno, '
      + 'y ve quiénes se inscribieron.'
  },
  {
    anchorId: 'students.page',
    route: '/app/admin/students-condition',
    title: 'Alumnos',
    content: 'La condición de los alumnos en cada materia (regular, promocional o libre).'
  },
  {
    anchorId: 'admin.student-courses',
    route: '/app/admin/student-commission-assignment',
    title: 'Alumnos · Asignar curso',
    content: 'Asigna a varios alumnos de una vez al curso (año y turno) en el que van a cursar.'
  },
  {
    anchorId: 'admin.transcripts',
    route: '/app/admin/transcripts',
    title: 'Alumnos · Analíticos',
    content: 'Arma el certificado analítico de un alumno con todas sus materias, libro y folio, para imprimirlo o '
      + 'guardarlo como PDF.'
  },
  {
    anchorId: 'admin.bulk-actions',
    route: '/app/admin/bulk-actions',
    title: 'Alumnos · Acciones masivas',
    content: 'Para tareas de a muchos: marcar egresados con su fecha y resolución, o inscribir un curso completo '
      + 'en las materias de su año.'
  },
  {
    anchorId: 'admin.teachers',
    route: '/app/admin/teachers',
    title: 'Docentes · Listado de docentes',
    content: 'Alta, edición y legajo de los docentes, con sus materias asignadas.'
  },
  {
    anchorId: 'admin.courses',
    route: '/app/admin/commissions',
    title: 'Docentes · Cursos',
    content: 'Los cursos son los grupos de alumnos de cada año, turno y escuela (por ejemplo, "1° Tarde"). '
      + 'Desde aquí también asigna alumnos o inscribe un curso en sus materias.'
  },
  {
    anchorId: 'admin.commissions',
    route: '/app/admin/teaching-positions',
    title: 'Docentes · Comisiones',
    content: 'Una comisión es una materia dictada a un curso, con su docente. Muestra los cupos libres y, al '
      + 'desplegarla, los alumnos inscriptos.'
  },
  {
    anchorId: 'admin.teacher-courses',
    route: '/app/admin/teacher-courses',
    title: 'Docentes · Asignar materia',
    content: 'Arrastre cada docente hasta la materia, elija el curso y el cargo, y guarde. Al pasar el mouse por un '
      + 'docente ve las materias que ya tiene.'
  },
  {
    anchorId: 'admin.attendance',
    route: '/app/admin/attendance',
    title: 'Registros académicos · Asistencias',
    content: 'La asistencia de todas las materias, con filtros por carrera, año y turno.'
  },
  {
    anchorId: 'grades.staff-filters',
    route: '/app/grades',
    title: 'Registros académicos · Calificaciones',
    content: 'Las planillas de notas de cada materia. Secretaría las revisa, las aprueba, las publica y las cierra.'
  },
  {
    anchorId: 'certificates.staff',
    route: '/app/certificates',
    title: 'Certificados · Peticiones',
    content: 'Las solicitudes de certificados de alumnos y docentes. Filtre por estado y apruebe o rechace cada una.'
  },
  {
    anchorId: 'admin.exam-schedule',
    route: '/app/admin/exam-schedule',
    title: 'Mesa de examen · Armar mesas automáticas',
    content: 'El sistema propone las fechas de las mesas respetando feriados y correlatividades, y sugiere el '
      + 'tribunal. Después las ajusta y las confirma.'
  },
  {
    anchorId: 'admin.reports',
    route: '/app/admin/reports',
    title: 'Reportes y Excel',
    content: 'Listados de alumnos, egresados, libres, bajas y aspirantes para descargar en Excel.'
  },
  {
    anchorId: 'admin.users',
    route: '/app/admin/users',
    title: 'Gestión de Usuarios',
    content: 'Todas las cuentas del sistema. Desde aquí cambia el rol de un usuario o da de baja una cuenta.'
  },
  {
    anchorId: 'admin.careers',
    route: '/app/admin/careers',
    title: 'Gestión de Carreras',
    content: 'Las carreras, sus planes de estudio (se importan desde CSV y se comparan entre versiones) y las escuelas.'
  },
  {
    anchorId: 'admin.schools',
    route: '/app/admin/schools',
    title: 'Agregar escuelas',
    content: 'Las escuelas (sedes) del instituto. Cada curso se asigna a una escuela.'
  },
  {
    anchorId: 'admin.approval-rules',
    route: '/app/admin/approval-rules',
    title: 'Reglas de aprobación',
    content: 'Por materia, si permite promoción y cuáles son las notas mínimas. Con estas reglas se calcula la '
      + 'condición de cada alumno.'
  },
  {
    anchorId: 'header.tour-button',
    title: '¡Listo!',
    content: 'Ya conoce las secciones de administración. Puede volver a ver este recorrido cuando quiera con este botón.'
  }
];
