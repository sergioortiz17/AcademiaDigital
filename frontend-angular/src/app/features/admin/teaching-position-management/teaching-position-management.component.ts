import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TeachingPositionService, TeachingPosition, SaveTeachingPositionRequest } from '../../../core/services/teaching-position.service';
import { GradebookService, SectionStudent } from '../../../core/services/gradebook.service';
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

  displayedColumns = ['expand', 'course', 'year', 'type', 'maxStudents', 'teacher', 'status', 'actions'];

  /** Comisión desplegada y alumnos ya cargados por comisión (se piden al desplegar). */
  expandedId: number | null = null;
  studentsBySection: Record<number, SectionStudent[] | 'loading' | 'error'> = {};

  constructor(
    private readonly teachingPositionService: TeachingPositionService,
    private readonly gradebookService: GradebookService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  /** Despliega o comprime la fila; la primera vez trae los alumnos inscriptos en la comisión. */
  toggleStudents(position: TeachingPosition): void {
    this.expandedId = this.expandedId === position.id ? null : position.id;
    this.cdr.markForCheck();
    if (this.expandedId == null || Array.isArray(this.studentsBySection[position.id])) return;
    this.studentsBySection[position.id] = 'loading';
    this.gradebookService.getSectionStudents(position.id).subscribe({
      next: students => {
        this.studentsBySection[position.id] = [...students].sort((a, b) => a.studentName.localeCompare(b.studentName, 'es'));
        this.cdr.detectChanges();
      },
      error: () => {
        this.studentsBySection[position.id] = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  studentsOf(position: TeachingPosition): SectionStudent[] {
    const value = this.studentsBySection[position.id];
    return Array.isArray(value) ? value : [];
  }

  studentsState(position: TeachingPosition): 'loading' | 'error' | 'ready' {
    const value = this.studentsBySection[position.id];
    return Array.isArray(value) ? 'ready' : (value ?? 'loading');
  }

  ngOnInit(): void {
    this.loadPositions();
  }

  loadPositions(): void {
    this.studentsBySection = {};
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
        this.errorMsg = 'Error al cargar las comisiones.';
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
          this.successMsg = 'Comisión creada correctamente.';
          this.loadPositions();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.errorMsg = err.message || 'Error al crear la comisión.';
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
          this.successMsg = 'Comisión actualizada correctamente.';
          this.loadPositions();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.errorMsg = err.message || 'Error al actualizar la comisión.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  deactivatePosition(position: TeachingPosition): void {
    if (!confirm(`¿Dar de baja la comisión de ${position.courseName} — ${position.divisionName}?`)) return;
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
        this.errorMsg = err.message || 'Error al dar de baja la comisión.';
        this.cdr.detectChanges();
      }
    });
  }
}
