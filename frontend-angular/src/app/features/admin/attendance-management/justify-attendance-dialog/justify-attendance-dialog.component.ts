import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { AttendanceRecord } from '../../../../core/services/attendance.service';

export interface JustifyAttendanceDialogData {
  record: AttendanceRecord;
}

export interface JustifyAttendanceDialogResult {
  category: string;
  reason: string;
  evidenceUrl: string | null;
}

const CATEGORY_OPTIONS = ['Médica', 'Laboral', 'Familiar', 'Institucional', 'Otra'];

@Component({
  selector: 'app-justify-attendance-dialog',
  templateUrl: './justify-attendance-dialog.component.html',
  styleUrls: ['./justify-attendance-dialog.component.scss'],
  standalone: false
})
export class JustifyAttendanceDialogComponent {
  categoryOptions = CATEGORY_OPTIONS;
  category = '';
  reason = '';
  evidenceUrl = '';

  constructor(
    public dialogRef: MatDialogRef<JustifyAttendanceDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: JustifyAttendanceDialogData
  ) {}

  get isEvidenceValid(): boolean {
    const url = this.evidenceUrl.trim();
    if (!url) return true;
    return url.startsWith('https://') || url.startsWith('storage://');
  }

  get isValid(): boolean {
    return !!this.category && this.reason.trim().length >= 3 && this.isEvidenceValid;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    const result: JustifyAttendanceDialogResult = {
      category: this.category,
      reason: this.reason.trim(),
      evidenceUrl: this.evidenceUrl.trim() || null
    };
    this.dialogRef.close(result);
  }
}
