import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { UserService, ProfileData } from '../../core/services/user.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  standalone: false
})
export class ProfileComponent implements OnInit {

  profile: ProfileData | null = null;
  isLoadingProfile = true;

  profileForm: FormGroup;
  profileSaving = false;
  profileSuccess = '';
  profileError = '';

  passwordForm: FormGroup;
  passwordSaving = false;
  passwordSuccess = '';
  passwordError = '';

  hideCurrentPassword = true;
  hideNewPassword = true;
  hideConfirmPassword = true;

  maxBirthDate = new Date();

  constructor(
    private readonly fb: FormBuilder,
    private readonly userService: UserService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.profileForm = this.fb.group({
      username:  ['', Validators.required],
      lastName:  ['', Validators.required],
      gender:    [''],
      cuil:      [''],
      birthDate: [null],
      phoneCode: [''],
      phone:     ['']
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
    this.userService.getProfile().subscribe({
      next: (data) => {
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
    this.profileSuccess = '';
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

    this.userService.updateProfile(payload).subscribe({
      next: (res) => {
        this.profileSaving = false;
        this.profileSuccess = 'Datos actualizados correctamente';
        if (this.profile) Object.assign(this.profile, res);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.profileSaving = false;
        this.profileError = err.message || 'Error al guardar los datos';
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
    this.passwordSuccess = '';
    this.passwordError = '';

    const { currentPassword, newPassword } = this.passwordForm.value;

    this.userService.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.passwordSaving = false;
        this.passwordSuccess = 'Contraseña cambiada correctamente';
        this.passwordForm.reset();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.passwordSaving = false;
        this.passwordError = err.message || 'Error al cambiar la contraseña';
        this.cdr.detectChanges();
      }
    });
  }
}
