import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable, Subscription, timer } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { logout, UserModel } from '../../../store/account/account.actions';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { TimeTravelService, TimeTravelStatus } from '../../../core/services/time-travel.service';
import { selectToken, selectUser } from '../../../store/account/account.selectors';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-nav-right',
  templateUrl: './nav-right.component.html',
  styleUrls: ['./nav-right.component.scss'],
  standalone: false
})
export class NavRightComponent implements OnInit, OnDestroy {
  profileOpen = false;
  user$: Observable<UserModel | null>;
  timeEnabled = false;
  effectiveDate: Date | null = null;
  private offsetMs = 0;
  private timeSub?: Subscription;
  private tickSub?: Subscription;

  constructor(
    private readonly store: Store,
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly timeTravelService: TimeTravelService,
    readonly themeService: ThemeService
  ) {
    this.user$ = this.store.select(selectUser);
  }

  ngOnInit(): void {
    // Resync liviano cada 30s contra el backend (no cada segundo): solo para enterarse si
    // "Volver al futuro" está prendido/apagado o si la fecha simulada cambió desde otro lado
    // (otra pestaña, DevTools, curl). El reloj mostrado en pantalla NO pega al backend: se
    // calcula localmente sumando el tiempo real transcurrido al offset (ver tick()).
    this.timeSub = timer(0, 30000)
      .pipe(switchMap(() => this.timeTravelService.getStatus()))
      .subscribe(status => this.applyStatus(status));

    this.tickSub = timer(0, 15000).subscribe(() => this.tick());
  }

  ngOnDestroy(): void {
    this.timeSub?.unsubscribe();
    this.tickSub?.unsubscribe();
  }

  private applyStatus(status: TimeTravelStatus | null): void {
    this.timeEnabled = !!status?.enabled;
    const effective = status?.effectiveNow ? new Date(status.effectiveNow) : new Date();
    this.offsetMs = effective.getTime() - Date.now();
    this.tick();
  }

  private tick(): void {
    this.effectiveDate = new Date(Date.now() + this.offsetMs);
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
