import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthService } from '../../../core/services/auth.service';
import { accountInitialize } from '../../../store/account/account.actions';

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
    private readonly router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });

    //Recuerdame
    const rememberedUser = localStorage.getItem('remember-user');
    if (rememberedUser) {
      const parsedUser = JSON.parse(rememberedUser);
    this.loginForm.patchValue({
    email: parsedUser.email,
    password: parsedUser.password,
    rememberMe: true
  });
}
  }

  onSubmit(): void {
    if (this.loginForm.invalid) { this.loginForm.markAllAsTouched(); return; }
    this.isLoading = true;
    this.errorMessage = '';
    const { email, password, rememberMe } = this.loginForm.value;
    this.authService.login({ email, password }).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.token) {
          localStorage.setItem('academia-account', JSON.stringify({ token: response.token, user: response.user }));
          if (rememberMe) {
            localStorage.setItem('remember-user',JSON.stringify({email,password}));
          } else {
            localStorage.removeItem('remember-user');
          }
          const user = { ...response.user, role: response.user?.role ?? null };
          this.store.dispatch(accountInitialize({ isLoggedIn: true, user, token: response.token }));
          this.router.navigate(['/app/dashboard/default']);
        } else {
          this.errorMessage = response.msg || 'Login failed';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.message || 'Login failed';
      }
    });
  }
}
