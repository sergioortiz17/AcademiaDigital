import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface ConfirmDialogData {
  title: string;
  action: string;
  username: string;
  dni?: string;
  role?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  templateUrl: './confirm-dialog.component.html',
  styleUrls: ['./confirm-dialog.component.scss'],
  standalone: false
})
export class ConfirmDialogComponent {

  constructor(
    public dialogRef: MatDialogRef<ConfirmDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData
  ) {}

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  get actionClass(): string {

  switch (this.data.action.toLowerCase()) {

    case 'activar':
      return 'action-activate';

    case 'desactivar':
      return 'action-deactivate';

    case 'modificar':
      return 'action-modify';

    case 'eliminar':
      return 'action-delete';

    default:
      return 'action-default';
  }

}
}