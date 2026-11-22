import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { Store } from '@ngrx/store';
import { selectUserRole } from '../../../store/account/account.selectors';
import { UserRole } from '../../../store/account/account.actions';
import { hasPermission, isStaff, Permission } from '../../../core/auth/permissions';

export interface MenuItem {
  id: string;
  /** Área necesaria para ver el ítem (roles de gestión); sin área, lo ve cualquiera del menú. */
  permission?: Permission;
  title: string;
  url?: string;
  icon?: string;
  children?: MenuItem[];
  /** Sección con título fijo (no desplegable): sus hijos se muestran siempre, con ícono. */
  section?: boolean;
}

const MENU_ALUMNO: MenuItem[] = [
  { id: 'dashboard',    title: 'Inicio',         url: '/app/dashboard/default', icon: 'home' },
  { id: 'courses',      title: 'Plan de estudios', url: '/app/courses',          icon: 'book' },
  { id: 'admission-documents', title: 'Requisitos de ingreso', url: '/app/admission-documents', icon: 'upload_file' },
  { id: 'enrollments', title: 'Inscripciones', icon: 'assignment', children: [
    { id: 'course-enrollments', title: 'Insc. Materias', url: '/app/enrollments' },
    { id: 'exam-enrollments', title: 'Mesa de examen', url: '/app/enrollments/exam' }
  ] },
  { id: 'calendar',     title: 'Calendario',      url: '/app/calendar',          icon: 'calendar_month' },
  { id: 'certificates', title: 'Certificados',    url: '/app/certificates',      icon: 'military_tech' },
  { id: 'grades',       title: 'Calificaciones',  url: '/app/grades',            icon: 'grade' },
  { id: 'academic-history', title: 'Historial académico', url: '/app/academic-history', icon: 'history_edu' },
  { id: 'my-attendance', title: 'Asistencias',    url: '/app/attendance/me',     icon: 'fact_check' },
];

const MENU_PROFESOR: MenuItem[] = [
  { id: 'dashboard',   title: 'Inicio',          url: '/app/dashboard/default', icon: 'home' },
  { id: 'grades',      title: 'Cargar Calificaciones', url: '/app/grades',      icon: 'grade' },
  { id: 'attendance',  title: 'Cargar Asistencia', url: '/app/attendance',      icon: 'fact_check' },
  { id: 'courses',     title: 'Plan de estudios', url: '/app/courses',           icon: 'book' },
  { id: 'calendar',    title: 'Calendario',       url: '/app/calendar',          icon: 'calendar_month' },
  { id: 'certificates', title: 'Certificados',    url: '/app/certificates',      icon: 'military_tech' },
  { id: 'my-assignments', title: 'Mis asignaciones', url: '/app/teachers',       icon: 'assignment_ind' },
  { id: 'students',    title: 'Alumnos',          url: '/app/admin/students-condition', icon: 'groups' },
  //cambiar por Registro
];

/**
 * Menú de Administración, ordenado según el recorrido del alumno (ingresa → cursa → rinde → egresa),
 * con la configuración y las herramientas al final. Las secciones tienen título fijo. Las URL y los
 * permisos no cambiaron con la reorganización de la 1.15.0; cada ítem tiene un ícono distinto.
 */
const MENU_ADMIN: MenuItem[] = [
  { id: 'dashboard', title: 'Inicio', url: '/app/dashboard/default', icon: 'home' },
  { id: 'calendar', title: 'Calendario académico', url: '/app/calendar', icon: 'calendar_month' },
  { id: 'sec-admission', title: 'Admisión', section: true, children: [
    { id: 'applicants', permission: 'ManageStudents', title: 'Aspirantes', url: '/app/admin/applicants', icon: 'person_add' },
    { id: 'student-documents', permission: 'ManageStudents', title: 'Documentación de ingreso', url: '/app/admin/student-documents', icon: 'folder_shared' },
    { id: 'admin-enrollments', permission: 'ManageEnrollments', title: 'Períodos de inscripción', url: '/app/admin/enrollments', icon: 'how_to_reg' }
  ] },
  { id: 'sec-people', title: 'Personas', section: true, children: [
    { id: 'students', permission: 'ManageGrades', title: 'Alumnos', url: '/app/admin/students-condition', icon: 'groups' },
    { id: 'teacher-list', permission: 'ManageTeachers', title: 'Docentes', url: '/app/admin/teachers', icon: 'co_present' }
  ] },
  { id: 'sec-courses', title: 'Cursado', section: true, children: [
    { id: 'courses', title: 'Plan de estudios', url: '/app/courses', icon: 'menu_book' },
    { id: 'commissions', permission: 'ManageCareers', title: 'Divisiones', url: '/app/admin/commissions', icon: 'workspaces' },
    { id: 'teaching-positions', permission: 'ManageCareers', title: 'Comisiones', url: '/app/admin/teaching-positions', icon: 'class' },
    { id: 'teacher-courses', permission: 'ManageTeachers', title: 'Asignación docente', url: '/app/admin/teacher-courses', icon: 'assignment_ind' },
    { id: 'attendance', permission: 'ManageAttendance', title: 'Asistencias', url: '/app/admin/attendance', icon: 'fact_check' },
    { id: 'gradebooks-shared', permission: 'ManageGrades', title: 'Calificaciones', url: '/app/grades', icon: 'grade' }
  ] },
  { id: 'sec-exams', title: 'Exámenes', section: true, children: [
    { id: 'exam-schedule', permission: 'ManageExams', title: 'Mesas de examen', url: '/app/admin/exam-schedule', icon: 'event_note' },
    { id: 'exam-form', permission: 'ManageExams', title: 'Formulario de mesas', url: '/app/admin/exam-form', icon: 'edit_note' }
  ] },
  { id: 'sec-paperwork', title: 'Trámites y egreso', section: true, children: [
    { id: 'certificate-requests', permission: 'ManageCertificates', title: 'Solicitudes de certificados', url: '/app/certificates', icon: 'military_tech' },
    { id: 'transcripts', permission: 'ManageStudents', title: 'Analíticos', url: '/app/admin/transcripts', icon: 'history_edu' },
    { id: 'bulk-actions', permission: 'ManageStudents', title: 'Egresos', url: '/app/admin/bulk-actions', icon: 'workspace_premium' }
  ] },
  { id: 'sec-config', title: 'Configuración', section: true, children: [
    { id: 'careers-admin', permission: 'ManageCareers', title: 'Carreras', url: '/app/admin/careers', icon: 'school' },
    { id: 'approval-rules', permission: 'ManageCareers', title: 'Reglas de aprobación', url: '/app/admin/approval-rules', icon: 'rule' }
  ] },
  { id: 'sec-tools', title: 'Herramientas', section: true, children: [
    { id: 'reports', permission: 'ViewReports', title: 'Reportes', url: '/app/admin/reports', icon: 'table_view' },
    { id: 'users', permission: 'ManageUsers', title: 'Usuarios y permisos', url: '/app/admin/users', icon: 'manage_accounts' }
  ] }
];

/** Menú de Administración recortado a las áreas del rol (secretaría, coordinación, etc.). */
function menuFor(role: UserRole): MenuItem[] {
  const allowed = (item: MenuItem) => !item.permission || hasPermission(role, item.permission);
  return MENU_ADMIN
    .map(item => item.children ? { ...item, children: item.children.filter(allowed) } : item)
    .filter(item => allowed(item) && (!item.children || item.children.length > 0));
}

@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss'],
  standalone: false
})
export class NavigationComponent implements OnInit {
  @Output() menuItemClick = new EventEmitter<void>();

  menuItems: MenuItem[] = MENU_ALUMNO;

  constructor(private readonly store: Store) {}

  ngOnInit(): void {
    this.store.select(selectUserRole).subscribe(role => {
      if (role === UserRole.Admin) this.menuItems = MENU_ADMIN;
      else if (isStaff(role as UserRole)) this.menuItems = menuFor(role as UserRole);
      else if (role === UserRole.Profesor) this.menuItems = MENU_PROFESOR;
      else this.menuItems = MENU_ALUMNO;
    });
  }

  onItemClick(): void {
    this.menuItemClick.emit();
  }
}
