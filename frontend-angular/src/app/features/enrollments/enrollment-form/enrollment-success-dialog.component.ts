import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-enrollment-success-dialog',
  standalone: false,
  template: `
    <div class="success-dialog">
      <div class="success-icon">
        <mat-icon>check_circle</mat-icon>
      </div>
      <h2 mat-dialog-title>¡Inscripción realizada!</h2>
      <mat-dialog-content>
        <p>Tu solicitud de inscripción fue enviada correctamente.</p>
        <p class="sub">Recibirás una confirmación por correo electrónico con los detalles de tu inscripción.</p>
      </mat-dialog-content>
      <mat-dialog-actions align="center">
        <button mat-raised-button color="primary" (click)="close()">Aceptar</button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .success-dialog {
      text-align: center;
      padding: 12px 8px 4px;
    }
    .success-icon mat-icon {
      font-size: 72px;
      height: 72px;
      width: 72px;
      color: #2e7d32;
    }
    h2 {
      margin: 12px 0 0;
      font-size: 1.5rem;
      font-weight: 700;
      color: #1b5e20;
    }
    p {
      margin: 8px 0 0;
      font-size: 0.97rem;
      color: #333;
    }
    .sub {
      margin-top: 8px;
      font-size: 0.85rem;
      color: #666;
    }
    mat-dialog-actions {
      margin-top: 20px;
      padding-bottom: 8px;
    }
    button {
      min-width: 120px;
      border-radius: 999px;
    }
  `]
})
export class EnrollmentSuccessDialogComponent {
  constructor(private readonly dialogRef: MatDialogRef<EnrollmentSuccessDialogComponent>) {}

  close(): void {
    this.dialogRef.close();
  }
}
