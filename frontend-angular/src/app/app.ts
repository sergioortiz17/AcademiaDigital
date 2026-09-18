import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Store } from '@ngrx/store';
import { accountInitialize } from './store/account/account.actions';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss'
})
export class App implements OnInit {
  constructor(
    private translate: TranslateService,
    private store: Store,
    private themeService: ThemeService
  ) {}

  ngOnInit(): void {
    // La app es siempre en español, sin selector de idioma. Se ignora
    // (y se limpia) cualquier preferencia vieja guardada en localStorage
    // de cuando existía el selector de inglés.
    localStorage.removeItem('i18nextLng');
    this.translate.setDefaultLang('es');
    this.translate.use('es');

    // Restore persisted auth session
    try {
      const raw = localStorage.getItem('academia-account');
      if (raw) {
        const state = JSON.parse(raw);
        if (state.token) {
          this.store.dispatch(accountInitialize({
            isLoggedIn: true,
            user: state.user,
            token: state.token
          }));
        }
      }
    } catch {
      // ignore parse errors
    }
  }
}
