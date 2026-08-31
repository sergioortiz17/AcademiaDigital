import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CareerService, Career } from '../../../core/services/career.service';
import { TeachingPositionService, TeachingPosition } from '../../../core/services/teaching-position.service';
import {
  AttendanceService,
  AttendanceSession,
  AttendanceRecordStatus,
  AttendanceJustification
} from '../../../core/services/attendance.service';
import { ReopenSessionDialogComponent } from './reopen-session-dialog/reopen-session-dialog.component';
import { JustifyAttendanceDialogComponent, JustifyAttendanceDialogResult } from './justify-attendance-dialog/justify-attendance-dialog.component';

interface ReadonlyCell {
  recordId: number | null;
  status: AttendanceRecordStatus | null;
  justification: AttendanceJustification | null;
}

interface StudentRow {
  enrollmentId: number;
  studentId: number;
  studentName: string;
  legajoNumber: string;
  dni: string;
  cells: Record<number, ReadonlyCell>;
}

@Component({
  selector: 'app-attendance-management',
  templateUrl: './attendance-management.component.html',
  styleUrls: ['./attendance-management.component.scss'],
  standalone: false
})
export class AttendanceManagementComponent implements OnInit {
  careers: Career[] = [];
  selectedCareerId: number | null = null;
  sedes = ['Sede Centro', 'Sede Norte'];
  selectedSede: string | null = null;
  planYears = [
    { value: 1, label: 'Primero' },
    { value: 2, label: 'Segundo' },
    { value: 3, label: 'Tercero' }
  ];
  selectedPlanYear: number | null = null;

  positions: TeachingPosition[] = [];
  selectedTeachingPositionId: number | null = null;

  allSessions: AttendanceSession[] = [];
  rows: StudentRow[] = [];
  currentMonth = new Date();
  searchTerm = '';
  selectedEnrollmentId: number | null = null;

  isLoading = false;
  isProcessing = false;
  errorMsg = '';
  successMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly attendanceService: AttendanceService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe(careers => {
      this.careers = careers;
      this.selectedCareerId = careers[0]?.id ?? null;
      this.cdr.detectChanges();
    });

    this.teachingPositionService.getTeachingPositions({ includeInactive: false }).subscribe({
      next: (positions) => {
        this.positions = positions;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudieron cargar los cargos docentes.';
        this.cdr.detectChanges();
      }
    });
  }

  get selectedPosition(): TeachingPosition | undefined {
    return this.positions.find(p => p.id === this.selectedTeachingPositionId);
  }

  get visibleSessions(): AttendanceSession[] {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();
    return this.allSessions
      .filter(s => {
        const d = new Date(s.sessionDate);
        return d.getFullYear() === year && d.getMonth() === month;
      })
      .sort((a, b) => a.sessionDate.localeCompare(b.sessionDate));
  }

  get filteredRows(): StudentRow[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.rows;
    return this.rows.filter(r => r.studentName.toLowerCase().includes(term));
  }

  get selectedRow(): StudentRow | null {
    return this.rows.find(r => r.enrollmentId === this.selectedEnrollmentId) ?? null;
  }

  get justifiableRecordForSelected(): { sessionId: number; cell: ReadonlyCell } | null {
    const row = this.selectedRow;
    if (!row) return null;
    const candidates = this.allSessions
      .filter(s => {
        const cell = row.cells[s.id];
        return cell?.recordId != null && !cell.justification && (cell.status === 'Absent' || cell.status === 'Late');
      })
      .sort((a, b) => b.sessionDate.localeCompare(a.sessionDate));
    if (candidates.length === 0) return null;
    return { sessionId: candidates[0].id, cell: row.cells[candidates[0].id] };
  }

  formatDay(dateStr: string): string {
    const d = new Date(dateStr);
    return `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')}`;
  }

  monthLabel(): string {
    return this.currentMonth.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
  }

  onPositionChange(): void {
    const position = this.selectedPosition;
    if (!position) return;

    this.currentMonth = new Date(position.academicYear, new Date().getFullYear() === position.academicYear ? new Date().getMonth() : 0, 1);
    this.loadSessions();
  }

  loadSessions(): void {
    const position = this.selectedPosition;
    if (!position) return;

    this.isLoading = true;
    this.errorMsg = '';
    this.attendanceService.getSessions({
      courseId: position.courseId,
      commissionId: position.commissionId ?? undefined,
      academicYear: position.academicYear
    }).subscribe({
      next: (sessions) => {
        this.allSessions = sessions;
        this.loadAllDetails();
      },
      error: (err) => {
        this.errorMsg = err.message || 'Error al cargar las sesiones.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadAllDetails(): void {
    if (this.allSessions.length === 0) {
      this.rows = [];
      this.isLoading = false;
      this.cdr.detectChanges();
      return;
    }

    const calls = this.allSessions.map(session =>
      this.attendanceService.getSession(session.id).pipe(catchError(() => of(null)))
    );

    forkJoin(calls).subscribe(details => {
      const rowsByEnrollment = new Map<number, StudentRow>();

      details.forEach((detail, index) => {
        if (!detail) return;
        const sessionId = this.allSessions[index].id;
        for (const record of detail.records) {
          let row = rowsByEnrollment.get(record.enrollmentId);
          if (!row) {
            row = {
              enrollmentId: record.enrollmentId,
              studentId: record.studentId,
              studentName: record.studentName,
              legajoNumber: record.legajoNumber,
              dni: record.dni,
              cells: {}
            };
            rowsByEnrollment.set(record.enrollmentId, row);
          }
          row.cells[sessionId] = { recordId: record.id, status: record.status, justification: record.justification };
        }
      });

      this.rows = [...rowsByEnrollment.values()].sort((a, b) => a.studentName.localeCompare(b.studentName));
      this.selectedEnrollmentId = this.rows[0]?.enrollmentId ?? null;
      this.isLoading = false;
      this.cdr.detectChanges();
    });
  }

  prevMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() - 1, 1);
  }

  nextMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + 1, 1);
  }

  selectStudent(row: StudentRow): void {
    this.selectedEnrollmentId = row.enrollmentId;
  }

  attendancePercentage(row: StudentRow): number | null {
    let earned = 0;
    let possible = 0;
    for (const session of this.allSessions) {
      const cell = row.cells[session.id];
      if (!cell || cell.status == null || cell.status === 'Justified') continue;
      possible += session.units;
      if (cell.status === 'Present') earned += session.units;
      else if (cell.status === 'Late') earned += session.units * 0.5;
    }
    if (possible === 0) return null;
    return Math.round((earned / possible) * 100);
  }

  reopenSession(session: AttendanceSession, event: Event): void {
    event.stopPropagation();
    if (this.isProcessing) return;

    const dialogRef = this.dialog.open(ReopenSessionDialogComponent, { width: '450px', disableClose: true });
    dialogRef.afterClosed().subscribe((reason: string | null) => {
      if (!reason) return;
      this.isProcessing = true;
      this.attendanceService.reopenSession(session.id, reason).subscribe({
        next: () => {
          this.isProcessing = false;
          this.loadSessions();
        },
        error: (err) => {
          this.isProcessing = false;
          this.errorMsg = err.message || 'Error al reabrir la sesión.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  justifySelected(): void {
    const candidate = this.justifiableRecordForSelected;
    if (!candidate || !this.selectedRow) return;

    const dialogRef = this.dialog.open(JustifyAttendanceDialogComponent, {
      width: '450px',
      disableClose: true,
      data: {
        record: {
          id: candidate.cell.recordId,
          enrollmentId: this.selectedRow.enrollmentId,
          studentId: this.selectedRow.studentId,
          studentName: this.selectedRow.studentName,
          legajoNumber: this.selectedRow.legajoNumber,
          dni: this.selectedRow.dni,
          status: candidate.cell.status,
          notes: null,
          updatedAt: null,
          justification: null
        }
      }
    });

    dialogRef.afterClosed().subscribe((result: JustifyAttendanceDialogResult | null) => {
      if (!result) return;
      this.isProcessing = true;
      this.attendanceService.justifyRecord(candidate.cell.recordId!, result.category, result.reason, result.evidenceUrl).subscribe({
        next: () => {
          this.isProcessing = false;
          this.successMsg = 'Ausencia justificada correctamente.';
          this.loadSessions();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.isProcessing = false;
          this.errorMsg = err.message || 'Error al justificar la inasistencia.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  downloadSummary(): void {
    const header = ['Alumno', 'Legajo', ...this.visibleSessions.map(s => this.formatDay(s.sessionDate)), 'Promedio Asistencia'];
    const lines = [header.join(',')];

    for (const row of this.filteredRows) {
      const cells = this.visibleSessions.map(s => row.cells[s.id]?.status ?? '');
      lines.push([row.studentName, row.legajoNumber, ...cells, `${this.attendancePercentage(row) ?? ''}%`].join(','));
    }

    const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `asistencia_${this.currentMonth.getFullYear()}-${this.currentMonth.getMonth() + 1}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
