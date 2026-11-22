import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { UserService, ProfileData, ProfileContactInfo } from '../../core/services/user.service';
import { Student, StudentRecord, StudentService } from '../../core/services/student.service';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { UserRole } from '../../store/account/account.actions';

interface StudentProfileDetails {
  student: Student;
  record: StudentRecord | null;
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  standalone: false
})
export class ProfileComponent implements OnInit {

  profile: ProfileData | null = null;
  student: Student | null = null;
  isLoadingProfile = true;

  profileForm: FormGroup;
  profileSaving = false;

  /** Documento, domicilio y contacto: los carga cualquier rol (son los datos de las planillas). */
  contactForm: FormGroup;
  hasContactInfo = false;
  contactSaving = false;
  profileError = '';

  passwordForm: FormGroup;
  passwordSaving = false;

  hideCurrentPassword = true;
  hideNewPassword = true;
  hideConfirmPassword = true;

  maxBirthDate = new Date();

  readonly documentTypes = [
    { value: 'DNI', label: 'DNI' },
    { value: 'LC', label: 'Libreta cívica' },
    { value: 'LE', label: 'Libreta de enrolamiento' },
    { value: 'CI', label: 'Cédula de identidad' },
    { value: 'PAS', label: 'Pasaporte' }
  ];

  /** El legajo académico solo se muestra al alumno. */
  get isStudent(): boolean {
    return this.profile?.role === UserRole.Alumno;
  }

  constructor(
    private readonly fb: FormBuilder,
    private readonly userService: UserService,
    private readonly studentService: StudentService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.profileForm = this.fb.group({
      username:  ['', Validators.required],
      lastName:  ['', Validators.required],
      gender:    [''],
      // Mismas reglas que el servidor: CUIL de 11 dígitos y teléfono argentino de 10 dígitos
      // (código de área de 2 a 4 sin el 0, número de 6 a 8 sin el 15).
      cuil:      ['', Validators.pattern(/^\d{2}-?\d{8}-?\d$/)],
      birthDate: [null],
      phoneCode: ['', Validators.pattern(/^\d{2,4}$/)],
      phone:     ['', Validators.pattern(/^\d{6,8}$/)]
    }, { validators: phoneLengthValidator });

    this.contactForm = this.fb.group({
      documentType: ['DNI'],
      alternatePhone: ['', Validators.pattern(/^\+?[\d\s-]{6,20}$/)],
      addressLine: ['', Validators.maxLength(300)],
      neighborhood: ['', Validators.maxLength(120)],
      city: ['', Validators.maxLength(120)],
      district: ['', Validators.maxLength(120)],
      province: ['', Validators.maxLength(120)],
      country: ['', Validators.maxLength(100)],
      postalCode: ['', Validators.maxLength(20)],
      emergencyContactName: ['', Validators.maxLength(200)],
      emergencyContactRelationship: ['', Validators.maxLength(100)],
      emergencyContactPhone: ['', Validators.pattern(/^\+?[\d\s-]{6,20}$/)],
      healthInsurance: ['', Validators.maxLength(150)]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&._-]).{8,}$/)
      ]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordsMatchValidator });
  }

  ngOnInit(): void {
    this.userService.getProfile().pipe(
      switchMap((data) => {
        this.profile = data;
        this.profileForm.patchValue({
          username:  data.username,
          lastName:  data.lastName,
          gender:    data.gender ?? '',
          cuil:      data.cuil ?? '',
          birthDate: data.birthDate ? new Date(data.birthDate) : null,
          phoneCode: data.phoneCode ?? '',
          phone:     data.phone ?? ''
        });

        // Solo un Alumno tiene ficha de estudiante. Para Admin y Profesor estas consultas
        // responden 404 ("El usuario actual no es un alumno.") y el interceptor global lo
        // mostraba como error al abrir el perfil.
        if (data.role !== UserRole.Alumno) return of(null);

        return this.studentService.getMyAcademicProgress().pipe(
          switchMap((progress) => this.loadStudentDetails(progress.studentId)),
          catchError(() => this.studentService.getStudent(data.id).pipe(
            switchMap((student) => this.loadStudentDetails(student.id)),
            catchError(() => of(null))
          ))
        );
      })
    ).subscribe({
      next: (details) => {
        this.student = details?.student ?? null;
        this.isLoadingProfile = false;
        this.loadContactInfo();
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingProfile = false;
        this.profileError = 'Error al cargar el perfil';
        this.cdr.detectChanges();
      }
    });
  }

  private loadContactInfo(): void {
    this.userService.getContactInfo().pipe(catchError(() => of(null))).subscribe(info => {
      this.hasContactInfo = info != null;
      if (info) this.contactForm.patchValue({ ...mapNulls(info), documentType: info.documentType || 'DNI' });
      this.contactForm.markAsPristine();
      this.cdr.detectChanges();
    });
  }

  get phoneErrorText(): string {
    const form = this.profileForm;
    const touched = form.get('phoneCode')?.touched || form.get('phone')?.touched;
    if (!touched) return '';
    if (form.get('phoneCode')?.hasError('pattern')) return 'El código de área tiene entre 2 y 4 dígitos, sin el 0.';
    if (form.get('phone')?.hasError('pattern')) return 'El número tiene entre 6 y 8 dígitos, sin el 15.';
    if (form.hasError('phoneIncomplete')) return 'Complete el código de área y el número.';
    if (form.hasError('phoneLength')) return 'El código de área y el número juntos deben sumar 10 dígitos.';
    return '';
  }

  saveContact(): void {
    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      return;
    }
    this.contactSaving = true;
    const raw = this.contactForm.value as Record<string, string | null>;
    const payload = Object.fromEntries(
      Object.entries(raw).map(([key, value]) => [key, (value ?? '').trim() || null])) as unknown as ProfileContactInfo;
    this.userService.updateContactInfo(payload).subscribe({
      next: saved => {
        this.contactSaving = false;
        this.contactForm.patchValue({ ...mapNulls(saved), documentType: saved.documentType || 'DNI' });
        this.contactForm.markAsPristine();
        void Swal.fire({ icon: 'success', titleText: 'Datos de contacto guardados', confirmButtonText: 'Aceptar' });
        this.cdr.detectChanges();
      },
      error: err => {
        this.contactSaving = false;
        void Swal.fire({
          icon: 'error',
          titleText: 'No se pudieron guardar los datos de contacto',
          text: this.errorMessage(err, 'Revise los datos e intente nuevamente.'),
          confirmButtonText: 'Aceptar'
        });
        this.cdr.detectChanges();
      }
    });
  }

  private loadStudentDetails(studentId: number): Observable<StudentProfileDetails> {
    return forkJoin({
      student: this.studentService.getStudent(studentId),
      record: this.studentService.getStudentRecord(studentId).pipe(catchError(() => of(null)))
    });
  }

  private passwordsMatchValidator(form: AbstractControl): ValidationErrors | null {
    const newPass = form.get('newPassword')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return newPass === confirm ? null : { passwordMismatch: true };
  }

  get newPassword() { return this.passwordForm.get('newPassword'); }

  hasMinLength(): boolean { return (this.newPassword?.value?.length || 0) >= 8; }
  hasUpperCase(): boolean { return /[A-Z]/.test(this.newPassword?.value || ''); }
  hasLowerCase(): boolean { return /[a-z]/.test(this.newPassword?.value || ''); }
  hasSpecialChar(): boolean { return /[@$!%*?&._-]/.test(this.newPassword?.value || ''); }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.profileSaving = true;
    this.profileError = '';

    const val = this.profileForm.value;
    const payload = {
      username:  val.username,
      lastName:  val.lastName,
      gender:    val.gender || null,
      cuil:      val.cuil || null,
      birthDate: val.birthDate ? (val.birthDate as Date).toISOString() : null,
      phoneCode: val.phoneCode || null,
      phone:     val.phone || null
    };

    let profileSaved = false;
    this.userService.updateProfile(payload).pipe(
      tap((res) => {
        profileSaved = true;
        this.profile = res;
        this.profileForm.patchValue({
          username: res.username,
          lastName: res.lastName,
          gender: res.gender ?? '',
          cuil: res.cuil ?? '',
          birthDate: res.birthDate ? new Date(res.birthDate) : null,
          phoneCode: res.phoneCode ?? '',
          phone: res.phone ?? ''
        });
      })
    ).subscribe({
      next: () => {
        this.profileSaving = false;
        this.profileForm.markAsPristine();
        void Swal.fire({
          icon: 'success',
          titleText: 'Perfil actualizado',
          text: 'Los datos se guardaron correctamente.',
          confirmButtonText: 'Aceptar'
        });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.profileSaving = false;
        void Swal.fire({
          icon: 'error',
          titleText: 'No se pudo guardar el perfil',
          text: this.errorMessage(err, 'Ocurrió un error al guardar los datos.'),
          confirmButtonText: 'Aceptar'
        });
        this.cdr.detectChanges();
      }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.passwordSaving = true;

    const { currentPassword, newPassword } = this.passwordForm.value;

    this.userService.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.passwordSaving = false;
        this.passwordForm.reset();
        void Swal.fire({
          icon: 'success',
          titleText: 'Contraseña actualizada',
          text: 'La contraseña se cambió correctamente.',
          confirmButtonText: 'Aceptar'
        });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.passwordSaving = false;
        void Swal.fire({
          icon: 'error',
          titleText: 'No se pudo cambiar la contraseña',
          text: this.errorMessage(err, 'Ocurrió un error al cambiar la contraseña.'),
          confirmButtonText: 'Aceptar'
        });
        this.cdr.detectChanges();
      }
    });
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (!error || typeof error !== 'object') return fallback;
    const response = error as {
      error?: { detail?: string; msg?: string; message?: string };
      message?: string;
    };
    return response.error?.detail || response.error?.msg || response.error?.message
      || response.message || fallback;
  }
}

/** Código de área + número deben sumar 10 dígitos (numeración argentina sin 0 ni 15). */
function phoneLengthValidator(form: AbstractControl): ValidationErrors | null {
  const code = String(form.get('phoneCode')?.value ?? '').trim();
  const phone = String(form.get('phone')?.value ?? '').trim();
  if (!code && !phone) return null;
  if (!code || !phone) return { phoneIncomplete: true };
  return code.length + phone.length === 10 ? null : { phoneLength: true };
}

function mapNulls(info: ProfileContactInfo): Record<string, string> {
  return Object.fromEntries(Object.entries(info).map(([key, value]) => [key, value ?? '']));
}
