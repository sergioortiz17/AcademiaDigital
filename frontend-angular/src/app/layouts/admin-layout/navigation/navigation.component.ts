import { Component, Output, EventEmitter } from '@angular/core';

export interface MenuItem {
  id: string;
  title: string;
  url: string;
  icon: string;
}

@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss'],
  standalone: false
})
export class NavigationComponent {
  @Output() menuItemClick = new EventEmitter<void>();

  menuItems: MenuItem[] = [
    { id: 'dashboard',    title: 'Inicio',        url: '/app/dashboard/default', icon: 'home' },
    { id: 'courses',      title: 'Materias',       url: '/app/courses',           icon: 'menu_book' },
    { id: 'enrollments',  title: 'Inscripciones',  url: '/app/enrollments',       icon: 'assignment' },
    { id: 'calendar',     title: 'Calendario',     url: '/app/calendar',          icon: 'calendar_today' },
    { id: 'teachers',     title: 'Profesores',     url: '/app/teachers',          icon: 'group' },
    { id: 'certificates', title: 'Certificados',   url: '/app/certificates',      icon: 'military_tech' },
    { id: 'grades',       title: 'Notas',          url: '/app/grades',            icon: 'grade' },
    { id: 'messages',     title: 'Mensajes',       url: '/app/messages',          icon: 'message' },
    { id: 'profile',      title: 'Perfil',         url: '/app/profile',           icon: 'person' },
  ];

  onItemClick(): void {
    this.menuItemClick.emit();
  }
}
