import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import {
  TeachingPositionService,
  TeachingPosition,
  SaveTeachingPositionRequest
} from '../../../../core/services/teaching-position.service';
import { TeachingPositionFormDialogComponent } from '../../teaching-position-management/teaching-position-form-dialog/teaching-position-form-dialog.component';

export interface AssignPositionDialogResult {
  courseSectionId: number;
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
  isCreating = false;
  createErrorMsg = '';

  courseSectionId: number | null = null;
  startedOn: Date = new Date();
  reason = '';

  constructor(
    public dialogRef: MatDialogRef<AssignPositionDialogComponent>,
    private readonly dialog: MatDialog,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadVacantPositions();
  }

  private loadVacantPositions(): void {
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

  createNewPosition(): void {
    this.createErrorMsg = '';
    const dialogRef = this.dialog.open(TeachingPositionFormDialogComponent, {
      width: '520px',
      disableClose: true,
      data: { position: null }
    });

    dialogRef.afterClosed().subscribe((request: SaveTeachingPositionRequest | null) => {
      if (!request) return;
      this.isCreating = true;
      this.teachingPositionService.createTeachingPosition(request).subscribe({
        next: (created) => {
          this.isCreating = false;
          this.positions = [...this.positions, created];
          this.courseSectionId = created.id;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.isCreating = false;
          this.createErrorMsg = err.message || 'Error al crear la comisión.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  get isValid(): boolean {
    return !!this.courseSectionId && !!this.startedOn;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    const result: AssignPositionDialogResult = {
      courseSectionId: this.courseSectionId!,
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
