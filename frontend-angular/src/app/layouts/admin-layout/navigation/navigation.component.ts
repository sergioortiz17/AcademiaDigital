import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { Store } from '@ngrx/store';
import { selectUserRole } from '../../../store/account/account.selectors';
import { UserRole } from '../../../store/account/account.actions';

export interface MenuItem {
  id: string;
  title: string;
  url?: string;
  icon?: string;
  children?: MenuItem[];
}

const MENU_ALUMNO: MenuItem[] = [
  { id: 'dashboard',    title: 'Inicio',         url: '/app/dashboard/default', icon: 'home' },
  { id: 'courses',      title: 'Carreras',        url: '/app/courses',           icon: 'book' },
  { id: 'enrollments',  title: 'Inscripciones',   url: '/app/enrollments',       icon: 'assignment' },
  { id: 'calendar',     title: 'Calendario',      url: '/app/calendar',          icon: 'calendar_month' },
  { id: 'certificates', title: 'Certificados',    url: '/app/certificates',      icon: 'military_tech' },
  { id: 'grades',       title: 'Calificaciones',  url: '/app/grades',            icon: 'grade' },
];

const MENU_PROFESOR: MenuItem[] = [
  { id: 'dashboard',   title: 'Inicio',          url: '/app/dashboard/default', icon: 'home' },
  { id: 'grades',      title: 'Cargar Calificaciones', url: '/app/grades',      icon: 'grade' },
  { id: 'attendance',  title: 'Cargar Asistencia', url: '/app/attendance',      icon: 'fact_check' },
  { id: 'courses',     title: 'Carreras',         url: '/app/courses',           icon: 'book' },
  { id: 'calendar',    title: 'Calendario',       url: '/app/calendar',          icon: 'calendar_month' },
  { id: 'enrollments', title: 'Inscripciones',    url: '/app/enrollments',       icon: 'assignment' },
  { id: 'my-assignments', title: 'Mis asignaciones', url: '/app/teachers',       icon: 'assignment_ind' },
  //cambiar por Registro
];

const MENU_ADMIN: MenuItem[] = [
  { id: 'dashboard',   title: 'Inicio',            url: '/app/dashboard/default', icon: 'home' },
  { id: 'courses',     title: 'Carreras',           url: '/app/courses',           icon: 'book' },
  { id: 'enrollments', title: 'Inscripciones',      url: '/app/enrollments',       icon: 'assignment' },
  { id: 'calendar',    title: 'Calendario',         url: '/app/calendar',          icon: 'calendar_month' },
  { id: 'teachers',    title: 'Docentes',           icon: 'group',
    children: [
      { id: 'teacher-list',       title: '• Legajos', url: '/app/admin/teachers',           icon: '' },
      { id: 'teaching-positions', title: '• Cargos',  url: '/app/admin/teaching-positions',  icon: '' }
    ]
  },
  { id: 'registros',   title: 'Registros',          icon: 'assignment_ind',
    children: [
      { id: 'attendance', title: '• Asistencias', url: '/app/admin/attendance', icon: '' },
      { id: 'gradebooks', title: '• Calificaciones', url: '/app/admin/gradebooks', icon: '' }
    ]
  },
  { id: 'certificates', title: 'Certificados',      url: '/app/certificates',      icon: 'fact_check',
    children: [
      { id: 'certificate-requests', title: '• Peticiones',url: '/app/certificates',icon: '' }
    ]
  },
  { id: 'admin-enrollments', title: 'Gestión de Inscripciones', url: '/app/admin/enrollments', icon: 'how_to_reg' },
  { id: 'users',       title: 'Gestión de Usuarios', url: '/app/admin/users',     icon: 'group' },
];

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
      else if (role === UserRole.Profesor) this.menuItems = MENU_PROFESOR;
      else this.menuItems = MENU_ALUMNO;
    });
  }

  onItemClick(): void {
    this.menuItemClick.emit();
  }
}
