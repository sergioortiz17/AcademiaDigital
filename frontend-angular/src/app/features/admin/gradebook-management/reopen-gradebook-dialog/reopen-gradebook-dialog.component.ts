import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-reopen-gradebook-dialog',
  templateUrl: './reopen-gradebook-dialog.component.html',
  styleUrls: ['./reopen-gradebook-dialog.component.scss'],
  standalone: false
})
export class ReopenGradebookDialogComponent {
  reason = '';

  constructor(public dialogRef: MatDialogRef<ReopenGradebookDialogComponent>) {}

  get isValid(): boolean {
    return this.reason.trim().length >= 3;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    this.dialogRef.close(this.reason.trim());
  }
}
