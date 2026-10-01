import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TeacherAssignment } from '../../../../core/services/teacher.service';

export interface EndAssignmentDialogData {
  assignment: TeacherAssignment;
}

export interface EndAssignmentDialogResult {
  endedOn: string;
  reason: string;
}

@Component({
  selector: 'app-end-assignment-dialog',
  templateUrl: './end-assignment-dialog.component.html',
  styleUrls: ['./end-assignment-dialog.component.scss'],
  standalone: false
})
export class EndAssignmentDialogComponent {
  endedOn: Date = new Date();
  reason = '';
  minDate: Date;

  constructor(
    public dialogRef: MatDialogRef<EndAssignmentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EndAssignmentDialogData
  ) {
    this.minDate = new Date(data.assignment.startedOn);
  }

  get isValid(): boolean {
    return !!this.endedOn && this.endedOn >= this.minDate && this.reason.trim().length >= 3;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    const result: EndAssignmentDialogResult = {
      endedOn: this.toIsoDate(this.endedOn),
      reason: this.reason.trim()
    };
    this.dialogRef.close(result);
  }

  private toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
