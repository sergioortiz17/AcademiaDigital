import { Component, Input } from '@angular/core';
import { Store } from '@ngrx/store';
import { collapseMenu } from '../../../store/config/config.actions';

export interface MenuItem {
  id: string;
  title: string;
  type: 'item' | 'group' | 'collapse';
  url?: string;
  icon?: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss'],
  standalone: false
})
export class NavigationComponent {
  @Input() collapseMenu = false;
  @Input() isMobile = false;

  menuItems: MenuItem[] = [
    {
      id: 'navigation',
      title: 'menu.navigation',
      type: 'group',
      icon: 'icon-navigation',
      children: [
        { id: 'dashboard', title: 'Inicio', type: 'item', url: '/app/dashboard/default', icon: 'feather icon-home' },
        { id: 'courses', title: 'Materias', type: 'item', url: '/app/courses', icon: 'feather icon-book' },
        { id: 'enrollments', title: 'Inscripciones', type: 'item', url: '/app/enrollments', icon: 'feather icon-file-text' },
        { id: 'calendar', title: 'Calendario', type: 'item', url: '/app/calendar', icon: 'feather icon-calendar' },
        { id: 'teachers', title: 'Profesores', type: 'item', url: '/app/teachers', icon: 'feather icon-users' },
        { id: 'certificates', title: 'Certificados', type: 'item', url: '/app/certificates', icon: 'feather icon-award' },
        { id: 'grades', title: 'Notas', type: 'item', url: '/app/grades', icon: 'feather icon-star' },
        { id: 'messages', title: 'Mensajes', type: 'item', url: '/app/messages', icon: 'feather icon-message-circle' },
        { id: 'profile', title: 'Perfil', type: 'item', url: '/app/profile', icon: 'feather icon-user' }
      ]
    }
  ];

  constructor(private readonly store: Store) {}

  get navClass(): string[] {
    const classes = ['pcoded-navbar', 'menu-dark', 'navbar-default', 'brand-default'];
    if (this.collapseMenu) {
      classes.push('navbar-collapsed');
    }
    if (this.isMobile) {
      classes.push('mob-open');
    }
    return classes;
  }

  toggleCollapse(): void {
    this.store.dispatch(collapseMenu());
  }
}
