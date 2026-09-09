import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface RejectCertificateDialogData {
  username?: string;
  certificateType: string;
}

@Component({
  selector: 'app-reject-certificate-dialog',
  templateUrl: './reject-certificate-dialog.component.html',
  styleUrls: ['./reject-certificate-dialog.component.scss'],
  standalone: false
})
export class RejectCertificateDialogComponent {
  reason = '';

  constructor(
    public dialogRef: MatDialogRef<RejectCertificateDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RejectCertificateDialogData
  ) {}

  get isValid(): boolean {
    return this.reason.trim().length >= 3;
  }

  confirm(): void {
    if (!this.isValid) return;
    this.dialogRef.close(this.reason.trim());
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
