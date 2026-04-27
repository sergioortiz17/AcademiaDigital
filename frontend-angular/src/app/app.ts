import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Store } from '@ngrx/store';
import { accountInitialize } from './store/account/account.actions';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss'
})
export class App implements OnInit {
  constructor(
    private translate: TranslateService,
    private store: Store
  ) {}

  ngOnInit(): void {
    // Setup i18n
    const savedLang = localStorage.getItem('i18nextLng') || 'es';
    this.translate.setDefaultLang('es');
    this.translate.use(savedLang);

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
