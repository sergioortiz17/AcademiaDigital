import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { UserService, ProfileData } from '../../core/services/user.service';
import { Student, StudentRecord, StudentService } from '../../core/services/student.service';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import Swal from 'sweetalert2';

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
  profileError = '';

  passwordForm: FormGroup;
  passwordSaving = false;

  hideCurrentPassword = true;
  hideNewPassword = true;
  hideConfirmPassword = true;

  maxBirthDate = new Date();

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
      cuil:      [''],
      birthDate: [null],
      phoneCode: [''],
      phone:     [''],
      emergencyContactPhone: ['', Validators.maxLength(30)]
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
        this.profileForm.patchValue({
          emergencyContactPhone: details?.record?.emergencyContact.phone ?? ''
        });
        this.profileForm.get('emergencyContactPhone')?.markAsPristine();
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingProfile = false;
        this.profileError = 'Error al cargar el perfil';
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
    const emergencyContactControl = this.profileForm.get('emergencyContactPhone');
    const emergencyContactUpdate = this.student && emergencyContactControl?.dirty
      ? { studentId: this.student.id, phone: (val.emergencyContactPhone as string).trim() || null }
      : null;
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
      }),
      switchMap(() => emergencyContactUpdate
        ? this.studentService.updateEmergencyContactPhone(
          emergencyContactUpdate.studentId,
          emergencyContactUpdate.phone
        ).pipe(tap(() => emergencyContactControl?.markAsPristine()))
        : of(null))
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
          titleText: profileSaved ? 'Perfil parcialmente actualizado' : 'No se pudo guardar el perfil',
          text: this.errorMessage(err, profileSaved
            ? 'Los datos personales se guardaron, pero no se pudo guardar el teléfono de emergencia.'
            : 'Ocurrió un error al guardar los datos.'),
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
