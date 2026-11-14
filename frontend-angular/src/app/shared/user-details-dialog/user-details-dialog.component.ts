import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { StudentService, UpdateStudentRequest } from '../../core/services/student.service';

export interface UserDetailsDialogData {
  studentId: number | null;
  username: string;
  lastName?: string | null;
  email: string;
  dni?: string | null;
  carrera: string;
  legajo: string;
  birthDate: string | null;
  phone: string | null;
  fechaIngreso: string;
  estado: string;
  plan: string;
  addressLine: string | null;
  city: string | null;
  province: string | null;
  postalCode: string | null;
  emergencyContactName: string | null;
  emergencyContactRelationship: string | null;
  emergencyContactPhone: string | null;
}

@Component({
  selector: 'app-user-details-dialog',
  templateUrl: './user-details-dialog.component.html',
  styleUrls: ['./user-details-dialog.component.scss'],
  standalone: false
})
export class UserDetailsDialogComponent {
  isSaving = false;
  errorMsg = '';

  constructor(
    public dialogRef: MatDialogRef<UserDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: UserDetailsDialogData,
    private readonly studentService: StudentService
  ) {}

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    const legajoNumber = this.data.legajo.trim();
    if (this.data.studentId == null || !legajoNumber || legajoNumber.length > 50 || this.isSaving) return;

    const request: UpdateStudentRequest = {
      legajoNumber,
      addressLine: this.data.addressLine,
      city: this.data.city,
      province: this.data.province,
      postalCode: this.data.postalCode,
      emergencyContactName: this.data.emergencyContactName,
      emergencyContactRelationship: this.data.emergencyContactRelationship,
      emergencyContactPhone: this.data.emergencyContactPhone
    };

    this.isSaving = true;
    this.errorMsg = '';
    this.studentService.updateStudent(this.data.studentId, request).subscribe({
      next: () => this.dialogRef.close(true),
      error: err => {
        this.errorMsg = err.error?.msg || err.error?.detail || err.error?.message || err.message || 'No se pudo actualizar el legajo.';
        this.isSaving = false;
      }
    });
  }

}