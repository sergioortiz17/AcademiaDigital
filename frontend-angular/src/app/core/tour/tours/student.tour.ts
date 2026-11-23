import { IStepOption } from 'ngx-ui-tour-md-menu';

/**
 * Tour del alumno. Cada paso apunta a un elemento marcado con tourAnchor="…" en su pantalla; si el
 * paso tiene route, el tour navega y espera a que el elemento aparezca. Los textos tratan de usted,
 * como el resto de la aplicación.
 */
export const STUDENT_TOUR: IStepOption[] = [
  {
    anchorId: 'student.welcome',
    route: '/app/dashboard/default',
    title: 'Le damos la bienvenida',
    content: 'Este recorrido le muestra para qué sirve cada parte del sistema. Use <b>Siguiente</b> y <b>Anterior</b>, '
      + 'o las flechas del teclado. Puede cerrarlo en cualquier momento con la X o con la tecla Esc.'
  },
  {
    anchorId: 'nav.menu',
    title: 'Menú principal',
    content: 'Desde este menú entra a todas las secciones: plan de estudios, inscripciones, calificaciones, '
      + 'asistencias, certificados y más. La sección en la que está queda resaltada.'
  },
  {
    anchorId: 'student.quick-access',
    route: '/app/dashboard/default',
    title: 'Accesos rápidos',
    content: 'Atajos a lo que más se usa: pedir certificados, inscribirse, ver el plan de estudios, el calendario '
      + 'y Google Classroom.'
  },
  {
    anchorId: 'student.plan',
    route: '/app/courses',
    title: 'Plan de estudios',
    content: 'Las materias de su carrera, año por año, y las correlatividades: para cursar una materia tiene que '
      + 'tener aprobada o regular la anterior.'
  },
  {
    anchorId: 'student.admission-docs',
    route: '/app/admission-documents',
    title: 'Requisitos de ingreso',
    content: 'Aquí sube el analítico y los demás documentos que pide el instituto (PDF, JPG o PNG). '
      + 'Administración los revisa y usted ve si están en revisión, aprobados o rechazados, con el motivo.'
  },
  {
    anchorId: 'student.pre-enrollment',
    route: '/app/admission-documents',
    title: 'Prematrícula',
    content: 'Si es ingresante, primero tiene que hacer la prematrícula en el sistema del Ministerio. '
      + 'Sin ese paso el instituto no puede matricularlo.'
  },
  {
    anchorId: 'student.enrollment-form',
    route: '/app/enrollments',
    title: 'Inscripción a materias',
    content: 'Cuando hay un período de inscripción abierto, aquí elige el turno y las materias que va a cursar. '
      + 'Las que todavía no puede cursar aparecen bloqueadas, con el motivo.'
  },
  {
    anchorId: 'student.my-enrollments',
    route: '/app/enrollments',
    title: 'Mis inscripciones',
    content: 'Su historial de inscripciones, con las materias de cada período. Desde aquí puede descargar la '
      + 'constancia o dar de baja una inscripción.'
  },
  {
    anchorId: 'student.exam',
    route: '/app/enrollments/exam',
    title: 'Mesas de examen',
    content: 'Para inscribirse a rendir finales: elige las materias y la fecha de cada mesa.'
  },
  {
    anchorId: 'student.grades',
    route: '/app/grades',
    title: 'Calificaciones',
    content: 'Sus notas de cada materia, con la condición (regular, promocional o libre) y el promedio.'
  },
  {
    anchorId: 'student.attendance',
    route: '/app/attendance/me',
    title: 'Asistencias',
    content: 'Su porcentaje de asistencia por materia. Las materias en riesgo aparecen primero, para que pueda '
      + 'revisarlas a tiempo.'
  },
  {
    anchorId: 'student.history',
    route: '/app/academic-history',
    title: 'Historial académico',
    content: 'Todas las materias de su plan con su estado: aprobadas, en curso o pendientes.'
  },
  {
    anchorId: 'student.certificates',
    route: '/app/certificates',
    title: 'Certificados',
    content: 'Aquí pide certificados, por ejemplo el de alumno regular. En "Ver mis solicitudes" sigue su estado '
      + 'y descarga los que ya están aprobados.'
  },
  {
    anchorId: 'student.calendar',
    route: '/app/calendar',
    title: 'Calendario',
    content: 'Feriados, fechas de mesas de examen y eventos del instituto. Toque un día para ver el detalle.'
  },
  {
    anchorId: 'header.tour-button',
    title: '¡Listo!',
    content: 'Ya conoce las secciones principales. Puede volver a ver este recorrido cuando quiera con este botón.'
  }
];
