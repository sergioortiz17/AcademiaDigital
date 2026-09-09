import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-reopen-session-dialog',
  templateUrl: './reopen-session-dialog.component.html',
  styleUrls: ['./reopen-session-dialog.component.scss'],
  standalone: false
})
export class ReopenSessionDialogComponent {
  reason = '';

  constructor(public dialogRef: MatDialogRef<ReopenSessionDialogComponent>) {}

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
