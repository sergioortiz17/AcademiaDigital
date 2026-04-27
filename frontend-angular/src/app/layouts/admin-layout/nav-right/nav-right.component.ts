import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { logout } from '../../../store/account/account.actions';
import { AuthService } from '../../../core/services/auth.service';
import { selectToken } from '../../../store/account/account.selectors';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-nav-right',
  templateUrl: './nav-right.component.html',
  styleUrls: ['./nav-right.component.scss'],
  standalone: false
})
export class NavRightComponent {
  notificationsOpen = false;
  profileOpen = false;
  langOpen = false;

  notifications = [
    { name: 'Jaz', message: 'Solicitud Constancia de Estudios', time: '30 min', type: 'new' },
    { name: 'Ramon', message: 'Solicitud de Inscripcion', time: '30 min', type: 'earlier' },
    { name: 'Claudia', message: 'Inscripcion de Catedra', time: '30 min', type: 'earlier' },
    { name: 'Prof.Pepito', message: 'Solicitud de Horarios', time: 'Ayer', type: 'earlier' }
  ];

  constructor(
    private readonly store: Store,
    private readonly router: Router,
    private readonly translate: TranslateService,
    private readonly authService: AuthService
  ) {}

  get currentLang(): string {
    return this.translate.currentLang ?? 'es';
  }

  changeLanguage(lang: string): void {
    this.translate.use(lang);
    localStorage.setItem('i18nextLng', lang);
    this.langOpen = false;
  }

  handleLogout(): void {
    this.store.select(selectToken).pipe(take(1)).subscribe((token) => {
      if (token) {
        this.authService.logoutApi(token).subscribe({
          error: () => {}
        });
      }
      this.store.dispatch(logout());
      this.authService.clearSession();
      this.router.navigate(['/auth/signin']);
    });
  }

  toggleNotifications(): void {
    this.notificationsOpen = !this.notificationsOpen;
    this.profileOpen = false;
    this.langOpen = false;
  }

  toggleProfile(): void {
    this.profileOpen = !this.profileOpen;
    this.notificationsOpen = false;
    this.langOpen = false;
  }

  toggleLang(): void {
    this.langOpen = !this.langOpen;
    this.notificationsOpen = false;
    this.profileOpen = false;
  }

  closeAll(): void {
    this.notificationsOpen = false;
    this.profileOpen = false;
    this.langOpen = false;
  }
}
