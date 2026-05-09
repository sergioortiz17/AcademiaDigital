import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { logout } from '../../../store/account/account.actions';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { selectToken } from '../../../store/account/account.selectors';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-nav-right',
  templateUrl: './nav-right.component.html',
  styleUrls: ['./nav-right.component.scss'],
  standalone: false
})
export class NavRightComponent {
  profileOpen = false;
  langOpen = false;

  constructor(
    private readonly store: Store,
    private readonly router: Router,
    private readonly translate: TranslateService,
    private readonly authService: AuthService,
    readonly themeService: ThemeService
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

  toggleProfile(): void {
    this.profileOpen = !this.profileOpen;
    this.langOpen = false;
  }

  toggleLang(): void {
    this.langOpen = !this.langOpen;
    this.profileOpen = false;
  }

  closeAll(): void {
    this.profileOpen = false;
    this.langOpen = false;
  }
}
