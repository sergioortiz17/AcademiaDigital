import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from '../../../core/services/auth.service';

vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn().mockResolvedValue({ isConfirmed: true }) }
}));

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let authService: {
    forgotPassword: ReturnType<typeof vi.fn>;
    verifyResetCode: ReturnType<typeof vi.fn>;
    resetPassword: ReturnType<typeof vi.fn>;
  };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authService = {
      forgotPassword: vi.fn(),
      verifyResetCode: vi.fn(),
      resetPassword: vi.fn()
    };
    router = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      declarations: [ForgotPasswordComponent],
      imports: [ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
  });

  it('starts at step 1', () => {
    expect(component.currentStep).toBe(1);
  });

  // ── Step 1 ───────────────────────────────────────────────────────────────────

  describe('step1Form', () => {
    it('is invalid when empty', () => {
      expect(component.step1Form.invalid).toBe(true);
    });

    it('rejects malformed email', () => {
      component.step1Form.setValue({ email: 'notanemail', dni: '12345678' });
      expect(component.step1Form.get('email')!.invalid).toBe(true);
    });

    it('rejects DNI shorter than 7 digits', () => {
      component.step1Form.setValue({ email: 'a@b.com', dni: '123456' });
      expect(component.step1Form.get('dni')!.invalid).toBe(true);
    });

    it('accepts DNI of 7 digits', () => {
      component.step1Form.setValue({ email: 'a@b.com', dni: '1234567' });
      expect(component.step1Form.valid).toBe(true);
    });

    it('accepts DNI of 8 digits', () => {
      component.step1Form.setValue({ email: 'a@b.com', dni: '12345678' });
      expect(component.step1Form.valid).toBe(true);
    });
  });

  describe('submitStep1()', () => {
    it('marks as touched when form is invalid', () => {
      vi.spyOn(component.step1Form, 'markAllAsTouched');
      component.submitStep1();
      expect(component.step1Form.markAllAsTouched).toHaveBeenCalled();
      expect(authService.forgotPassword).not.toHaveBeenCalled();
    });

    it('advances to step 2 on success', () => {
      authService.forgotPassword.mockReturnValue(of({ success: true }));
      component.step1Form.setValue({ email: 'a@b.com', dni: '12345678' });
      component.submitStep1();
      expect(component.currentStep).toBe(2);
    });

    it('sets step1Error when success is false', () => {
      authService.forgotPassword.mockReturnValue(of({ success: false }));
      component.step1Form.setValue({ email: 'a@b.com', dni: '12345678' });
      component.submitStep1();
      expect(component.step1Error).toBeTruthy();
      expect(component.currentStep).toBe(1);
    });

    it('sets step1Error on HTTP error', () => {
      authService.forgotPassword.mockReturnValue(throwError(() => new Error('err')));
      component.step1Form.setValue({ email: 'a@b.com', dni: '12345678' });
      component.submitStep1();
      expect(component.step1Error).toBeTruthy();
    });
  });

  // ── Step 2 — OTP ─────────────────────────────────────────────────────────────

  describe('submitOtp()', () => {
    beforeEach(() => {
      // Simulate being on step 2 with a known userEmail stored
      authService.forgotPassword.mockReturnValue(of({ success: true }));
      component.step1Form.setValue({ email: 'a@b.com', dni: '12345678' });
      component.submitStep1();

      // Create fake OTP input DOM nodes
      for (let i = 0; i < 6; i++) {
        const input = document.createElement('input');
        input.id = `otp-${i}`;
        document.body.appendChild(input);
      }
    });

    afterEach(() => {
      for (let i = 0; i < 6; i++) {
        document.getElementById(`otp-${i}`)?.remove();
      }
    });

    it('sets otpError when code has fewer than 6 digits', () => {
      (document.getElementById('otp-0') as HTMLInputElement).value = '1';
      component.submitOtp();
      expect(component.otpError).toBeTruthy();
      expect(authService.verifyResetCode).not.toHaveBeenCalled();
    });

    it('calls verifyResetCode with email and 6-digit code', () => {
      authService.verifyResetCode.mockReturnValue(of({ success: true, resetToken: 'rt' }));
      for (let i = 0; i < 6; i++) {
        (document.getElementById(`otp-${i}`) as HTMLInputElement).value = String(i);
      }
      component.submitOtp();
      expect(authService.verifyResetCode).toHaveBeenCalledWith('a@b.com', '012345');
    });

    it('advances to step 3 on valid code', () => {
      authService.verifyResetCode.mockReturnValue(of({ success: true, resetToken: 'rt' }));
      for (let i = 0; i < 6; i++) {
        (document.getElementById(`otp-${i}`) as HTMLInputElement).value = '1';
      }
      component.submitOtp();
      expect(component.currentStep).toBe(3);
    });

    it('sets otpError when code is wrong', () => {
      authService.verifyResetCode.mockReturnValue(of({ success: false, resetToken: '' }));
      for (let i = 0; i < 6; i++) {
        (document.getElementById(`otp-${i}`) as HTMLInputElement).value = '9';
      }
      component.submitOtp();
      expect(component.otpError).toBeTruthy();
      expect(component.currentStep).toBe(2);
    });
  });

  // ── Step 3 — password ─────────────────────────────────────────────────────────

  describe('passwordForm', () => {
    it('is invalid when empty', () => {
      expect(component.passwordForm.invalid).toBe(true);
    });

    it('rejects password shorter than 8 characters', () => {
      component.passwordForm.patchValue({ newPassword: 'Ab1!', confirmPassword: 'Ab1!' });
      expect(component.passwordForm.get('newPassword')!.invalid).toBe(true);
    });

    it('rejects password without uppercase', () => {
      component.passwordForm.patchValue({ newPassword: 'abc123!@', confirmPassword: 'abc123!@' });
      expect(component.passwordForm.get('newPassword')!.invalid).toBe(true);
    });

    it('flags mismatch when passwords differ', () => {
      component.passwordForm.patchValue({ newPassword: 'ValidPass1!', confirmPassword: 'Other1!' });
      expect(component.passwordForm.errors?.['passwordMismatch']).toBe(true);
    });

    it('is valid with a strong matching password', () => {
      component.passwordForm.patchValue({ newPassword: 'Secure1!', confirmPassword: 'Secure1!' });
      expect(component.passwordForm.valid).toBe(true);
    });
  });

  describe('password strength helpers', () => {
    it('hasMinLength returns false below 8 chars', () => {
      component.passwordForm.patchValue({ newPassword: 'Ab1!' });
      expect(component.hasMinLength()).toBe(false);
    });

    it('hasUpperCase detects uppercase', () => {
      component.passwordForm.patchValue({ newPassword: 'UPPER' });
      expect(component.hasUpperCase()).toBe(true);
    });

    it('hasLowerCase detects lowercase', () => {
      component.passwordForm.patchValue({ newPassword: 'lower' });
      expect(component.hasLowerCase()).toBe(true);
    });

    it('hasSpecialChar detects special chars', () => {
      component.passwordForm.patchValue({ newPassword: 'abc@' });
      expect(component.hasSpecialChar()).toBe(true);
    });

    it('hasSpecialChar returns false without special chars', () => {
      component.passwordForm.patchValue({ newPassword: 'abcDEF123' });
      expect(component.hasSpecialChar()).toBe(false);
    });
  });

  describe('submitPassword()', () => {
    it('marks as touched when form is invalid', () => {
      vi.spyOn(component.passwordForm, 'markAllAsTouched');
      component.submitPassword();
      expect(component.passwordForm.markAllAsTouched).toHaveBeenCalled();
    });

    it('calls resetPassword and shows success modal', async () => {
      authService.resetPassword.mockReturnValue(of({ success: true, msg: 'ok' }));
      component.passwordForm.patchValue({ newPassword: 'Secure1!', confirmPassword: 'Secure1!' });
      component.submitPassword();
      expect(authService.resetPassword).toHaveBeenCalled();
    });

    it('sets passwordError on HTTP error', () => {
      authService.resetPassword.mockReturnValue(
        throwError(() => ({ error: { msg: 'Token inválido' } }))
      );
      component.passwordForm.patchValue({ newPassword: 'Secure1!', confirmPassword: 'Secure1!' });
      component.submitPassword();
      expect(component.passwordError).toBe('Token inválido');
    });
  });

  // ── Navigation ───────────────────────────────────────────────────────────────

  describe('goBack()', () => {
    it('decrements step when above step 1', () => {
      component.currentStep = 2;
      component.goBack();
      expect(component.currentStep).toBe(1);
    });

    it('navigates to signin when already at step 1', () => {
      component.currentStep = 1;
      component.goBack();
      expect(router.navigate).toHaveBeenCalledWith(['/auth/signin']);
    });
  });

  describe('goLogin()', () => {
    it('navigates to signin', () => {
      component.goLogin();
      expect(router.navigate).toHaveBeenCalledWith(['/auth/signin']);
    });
  });
});
