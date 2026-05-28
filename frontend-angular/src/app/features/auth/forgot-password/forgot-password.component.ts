import { Component, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss'],
  standalone: false
})
export class ForgotPasswordComponent {

  currentStep = 1;

  // Step 1 — email + DNI
  step1Form: FormGroup;
  step1Loading = false;
  step1Error = '';

  // Step 2 — OTP code
  otpDigits: string[] = ['', '', '', '', '', ''];
  otpError = '';

  // Step 3 — new password
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

  // ── Step 1 ──────────────────────────────────────────────────────────────────

  submitStep1(): void {
    if (this.step1Form.invalid) {
      this.step1Form.markAllAsTouched();
      return;
    }

    this.step1Loading = true;
    this.step1Error = '';

    const { email, dni } = this.step1Form.value;

    this.authService.forgotPassword(email, dni).subscribe({
      next: (res) => {
        this.step1Loading = false;
        if (!res.resetToken) {
          this.step1Error = 'No encontramos una cuenta con ese correo y DNI. Verificá los datos.';
          this.cdr.detectChanges();
          return;
        }
        this.resetToken = res.resetToken;
        this.currentStep = 2;
        this.cdr.detectChanges();
      },
      error: () => {
        this.step1Loading = false;
        this.step1Error = 'Error al procesar la solicitud. Intentá de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Step 2 — OTP ─────────────────────────────────────────────────────────────

  trackByIndex(index: number): number { return index; }

  onDigitInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    const cleaned = input.value.replace(/\D/g, '');
    input.value = cleaned ? (cleaned.at(-1) ?? '') : '';

    if (input.value && index < 5) {
      (document.getElementById(`otp-${index + 1}`) as HTMLInputElement)?.focus();
    }
  }

  onDigitKeydown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace') {
      const input = event.target as HTMLInputElement;
      if (!input.value && index > 0) {
        (document.getElementById(`otp-${index - 1}`) as HTMLInputElement)?.focus();
      }
    }
  }

  submitOtp(): void {
    const code = Array.from({ length: 6 }, (_, i) =>
      (document.getElementById(`otp-${i}`) as HTMLInputElement)?.value ?? ''
    ).join('');

    if (code.length < 6) {
      this.otpError = 'Ingresá los 6 dígitos del código.';
      return;
    }

    if (code !== '000000') {
      this.otpError = 'Código incorrecto. Verificá e intentá de nuevo.';
      return;
    }

    this.otpError = '';
    this.currentStep = 3;
    this.cdr.detectChanges();
  }

  // ── Step 3 ───────────────────────────────────────────────────────────────────

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
      error: (err) => {
        this.passwordLoading = false;
        this.passwordError = err?.error?.msg || 'Error al actualizar la contraseña.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Navigation ───────────────────────────────────────────────────────────────

  goBack(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    } else {
      this.router.navigate(['/auth/signin']);
    }
  }

  goLogin(): void {
    this.router.navigate(['/auth/signin']);
  }
}
