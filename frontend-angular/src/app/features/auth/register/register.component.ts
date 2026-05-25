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

      fullName: ['', Validators.required],

      dni: ['', Validators.required],

      gender: ['', Validators.required],

      cuil: ['', Validators.required],

      birthDate: ['', Validators.required],

      phoneCode: [''],

      phone: ['', Validators.required]

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

  nextStepRegister(): void {

    // VALIDAR FORM

    if (this.registerForm.invalid) {

      this.registerForm.markAllAsTouched();

      this.errorMessage =
        'Completa correctamente todos los campos';

      return;

    }

    // ESPERANDO VERIFICACIÓN DNI

    //if (this.checkingDni) {

    //  this.errorMessage =
    //    'Esperando validación del DNI...';

    //  return;

    //}

    // DNI YA EXISTE

    //if (this.dniExists) {

    //  this.errorMessage =
    //    'El DNI ya está registrado';

    //  return;

    //}

    // LIMPIAR MENSAJES

    this.errorMessage = '';

    // AUTOCOMPLETAR DNI EN FORM PERSONAL

    //this.personalForm.patchValue({
    //  dni: this.registerForm.value.dni
    //});

    // AVANZAR

    this.currentStep = 2;

  }

  // =====================================================
  // PASO 2 -> PASO 3
  // =====================================================

  nextStepOne(): void {

    if (this.personalForm.invalid) {

      this.personalForm.markAllAsTouched();

      return;

    }

    this.currentStep = 3;

  }

  // =====================================================
  // PASO 3 -> REGISTRO FINAL
  // =====================================================

  submitFullRegister(): void {

    // VALIDAR FORM ACADÉMICO

    if (this.academicForm.invalid) {

      this.academicForm.markAllAsTouched();

      return;

    }

    // LOADING

    this.isLoading = true;

    this.errorMessage = '';

    // =====================================================
    // COMBINAR TODOS LOS DATOS
    // =====================================================

    const registerData = {

      // DATOS LOGIN

      //dni: this.registerForm.value.dni,

      username: this.registerForm.value.username,

      email: this.registerForm.value.email,

      password: this.registerForm.value.password,

      // DATOS PERSONALES

      //fullName: this.personalForm.value.fullName,

      //gender: this.personalForm.value.gender,

      //cuil: this.personalForm.value.cuil,

      //birthDate: this.personalForm.value.birthDate,

      //phoneCode: this.personalForm.value.phoneCode,

      //phone: this.personalForm.value.phone,

      // DATOS ACADÉMICOS

      //career: this.academicForm.value.career,

      //shift: this.academicForm.value.shift,

      //campus: this.academicForm.value.campus,

      //cohort: this.academicForm.value.cohort

    };

    // =====================================================
    // POST REGISTER
    // =====================================================

    this.authService.register(registerData)
      .subscribe({

      next: (response) => {

        console.log(response);
        //this.isLoading = false;


        if (response.success) {

          this.successMessage =
            'Usuario registrado correctamente';

          // DATOS PARA PANTALLA FINAL

          this.submittedData = {

            career:
              this.academicForm.value.career,

            shift:
              this.academicForm.value.shift

          };

          // IR A PANTALLA ÉXITO

          this.currentStep = 4;
          this.isLoading = false;
          this.cdr.detectChanges();

        } else {
          this.isLoading = false;

          this.errorMessage =
            response.msg || 'Error en el registro';

        }

      },

      error: (err) => {

        this.isLoading = false;

        this.errorMessage =
          err.message || 'Error en el registro';

      }

    });

  }

  skipStep(): void {

  this.submittedData = {

    career: 'No especificada',

    shift: 'No especificado'

  };

  this.currentStep = 4;
  }
  // =====================================================
  // VOLVER PASOS
  // =====================================================

  previousStep(): void {

    if (this.currentStep > 1) {

      this.currentStep--;

    }

  }

}
