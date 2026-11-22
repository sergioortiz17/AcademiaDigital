import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { AttendanceScope, CreateAttendanceSessionRequest } from '../../../core/services/attendance.service';

export interface NewSessionDialogData {
  courseSectionId: number;
  defaultDate?: Date;
  /** Inicio de la asignación del docente (AAAA-MM-DD): no se cargan días anteriores. */
  assignmentStartedOn?: string;
}

@Component({
  selector: 'app-new-session-dialog',
  templateUrl: './new-session-dialog.component.html',
  styleUrls: ['./new-session-dialog.component.scss'],
  standalone: false
})
export class NewSessionDialogComponent {
  sessionDate: Date;
  scope: AttendanceScope = 'ClassHour';
  startTime = '';
  endTime = '';
  units = 1;

  maxDate: Date;
  minDate: Date;
  private readonly assignmentStart: Date | null;

  constructor(
    public dialogRef: MatDialogRef<NewSessionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: NewSessionDialogData
  ) {
    const today = startOfDay(new Date());
    this.maxDate = today;
    const twoDaysAgo = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 2);
    this.assignmentStart = data.assignmentStartedOn ? startOfDay(new Date(data.assignmentStartedOn + 'T00:00:00')) : null;
    this.minDate = this.assignmentStart && this.assignmentStart > twoDaysAgo ? this.assignmentStart : twoDaysAgo;
    this.sessionDate = data.defaultDate ?? today;
  }

  /** Mismo control que el selector, pero también para fechas escritas a mano. */
  get dateError(): string | null {
    if (!this.sessionDate || isNaN(this.sessionDate.getTime())) return 'Ingrese una fecha válida.';
    const day = startOfDay(this.sessionDate);
    if (day > this.maxDate) return 'No se puede cargar asistencia de días futuros.';
    if (this.assignmentStart && day < this.assignmentStart)
      return `Su asignación empieza el ${this.assignmentStart.toLocaleDateString('es-AR')}: no puede cargar días anteriores.`;
    if (day < this.minDate) return 'Solo se puede cargar asistencia hasta 48 horas después de la clase.';
    return null;
  }

  get isValid(): boolean {
    if (this.dateError) return false;
    if (this.scope === 'ClassHour') {
      return !!this.startTime && !!this.endTime && this.startTime < this.endTime;
    }
    return true;
  }

  onScopeChange(): void {
    if (this.scope === 'FullDay') {
      this.startTime = '';
      this.endTime = '';
      this.units = 1;
    }
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;

    const request: CreateAttendanceSessionRequest = {
      courseSectionId: this.data.courseSectionId,
      sessionDate: this.toDateOnly(this.sessionDate),
      scope: this.scope,
      units: this.scope === 'ClassHour' ? this.units : 1,
      startTime: this.scope === 'ClassHour' ? `${this.startTime}:00` : null,
      endTime: this.scope === 'ClassHour' ? `${this.endTime}:00` : null
    };
    this.dialogRef.close(request);
  }

  private toDateOnly(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}
