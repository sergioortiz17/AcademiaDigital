import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, NgZone, OnDestroy, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthService } from '../../../core/services/auth.service';
import { accountInitialize } from '../../../store/account/account.actions';
import { finalize } from 'rxjs/internal/operators/finalize';
import { EMPTY } from 'rxjs/internal/observable/empty';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError } from 'rxjs/internal/operators/catchError';
import { Observable } from 'rxjs';
import { CurrentTenant, DEFAULT_TENANT_SLUG, TenantService } from '../../../core/services/tenant.service';

/** API mínima de Google Identity Services que usa la pantalla. */
interface GoogleIdentityApi {
  accounts: { id: {
    initialize(options: { client_id: string; callback: (response: { credential: string }) => void; ux_mode?: string }): void;
    renderButton(element: HTMLElement, options: Record<string, unknown>): void;
    cancel(): void;
  } };
}

const GOOGLE_SCRIPT_URL = 'https://accounts.google.com/gsi/client';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  standalone: false
})
export class LoginComponent implements AfterViewInit, OnDestroy {
  @ViewChild('googleButton') googleButton?: ElementRef<HTMLDivElement>;
  /** Hay Client ID configurado: se muestra "Continuar con Google". */
  googleEnabled = false;
  readonly tenant$: Observable<CurrentTenant | null>;
  readonly defaultTenantSlug = DEFAULT_TENANT_SLUG;
  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  hidePassword = true;

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly store: Store,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly tenantService: TenantService,
    private readonly zone: NgZone
  ) {
    this.tenant$ = this.tenantService.tenant$;
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });

    this.loginForm.valueChanges.subscribe(() => {
      if (this.errorMessage) {
        this.errorMessage = '';
      }
    });

    //Recuerdame
    const rememberedUser = localStorage.getItem('remember-user');
    if (rememberedUser) {
      try{
        const parsedUser = JSON.parse(rememberedUser);
        this.loginForm.patchValue({
          email: parsedUser.email,
          rememberMe: true
        });
      } catch {
        localStorage.removeItem('remember-user');
      }
    }
  }

  ngAfterViewInit(): void {
    this.authService.getGoogleConfig().pipe(catchError(() => EMPTY)).subscribe(({ clientId }) => {
      if (!clientId) return;
      this.googleEnabled = true;
      this.changeDetectorRef.detectChanges();
      this.loadGoogleScript().then(google => {
        if (!google || !this.googleButton) return;
        google.accounts.id.initialize({
          client_id: clientId,
          callback: response => this.zone.run(() => this.onGoogleCredential(response.credential))
        });
        google.accounts.id.renderButton(this.googleButton.nativeElement, {
          theme: 'outline', size: 'large', text: 'continue_with', shape: 'rectangular', locale: 'es', width: 320
        });
      });
    });
  }

  ngOnDestroy(): void {
    (window as unknown as { google?: GoogleIdentityApi }).google?.accounts.id.cancel();
  }

  private loadGoogleScript(): Promise<GoogleIdentityApi | null> {
    const existing = (window as unknown as { google?: GoogleIdentityApi }).google;
    if (existing?.accounts?.id) return Promise.resolve(existing);
    return new Promise(resolve => {
      const script = document.createElement('script');
      script.src = GOOGLE_SCRIPT_URL;
      script.async = true;
      script.defer = true;
      script.onload = () => resolve((window as unknown as { google?: GoogleIdentityApi }).google ?? null);
      script.onerror = () => resolve(null);
      document.head.appendChild(script);
    });
  }

  private onGoogleCredential(credential: string): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.authService.loginWithGoogle(credential).pipe(
      catchError((error: HttpErrorResponse) => {
        this.errorMessage = error.status === 404
          ? (error.error?.msg || 'No hay una cuenta registrada con ese correo de Google.')
          : this.getLoginErrorMessage(error);
        this.changeDetectorRef.detectChanges();
        return EMPTY;
      }),
      finalize(() => {
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      })
    ).subscribe(response => this.completeLogin(response));
  }

  private completeLogin(response: { success?: boolean; token?: string; user?: any; msg?: string }): boolean {
    if (!response.success || !response.token) {
      this.errorMessage = response.msg || 'No se pudo iniciar sesión. Intentá nuevamente.';
      return false;
    }
    localStorage.setItem('academia-account', JSON.stringify({ token: response.token, user: response.user }));
    const user = { ...response.user, role: response.user?.role ?? null };
    this.store.dispatch(accountInitialize({ isLoggedIn: true, user, token: response.token }));
    this.router.navigate(['/app/dashboard/default']);
    return true;
  }

  onSubmit(): void {
    if (this.loginForm.invalid) { this.loginForm.markAllAsTouched(); return; }
    this.isLoading = true;
    this.errorMessage = '';
    const { email, password, rememberMe } = this.loginForm.value;
    this.authService.login({ email, password }).pipe(
      catchError((error: HttpErrorResponse) => {
        this.errorMessage = this.getLoginErrorMessage(error);
        this.changeDetectorRef.detectChanges();
        return EMPTY;
      }),
      finalize(() => {
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      })
    ).subscribe({
      next: (response) => {
        if (response.success && response.token) {
          if (rememberMe) {
            localStorage.setItem('remember-user',JSON.stringify({email}));
          } else {
            localStorage.removeItem('remember-user');
          }
          this.completeLogin(response);
        } else {
          this.errorMessage = response.msg || 'El correo electrónico y/o contraseña ingresados no son correctos.';
        }
      }
    });
  }

  private getLoginErrorMessage(error: HttpErrorResponse | null | undefined): string {
    if (!error) {
      return 'No se pudo iniciar sesión. Intentá nuevamente.';
    }

    if (error.status === 423) {
      return 'Tu cuenta fue bloqueada temporalmente por demasiados intentos fallidos. Intentá nuevamente en 15 minutos.';
    }

    if (error.status === 401) {
      return 'El correo electrónico y/o contraseña ingresados no son correctos. Verificá tus datos e intentá nuevamente.';
    }

    return error.error?.msg || error.error?.message || error.message || 'No se pudo iniciar sesión. Intentá nuevamente.';
  }

}
