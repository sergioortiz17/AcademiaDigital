import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { logout, UserModel } from '../../../store/account/account.actions';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { selectToken, selectUser } from '../../../store/account/account.selectors';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-nav-right',
  templateUrl: './nav-right.component.html',
  styleUrls: ['./nav-right.component.scss'],
  standalone: false
})
export class NavRightComponent {
  profileOpen = false;
  user$: Observable<UserModel | null>;

  constructor(
    private readonly store: Store,
    private readonly router: Router,
    private readonly authService: AuthService,
    readonly themeService: ThemeService
  ) {
    this.user$ = this.store.select(selectUser);
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
  }

  closeAll(): void {
    this.profileOpen = false;
  }
}
