import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { Store } from '@ngrx/store';
import { selectUserRole } from '../../../store/account/account.selectors';
import { UserRole } from '../../../store/account/account.actions';

export interface MenuItem {
  id: string;
  title: string;
  url: string;
  icon: string;
}

const MENU_ALUMNO: MenuItem[] = [
  { id: 'dashboard',    title: 'Inicio',         url: '/app/dashboard/default', icon: 'home' },
  { id: 'courses',      title: 'Carreras',        url: '/app/courses',           icon: 'menu_book' },
  { id: 'enrollments',  title: 'Inscripciones',   url: '/app/enrollments',       icon: 'assignment' },
  { id: 'certificates', title: 'Certificados',    url: '/app/certificates',      icon: 'military_tech' },
  { id: 'calendar',     title: 'Calendario',      url: '/app/calendar',          icon: 'calendar_today' },
];

const MENU_PROFESOR: MenuItem[] = [
  { id: 'dashboard',   title: 'Inicio',          url: '/app/dashboard/default', icon: 'home' },
  { id: 'grades',      title: 'Cargar Notas',    url: '/app/grades',            icon: 'grade' },
  { id: 'courses',     title: 'Carreras',         url: '/app/courses',           icon: 'menu_book' },
  { id: 'enrollments', title: 'Inscripciones',    url: '/app/enrollments',       icon: 'assignment' },
  { id: 'calendar',    title: 'Calendario',       url: '/app/calendar',          icon: 'calendar_today' },
];

const MENU_ADMIN: MenuItem[] = [
  { id: 'dashboard',   title: 'Inicio',            url: '/app/dashboard/default', icon: 'home' },
  { id: 'users',       title: 'Gestión de Usuarios', url: '/app/admin/users',     icon: 'manage_accounts' },
  { id: 'courses',     title: 'Carreras',           url: '/app/courses',           icon: 'menu_book' },
  { id: 'enrollments', title: 'Inscripciones',      url: '/app/enrollments',       icon: 'assignment' },
  { id: 'teachers',    title: 'Profesores',         url: '/app/teachers',          icon: 'group' },
  { id: 'certificates', title: 'Certificados',      url: '/app/certificates',      icon: 'military_tech' },
  { id: 'calendar',    title: 'Calendario',         url: '/app/calendar',          icon: 'calendar_today' },
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
