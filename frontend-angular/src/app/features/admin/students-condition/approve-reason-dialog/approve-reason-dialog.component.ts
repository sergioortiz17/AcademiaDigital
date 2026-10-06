import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface ApproveReasonDialogData {
  count: number;
  courseName: string;
}

@Component({
  selector: 'app-approve-reason-dialog',
  templateUrl: './approve-reason-dialog.component.html',
  styleUrls: ['./approve-reason-dialog.component.scss'],
  standalone: false
})
export class ApproveReasonDialogComponent {
  reason = '';

  constructor(
    public dialogRef: MatDialogRef<ApproveReasonDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ApproveReasonDialogData
  ) {}

  get reasonValid(): boolean {
    return this.reason.trim().length >= 3;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.reasonValid) return;
    this.dialogRef.close(this.reason.trim());
  }
}
