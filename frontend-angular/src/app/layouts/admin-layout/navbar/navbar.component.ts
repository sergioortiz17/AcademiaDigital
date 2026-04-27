import { Component } from '@angular/core';
import { Store } from '@ngrx/store';
import { collapseMenu } from '../../../store/config/config.actions';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  standalone: false
})
export class NavbarComponent {
  constructor(private readonly store: Store) {}

  toggleMenu(): void {
    this.store.dispatch(collapseMenu());
  }
}
