import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CareerService, Career } from '../../../core/services/career.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  standalone: false
})
export class RegisterComponent implements OnInit {

  currentStep = 1;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  hidePassword = true;
  hideConfirmPassword = true;
  careers: Career[] = [];

  registerForm: FormGroup;
  personalForm: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly careerService: CareerService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&._-]).{8,}$/)
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordsMatchValidator });

    this.personalForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      dni: ['', [Validators.required, Validators.pattern(/^\d{7,8}$/)]],
      careerId: [null, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.careerService.getCareers().subscribe({
      next: data => this.careers = data,
      error: err => console.error('Error loading careers', err)
    });
  }

  get email() { return this.registerForm.get('email'); }
  get password() { return this.registerForm.get('password'); }
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }

  passwordsMatchValidator(form: AbstractControl): ValidationErrors | null {
    const password = form.get('password')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  hasMinLength(): boolean { return (this.password?.value?.length || 0) >= 8; }
  hasUpperCase(): boolean { return /[A-Z]/.test(this.password?.value || ''); }
  hasLowerCase(): boolean { return /[a-z]/.test(this.password?.value || ''); }
  hasSpecialCharacter(): boolean { return /[@$!%*?&._-]/.test(this.password?.value || ''); }

  nextStepRegister(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      this.errorMessage = 'Completa correctamente todos los campos';
      return;
    }
    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      this.errorMessage = 'Completa correctamente todos los campos';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const registerData = {
      name: this.personalForm.value.firstName,
      lastname: this.personalForm.value.lastName,
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
      DNI: this.personalForm.value.dni,
      careerId: this.personalForm.value.careerId
    };

    this.authService.register(registerData).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.currentStep = 3;
          this.cdr.detectChanges();
        } else {
          this.errorMessage = response.msg || 'Error en el registro';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.msg || err.message || 'Error en el registro';
      }
    });
  }

  previousStep(): void {
    if (this.currentStep > 1) this.currentStep--;
  }
}
