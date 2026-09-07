import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface AdminApproveDialogData {
  studentName: string;
  courseName: string;
  // Nota final sugerida: el promedio que el sistema ya calculó para el alumno. Se precarga
  // en el campo (editable) para no obligar al admin a re-tipear una nota que ya existe.
  initialGrade?: number | null;
}

export interface AdminApproveDialogResult {
  finalGrade: number;
  reason: string;
}

@Component({
  selector: 'app-admin-approve-dialog',
  templateUrl: './admin-approve-dialog.component.html',
  styleUrls: ['./admin-approve-dialog.component.scss'],
  standalone: false
})
export class AdminApproveDialogComponent {
  finalGrade: number | null = null;
  reason = '';

  constructor(
    public dialogRef: MatDialogRef<AdminApproveDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AdminApproveDialogData
  ) {
    // Precargar la nota calculada por el sistema (si vino), dejándola editable.
    this.finalGrade = data.initialGrade ?? null;
  }

  get gradeValid(): boolean {
    return this.finalGrade != null && this.finalGrade >= 1 && this.finalGrade <= 10;
  }

  get reasonValid(): boolean {
    return this.reason.trim().length >= 3;
  }

  get isValid(): boolean {
    return this.gradeValid && this.reasonValid;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    const result: AdminApproveDialogResult = {
      finalGrade: this.finalGrade!,
      reason: this.reason.trim()
    };
    this.dialogRef.close(result);
  }
}
