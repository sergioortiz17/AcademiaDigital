import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { TeachingPositionService, TeachingPosition } from '../../../../core/services/teaching-position.service';

export interface AssignPositionDialogResult {
  teachingPositionId: number;
  startedOn: string;
  reason: string | null;
}

@Component({
  selector: 'app-assign-position-dialog',
  templateUrl: './assign-position-dialog.component.html',
  styleUrls: ['./assign-position-dialog.component.scss'],
  standalone: false
})
export class AssignPositionDialogComponent implements OnInit {
  positions: TeachingPosition[] = [];
  isLoading = false;

  teachingPositionId: number | null = null;
  startedOn: Date = new Date();
  reason = '';

  constructor(
    public dialogRef: MatDialogRef<AssignPositionDialogComponent>,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.teachingPositionService.getTeachingPositions({ isVacant: true }).subscribe({
      next: (positions) => {
        this.positions = positions;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get isValid(): boolean {
    return !!this.teachingPositionId && !!this.startedOn;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    const result: AssignPositionDialogResult = {
      teachingPositionId: this.teachingPositionId!,
      startedOn: this.toIsoDate(this.startedOn),
      reason: this.reason.trim() || null
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
