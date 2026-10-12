import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MaterialModule } from '../../shared/material.module';

export interface GradebookActionDialogData {
  confirmation: string;
  confirmLabel: string;
  reasonPrompt?: string;
}

@Component({
  selector: 'app-gradebook-action-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  template: `
    <h2 mat-dialog-title>Confirmar acción</h2>
    <mat-dialog-content>
      <p>{{ data.confirmation }}</p>
      <mat-form-field *ngIf="data.reasonPrompt" appearance="outline" class="reason-field">
        <mat-label>{{ data.reasonPrompt }}</mat-label>
        <textarea matInput rows="3" maxlength="1000" [(ngModel)]="reason"></textarea>
        <mat-hint *ngIf="reason.trim().length < 3">El motivo debe tener al menos 3 caracteres.</mat-hint>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">Cancelar</button>
      <button mat-flat-button color="primary" type="button" [disabled]="!canConfirm" (click)="confirm()">
        {{ data.confirmLabel }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    :host { display: block; }
    mat-dialog-content { min-width: min(360px, 70vw); }
    .reason-field { width: 100%; margin-top: 8px; }
  `]
})
export class GradebookActionDialogComponent {
  reason = '';

  constructor(
    public readonly dialogRef: MatDialogRef<GradebookActionDialogComponent, string | true | null>,
    @Inject(MAT_DIALOG_DATA) public readonly data: GradebookActionDialogData
  ) {}

  get canConfirm(): boolean {
    return !this.data.reasonPrompt || this.reason.trim().length >= 3;
  }

  confirm(): void {
    if (!this.canConfirm) return;
    this.dialogRef.close(this.data.reasonPrompt ? this.reason.trim() : true);
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}