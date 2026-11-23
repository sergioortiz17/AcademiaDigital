import { IStepOption } from 'ngx-ui-tour-md-menu';

/** Tour del docente. Mismo formato que el del alumno (ver student.tour.ts). */
export const TEACHER_TOUR: IStepOption[] = [
  {
    anchorId: 'dashboard.welcome',
    route: '/app/dashboard/default',
    title: 'Le damos la bienvenida',
    content: 'Este recorrido le muestra para qué sirve cada parte del sistema como docente. Use <b>Siguiente</b> y '
      + '<b>Anterior</b>, o las flechas del teclado. Puede cerrarlo en cualquier momento con la X o con Esc.'
  },
  {
    anchorId: 'nav.menu',
    title: 'Menú principal',
    content: 'Desde este menú entra a todo lo que usa en el día a día: calificaciones, asistencia, sus materias y '
      + 'sus alumnos. La sección en la que está queda resaltada.'
  },
  {
    anchorId: 'dashboard.quick-access',
    route: '/app/dashboard/default',
    title: 'Accesos rápidos',
    content: 'Atajos a lo que más se usa: cargar calificaciones, inscripciones, plan de estudios, calendario y '
      + 'Google Classroom.'
  },
  {
    anchorId: 'teachers.tabs',
    route: '/app/teachers',
    title: 'Mis asignaciones',
    content: 'Las materias y comisiones que tiene a cargo, con su curso, turno y cuatrimestre. En la otra pestaña ve '
      + 'las materias del plan.'
  },
  {
    anchorId: 'grades.staff-filters',
    route: '/app/grades',
    title: 'Cargar calificaciones',
    content: 'Elija carrera, año, curso y materia para abrir la planilla de notas. Puede cargar y corregir notas '
      + 'hasta que la planilla se cierre, y enviarla a Secretaría cuando quiera (también de forma parcial).'
  },
  {
    anchorId: 'attendance.toolbar',
    route: '/app/attendance',
    title: 'Cargar asistencia',
    content: 'La asistencia es una matriz: alumnos en filas y días en columnas. Con <b>Hoy</b> agrega la clase del día, '
      + 'un toque en cada celda marca Presente, Ausente o Tarde, y <b>Guardar</b> guarda todos los cambios juntos.'
  },
  {
    anchorId: 'students.page',
    route: '/app/admin/students-condition',
    title: 'Alumnos',
    content: 'Elija una de sus materias para ver la condición de cada alumno (regular, promocional o libre), '
      + 'calculada con las notas cargadas.'
  },
  {
    anchorId: 'courses.plan',
    route: '/app/courses',
    title: 'Plan de estudios',
    content: 'Las materias de la carrera, año por año, con sus correlatividades.'
  },
  {
    anchorId: 'certificates.request',
    route: '/app/certificates',
    title: 'Certificados',
    content: 'Aquí pide su constancia de actividad docente y sigue el estado de sus solicitudes en "Ver mis solicitudes".'
  },
  {
    anchorId: 'calendar.header',
    route: '/app/calendar',
    title: 'Calendario',
    content: 'Feriados, mesas de examen y eventos del instituto. También puede cargar eventos propios.'
  },
  {
    anchorId: 'header.tour-button',
    title: '¡Listo!',
    content: 'Ya conoce las secciones principales. Puede volver a ver este recorrido cuando quiera con este botón.'
  }
];
