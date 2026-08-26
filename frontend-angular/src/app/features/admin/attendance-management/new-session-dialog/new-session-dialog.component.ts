import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { AttendanceScope, CreateAttendanceSessionRequest } from '../../../../core/services/attendance.service';
import { TeachingPosition } from '../../../../core/services/teaching-position.service';

export interface NewSessionDialogData {
  positions: TeachingPosition[];
}

@Component({
  selector: 'app-new-session-dialog',
  templateUrl: './new-session-dialog.component.html',
  styleUrls: ['./new-session-dialog.component.scss'],
  standalone: false
})
export class NewSessionDialogComponent {
  positions: TeachingPosition[] = [];

  teachingPositionId: number | null = null;
  sessionDate: Date = new Date();
  scope: AttendanceScope = 'ClassHour';
  startTime = '';
  endTime = '';
  units = 1;

  maxDate = new Date();

  constructor(
    public dialogRef: MatDialogRef<NewSessionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: NewSessionDialogData
  ) {
    this.positions = data.positions;
  }

  get isValid(): boolean {
    if (!this.teachingPositionId || !this.sessionDate) return false;
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
      teachingPositionId: this.teachingPositionId!,
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
