import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TeachingPositionService, TeachingPosition, SaveTeachingPositionRequest } from '../../../core/services/teaching-position.service';
import { TeachingPositionFormDialogComponent } from './teaching-position-form-dialog/teaching-position-form-dialog.component';

@Component({
  selector: 'app-teaching-position-management',
  templateUrl: './teaching-position-management.component.html',
  styleUrls: ['./teaching-position-management.component.scss'],
  standalone: false
})
export class TeachingPositionManagementComponent implements OnInit {
  positions: TeachingPosition[] = [];
  onlyVacant = false;
  includeInactive = false;

  isLoading = false;
  errorMsg = '';
  successMsg = '';

  displayedColumns = ['course', 'year', 'type', 'maxStudents', 'teacher', 'status', 'actions'];

  constructor(
    private readonly teachingPositionService: TeachingPositionService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPositions();
  }

  loadPositions(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.teachingPositionService.getTeachingPositions({
      isVacant: this.onlyVacant ? true : undefined,
      includeInactive: this.includeInactive
    }).subscribe({
      next: (positions) => {
        this.positions = positions;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'Error al cargar los cargos.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleOnlyVacant(): void {
    this.onlyVacant = !this.onlyVacant;
    this.loadPositions();
  }

  toggleIncludeInactive(): void {
    this.includeInactive = !this.includeInactive;
    this.loadPositions();
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(TeachingPositionFormDialogComponent, {
      width: '520px',
      disableClose: true,
      data: { position: null }
    });

    dialogRef.afterClosed().subscribe((request: SaveTeachingPositionRequest | null) => {
      if (!request) return;
      this.teachingPositionService.createTeachingPosition(request).subscribe({
        next: () => {
          this.successMsg = 'Cargo creado correctamente.';
          this.loadPositions();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.errorMsg = err.error?.message || err.error?.title || 'Error al crear el cargo.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  openEditDialog(position: TeachingPosition): void {
    const dialogRef = this.dialog.open(TeachingPositionFormDialogComponent, {
      width: '520px',
      disableClose: true,
      data: { position }
    });

    dialogRef.afterClosed().subscribe((request: SaveTeachingPositionRequest | null) => {
      if (!request) return;
      this.teachingPositionService.updateTeachingPosition(position.id, request).subscribe({
        next: () => {
          this.successMsg = 'Cargo actualizado correctamente.';
          this.loadPositions();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.errorMsg = err.error?.message || 'Error al actualizar el cargo.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  deactivatePosition(position: TeachingPosition): void {
    if (!confirm(`¿Dar de baja el cargo de ${position.courseName} — ${position.commissionName}?`)) return;
    let reason = prompt('Motivo de la baja (mínimo 3 caracteres):');
    if (reason === null) return;
    reason = reason.trim();
    if (reason.length < 3) {
      alert('El motivo debe tener al menos 3 caracteres.');
      return;
    }

    this.teachingPositionService.deactivateTeachingPosition(position.id, reason).subscribe({
      next: () => {
        this.loadPositions();
      },
      error: (err) => {
        this.errorMsg = err.error?.message || 'Error al dar de baja el cargo.';
        this.cdr.detectChanges();
      }
    });
  }
}
