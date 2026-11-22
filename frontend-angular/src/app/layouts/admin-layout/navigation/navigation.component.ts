import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
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
 * Menú de Administración: los mismos ítems y nombres de siempre, agrupados en desplegables por tema
 * (1.15.1). Las URL y los permisos son los de cada pantalla.
 */
const MENU_ADMIN: MenuItem[] = [
  { id: 'dashboard', title: 'Inicio', url: '/app/dashboard/default', icon: 'home' },
  { id: 'courses', title: 'Plan de estudios', url: '/app/courses', icon: 'book' },
  { id: 'calendar', title: 'Calendario', url: '/app/calendar', icon: 'calendar_month' },
  { id: 'admission', title: 'Admisión', icon: 'how_to_reg', children: [
    { id: 'applicants', permission: 'ManageStudents', title: 'Aspirantes', url: '/app/admin/applicants' },
    { id: 'student-documents', permission: 'ManageStudents', title: 'Requisitos de ingreso', url: '/app/admin/student-documents' },
    { id: 'admin-enrollments', permission: 'ManageEnrollments', title: 'Gestión de Inscripciones', url: '/app/admin/enrollments' }
  ] },
  { id: 'students-group', title: 'Alumnos', icon: 'groups', children: [
    { id: 'students', permission: 'ManageGrades', title: 'Alumnos', url: '/app/admin/students-condition' },
    { id: 'student-commission', permission: 'ManageStudents', title: 'Asignar división a alumnos', url: '/app/admin/student-commission-assignment' },
    { id: 'transcripts', permission: 'ManageStudents', title: 'Analíticos', url: '/app/admin/transcripts' },
    { id: 'bulk-actions', permission: 'ManageStudents', title: 'Acciones masivas', url: '/app/admin/bulk-actions' }
  ] },
  { id: 'teachers', title: 'Docentes', icon: 'co_present', children: [
    { id: 'teacher-list', permission: 'ManageTeachers', title: 'Listado de docentes', url: '/app/admin/teachers' },
    { id: 'teaching-positions', permission: 'ManageCareers', title: 'Comisiones', url: '/app/admin/teaching-positions' },
    { id: 'commissions', permission: 'ManageCareers', title: 'Divisiones', url: '/app/admin/commissions' },
    { id: 'teacher-courses', permission: 'ManageTeachers', title: 'Asignar materia', url: '/app/admin/teacher-courses' },
    { id: 'my-assignments', permission: 'ManageTeachers', title: 'Asignaciones', url: '/app/teachers' }
  ] },
  { id: 'registros', title: 'Registros Académicos', icon: 'assignment_ind', children: [
    { id: 'attendance', permission: 'ManageAttendance', title: 'Asistencias', url: '/app/admin/attendance' },
    { id: 'gradebooks-shared', permission: 'ManageGrades', title: 'Calificaciones', url: '/app/grades' }
  ] },
  { id: 'certificates', permission: 'ManageCertificates', title: 'Certificados', icon: 'military_tech', children: [
    { id: 'certificate-requests', permission: 'ManageCertificates', title: 'Peticiones', url: '/app/certificates' }
  ] },
  { id: 'admin-exam-form', title: 'Mesa de examen', icon: 'event_note', children: [
    { id: 'exam-form', permission: 'ManageExams', title: 'Formulario de inscripción', url: '/app/admin/exam-form' },
    { id: 'exam-schedule', permission: 'ManageExams', title: 'Armar mesas automáticas', url: '/app/admin/exam-schedule' }
  ] },
  { id: 'reports', permission: 'ViewReports', title: 'Reportes y Excel', url: '/app/admin/reports', icon: 'table_view' },
  { id: 'users', permission: 'ManageUsers', title: 'Gestión de Usuarios', url: '/app/admin/users', icon: 'manage_accounts' },
  { id: 'careers-admin', permission: 'ManageCareers', title: 'Gestión de Carreras', url: '/app/admin/careers', icon: 'school' },
  { id: 'schools', permission: 'ManageCareers', title: 'Agregar escuelas', url: '/app/admin/schools', icon: 'location_city' },
  { id: 'approval-rules', permission: 'ManageCareers', title: 'Reglas de aprobación', url: '/app/admin/approval-rules', icon: 'rule' }
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
  private isManagementMenu = false;

  constructor(private readonly store: Store, private readonly router: Router) {}

  /** El desplegable de la página actual se muestra abierto, para que se vea dónde está. */
  isGroupActive(item: MenuItem): boolean {
    if (!this.isManagementMenu) return false; // los menús de alumno y docente quedan como estaban
    const url = this.router.url.split('?')[0];
    return !!item.children?.some(child => !!child.url && (url === child.url || url.startsWith(child.url + '/')));
  }

  ngOnInit(): void {
    this.store.select(selectUserRole).subscribe(role => {
      this.isManagementMenu = role === UserRole.Admin || isStaff(role as UserRole);
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
