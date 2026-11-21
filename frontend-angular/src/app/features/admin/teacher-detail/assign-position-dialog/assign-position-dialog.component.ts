import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { AssignCoursesOutcome } from '../../assign-courses-form/assign-courses-form.component';

export interface AssignPositionDialogData {
  teacherId: number;
}

/** Resultado al cerrar: cuántas materias se asignaron (0 si se cerró sin asignar). */
export interface AssignPositionDialogResult {
  assigned: number;
}

/** Diálogo del legajo docente: envuelve el formulario "Asignar materias". */
@Component({
  selector: 'app-assign-position-dialog',
  templateUrl: './assign-position-dialog.component.html',
  styleUrls: ['./assign-position-dialog.component.scss'],
  standalone: false
})
export class AssignPositionDialogComponent {
  private assigned = 0;

  constructor(
    public dialogRef: MatDialogRef<AssignPositionDialogComponent, AssignPositionDialogResult>,
    @Inject(MAT_DIALOG_DATA) public data: AssignPositionDialogData
  ) {}

  onSubmitted(outcome: AssignCoursesOutcome): void {
    this.assigned += outcome.assigned;
    // Si todo salió bien se cierra; si alguna materia falló queda abierto para ver el motivo.
    if (!outcome.hasFailures) this.close();
  }

  close(): void {
    this.dialogRef.close({ assigned: this.assigned });
  }
}
