import { ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthService } from '../../../core/services/auth.service';
import { accountInitialize } from '../../../store/account/account.actions';
import { finalize } from 'rxjs/internal/operators/finalize';
import { EMPTY } from 'rxjs/internal/observable/empty';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError } from 'rxjs/internal/operators/catchError';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  standalone: false
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  hidePassword = true;

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly store: Store,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
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
          localStorage.setItem('academia-account', JSON.stringify({ token: response.token, user: response.user }));
          if (rememberMe) {
            localStorage.setItem('remember-user',JSON.stringify({email}));
          } else {
            localStorage.removeItem('remember-user');
          }
          const user = { ...response.user, role: response.user?.role ?? null };
          this.store.dispatch(accountInitialize({ isLoggedIn: true, user, token: response.token }));
          this.router.navigate(['/app/dashboard/default']);
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
