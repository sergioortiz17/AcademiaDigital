import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

/**
 * Recuperación de contraseña en dos momentos:
 *  1. /auth/forgot-password — email y DNI. El backend envía un correo con un enlace (si los
 *     datos corresponden a una cuenta) y responde siempre lo mismo: el token nunca llega acá.
 *  2. /auth/reset-password?token=... — el enlace del correo abre esta misma pantalla directo
 *     en el formulario de contraseña nueva.
 *
 * Pasos: 1 datos · 2 "revisa tu correo" · 3 contraseña nueva · 4 listo.
 */
@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss'],
  standalone: false
})
export class ForgotPasswordComponent implements OnInit {

  currentStep = 1;

  // Paso 1 — email + DNI
  step1Form: FormGroup;
  step1Loading = false;
  step1Error = '';
  sentToEmail = '';

  // Paso 3 — contraseña nueva
  passwordForm: FormGroup;
  passwordLoading = false;
  passwordError = '';
  hideNewPassword = true;
  hideConfirmPassword = true;

  private resetToken = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.step1Form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      dni: ['', [Validators.required, Validators.pattern(/^\d{7,8}$/)]]
    });

    this.passwordForm = this.fb.group({
      newPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&._-]).{8,}$/)
      ]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordsMatch });
  }

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (token) {
      this.resetToken = token;
      this.currentStep = 3;
      // El token no queda en la barra de direcciones ni en el historial del navegador.
      history.replaceState(null, '', '/auth/reset-password');
    }
  }

  private passwordsMatch(form: AbstractControl): ValidationErrors | null {
    const np = form.get('newPassword')?.value;
    const cp = form.get('confirmPassword')?.value;
    return np === cp ? null : { passwordMismatch: true };
  }

  get newPassword() { return this.passwordForm.get('newPassword'); }

  hasMinLength(): boolean { return (this.newPassword?.value?.length || 0) >= 8; }
  hasUpperCase(): boolean { return /[A-Z]/.test(this.newPassword?.value || ''); }
  hasLowerCase(): boolean { return /[a-z]/.test(this.newPassword?.value || ''); }
  hasSpecialChar(): boolean { return /[@$!%*?&._-]/.test(this.newPassword?.value || ''); }

  // ── Paso 1 ──────────────────────────────────────────────────────────────────

  submitStep1(): void {
    if (this.step1Form.invalid) {
      this.step1Form.markAllAsTouched();
      return;
    }

    this.step1Loading = true;
    this.step1Error = '';

    const { email, dni } = this.step1Form.value;

    this.authService.forgotPassword(email, dni).subscribe({
      next: () => {
        // Misma respuesta exista o no la cuenta: no se revela qué emails están registrados.
        this.step1Loading = false;
        this.sentToEmail = email;
        this.currentStep = 2;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.step1Loading = false;
        this.step1Error = err?.status === 429
          ? 'Hiciste demasiados pedidos. Espera unos minutos e intenta de nuevo.'
          : 'Error al procesar la solicitud. Intenta de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Paso 3 ───────────────────────────────────────────────────────────────────

  submitPassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.passwordLoading = true;
    this.passwordError = '';

    const { newPassword } = this.passwordForm.value;

    this.authService.resetPassword(this.resetToken, newPassword).subscribe({
      next: () => {
        this.passwordLoading = false;
        this.currentStep = 4;
        this.cdr.detectChanges();
      },
      error: () => {
        this.passwordLoading = false;
        this.passwordError = 'El enlace venció o ya fue usado. Pide uno nuevo desde "¿Olvidaste tu contraseña?".';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Navegación ───────────────────────────────────────────────────────────────

  goBack(): void {
    if (this.currentStep === 2) {
      this.currentStep = 1;
    } else {
      this.router.navigate(['/auth/signin']);
    }
  }

  requestNewLink(): void {
    this.resetToken = '';
    this.passwordError = '';
    this.currentStep = 1;
    this.router.navigate(['/auth/forgot-password']);
  }

  goLogin(): void {
    this.router.navigate(['/auth/signin']);
  }
}
