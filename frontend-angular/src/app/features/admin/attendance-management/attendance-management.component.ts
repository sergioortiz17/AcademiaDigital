import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { AttendanceService, AttendanceSession } from '../../../core/services/attendance.service';
import { TeachingPositionService, TeachingPosition } from '../../../core/services/teaching-position.service';
import { NewSessionDialogComponent } from './new-session-dialog/new-session-dialog.component';
import { ReopenSessionDialogComponent } from './reopen-session-dialog/reopen-session-dialog.component';

@Component({
  selector: 'app-attendance-management',
  templateUrl: './attendance-management.component.html',
  styleUrls: ['./attendance-management.component.scss'],
  standalone: false
})
export class AttendanceManagementComponent implements OnInit {
  positions: TeachingPosition[] = [];
  sessions: AttendanceSession[] = [];

  selectedPositionId: number | null = null;

  isLoading = false;
  isCreating = false;
  errorMsg = '';
  successMsg = '';
  processingIds = new Set<number>();

  displayedColumns = ['course', 'date', 'time', 'scope', 'status', 'records', 'actions'];

  constructor(
    private readonly attendanceService: AttendanceService,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly dialog: MatDialog,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.teachingPositionService.getTeachingPositions({ includeInactive: false }).subscribe({
      next: (positions) => {
        this.positions = positions;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'No se pudieron cargar los cargos docentes.';
        this.cdr.detectChanges();
      }
    });

    this.loadSessions();
  }

  get selectedPosition(): TeachingPosition | undefined {
    return this.positions.find(p => p.id === this.selectedPositionId);
  }

  onPositionFilterChange(): void {
    this.loadSessions();
  }

  loadSessions(): void {
    if (this.isLoading) return;
    this.isLoading = true;
    this.errorMsg = '';

    const position = this.selectedPosition;
    const filters = position
      ? { courseId: position.courseId, commissionId: position.commissionId ?? undefined, academicYear: position.academicYear }
      : {};

    this.attendanceService.getSessions(filters).subscribe({
      next: (sessions) => {
        this.sessions = [...sessions].sort((a, b) => b.sessionDate.localeCompare(a.sessionDate));
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'Error al cargar las sesiones de asistencia.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openNewSessionDialog(): void {
    if (this.positions.length === 0) {
      this.errorMsg = 'No hay cargos docentes activos para crear una sesión.';
      return;
    }

    const dialogRef = this.dialog.open(NewSessionDialogComponent, {
      width: '480px',
      disableClose: true,
      data: { positions: this.positions }
    });

    dialogRef.afterClosed().subscribe((request) => {
      if (!request) return;

      this.isCreating = true;
      this.attendanceService.createSession(request).subscribe({
        next: () => {
          this.isCreating = false;
          this.successMsg = 'Sesión creada correctamente.';
          this.loadSessions();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.isCreating = false;
          this.errorMsg = err.error?.msg || err.error?.title || 'Error al crear la sesión.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  viewSession(session: AttendanceSession): void {
    this.router.navigate(['/app/admin/attendance', session.id]);
  }

  closeSession(session: AttendanceSession, event: Event): void {
    event.stopPropagation();
    if (this.processingIds.has(session.id)) return;
    if (!confirm(`¿Cerrar la sesión del ${session.sessionDate}? Ya no se podrán editar los registros salvo reapertura administrativa.`)) return;

    this.processingIds.add(session.id);
    this.attendanceService.closeSession(session.id).subscribe({
      next: (updated) => {
        this.replaceSession(updated);
        this.processingIds.delete(session.id);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.processingIds.delete(session.id);
        this.errorMsg = err.error?.msg || 'Error al cerrar la sesión.';
        this.cdr.detectChanges();
      }
    });
  }

  reopenSession(session: AttendanceSession, event: Event): void {
    event.stopPropagation();
    if (this.processingIds.has(session.id)) return;

    const dialogRef = this.dialog.open(ReopenSessionDialogComponent, { width: '450px', disableClose: true });

    dialogRef.afterClosed().subscribe((reason: string | null) => {
      if (!reason) return;

      this.processingIds.add(session.id);
      this.attendanceService.reopenSession(session.id, reason).subscribe({
        next: (updated) => {
          this.replaceSession(updated);
          this.processingIds.delete(session.id);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.processingIds.delete(session.id);
          this.errorMsg = err.error?.msg || 'Error al reabrir la sesión.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  exportSession(session: AttendanceSession, format: 'csv' | 'pdf', event: Event): void {
    event.stopPropagation();
    this.attendanceService.exportSession(session.id, format).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `asistencia_sesion_${session.id}.${format}`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.errorMsg = 'Error al exportar la sesión.';
        this.cdr.detectChanges();
      }
    });
  }

  private replaceSession(updated: AttendanceSession): void {
    this.sessions = this.sessions.map(s => s.id === updated.id ? updated : s);
  }
}
