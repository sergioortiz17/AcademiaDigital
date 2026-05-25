import { Component, ChangeDetectorRef } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';

import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  standalone: false
})

export class RegisterComponent {

  // =====================================================
  // VARIABLES GENERALES
  // =====================================================

  currentStep = 1;

  isLoading = false;

  errorMessage = '';
  successMessage = '';

  hidePassword = true;
  hideConfirmPassword = true;

  dniExists = false;
  checkingDni = false;

  submittedData: any = null;

  // =====================================================
  // FORMS
  // =====================================================

  registerForm: FormGroup;
  personalForm: FormGroup;
  academicForm: FormGroup;

  // =====================================================
  // CONSTRUCTOR
  // =====================================================

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {

    // =====================================================
    // FORM REGISTRO
    // =====================================================

    this.registerForm = this.fb.group({

      //dni: ['',[Validators.required,Validators.pattern(/^\d{7,8}$/)]],
      username: ['Prueba'],

      email: [
        '',
        [
          Validators.required,
          Validators.email,
          Validators.maxLength(255)
        ]
      ],

      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(
            /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&._-]).{8,}$/
          )
        ]
      ],

      confirmPassword: [
        '',
        [Validators.required]
      ]

    },
    {
      validators: this.passwordsMatchValidator
    });

    // =====================================================
    // FORM DATOS PERSONALES
    // =====================================================

    this.personalForm = this.fb.group({

      firstName: ['', Validators.required],

      lastName: ['', Validators.required],

      dni: ['', [Validators.required, Validators.pattern(/^\d{7,8}$/)]]

    });

    // =====================================================
    // FORM DATOS ACADÉMICOS
    // =====================================================

    this.academicForm = this.fb.group({

      career: ['', Validators.required],

      shift: ['', Validators.required],

      campus: ['', Validators.required],

      cohort: ['', Validators.required]

    });

  }

  // =====================================================
  // GETTERS
  // =====================================================

  get dni() {
    return this.registerForm.get('dni');
  }

  get username() {
    return this.registerForm.get('username');
  }

  get email() {
    return this.registerForm.get('email');
  }

  get password() {
    return this.registerForm.get('password');
  }

  get confirmPassword() {
    return this.registerForm.get('confirmPassword');
  }

  // =====================================================
  // VALIDADOR PASSWORD
  // =====================================================

  passwordsMatchValidator(
    form: AbstractControl
  ): ValidationErrors | null {

    const password = form.get('password')?.value;

    const confirmPassword =
      form.get('confirmPassword')?.value;

    if (password !== confirmPassword) {

      return {
        passwordMismatch: true
      };

    }

    return null;

  }

  // =====================================================
  // VALIDACIONES VISUALES
  // =====================================================

  hasMinLength(): boolean {

    return (this.password?.value?.length || 0) >= 8;

  }

  hasUpperCase(): boolean {

    return /[A-Z]/.test(
      this.password?.value || ''
    );

  }

  hasLowerCase(): boolean {

    return /[a-z]/.test(
      this.password?.value || ''
    );

  }

  hasSpecialCharacter(): boolean {

    return /[@$!%*?&._-]/.test(
      this.password?.value || ''
    );

  }

  // =====================================================
  // VERIFICAR DNI
  // =====================================================

  //checkDni(): void {

    //if (this.dni?.invalid) {
    //  return;
    //}

    //const dniValue = this.dni?.value;

    //this.checkingDni = true;

    //this.dniExists = false;

    //this.authService.checkDniExists(dniValue)
    //  .subscribe({

    //  next: (exists: boolean) => {

    //    this.checkingDni = false;

    //    if (exists) {

    //      this.dniExists = true;

    //      this.errorMessage =
    //        'El DNI ya se encuentra registrado';

    //    } else {

    //      this.dniExists = false;

    //      this.errorMessage = '';

    //    }

    //  },

    //  error: () => {

    //    this.checkingDni = false;

   //     this.errorMessage =
   //       'Error al verificar el DNI';

   //   }

  //  });

  //}

  // =====================================================
  // PASO 1 -> PASO 2
  // =====================================================

  // =====================================================
  // PASO 1 -> REGISTRO (llamada a la API)
  // =====================================================

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
      DNI: this.personalForm.value.dni
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

  // =====================================================
  // PASO 3 -> PASO 4 (datos académicos opcionales)
  // =====================================================

  nextStepOne(): void {
    this.currentStep = 3;
  }

  submitFullRegister(): void {
    this.currentStep = 4;
  }

  skipStep(): void {
    this.currentStep = 4;
  }

  // =====================================================
  // VOLVER PASOS
  // =====================================================

  previousStep(): void {
    if (this.currentStep === 3) {
      this.currentStep = 1;
    } else if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

}
