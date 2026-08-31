import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CareerService, Career } from '../../core/services/career.service';
import { TeacherService, TeacherAssignment } from '../../core/services/teacher.service';
import {
  AttendanceService,
  AttendanceSession,
  AttendanceRecordStatus,
  SaveAttendanceRecordInput,
  parseDateOnly
} from '../../core/services/attendance.service';
import { NewSessionDialogComponent } from './new-session-dialog/new-session-dialog.component';

interface EditableCell {
  recordId: number | null;
  status: AttendanceRecordStatus | null;
}

interface StudentRow {
  enrollmentId: number;
  studentId: number;
  studentName: string;
  legajoNumber: string;
  dni: string;
  cells: Record<number, EditableCell>;
}

@Component({
  selector: 'app-attendance',
  templateUrl: './attendance.component.html',
  styleUrls: ['./attendance.component.scss'],
  standalone: false
})
export class AttendanceComponent implements OnInit {
  // Carrera/Sede/Año decorativos, consistentes con Calificaciones.
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

  assignments: TeacherAssignment[] = [];
  selectedTeachingPositionId: number | null = null;

  allSessions: AttendanceSession[] = [];
  rows: StudentRow[] = [];
  currentMonth = new Date();
  searchTerm = '';
  selectedEnrollmentId: number | null = null;

  isLoading = false;
  isSaving = false;
  isCreating = false;
  errorMsg = '';
  successMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly teacherService: TeacherService,
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

    this.teacherService.getMyAssignments(false).subscribe({
      next: (assignments) => {
        this.assignments = assignments;
        if (assignments.length === 1) {
          this.selectedTeachingPositionId = assignments[0].teachingPositionId;
          this.onPositionChange();
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudieron cargar tus materias asignadas.';
        this.cdr.detectChanges();
      }
    });
  }

  get selectedAssignment(): TeacherAssignment | undefined {
    return this.assignments.find(a => a.teachingPositionId === this.selectedTeachingPositionId);
  }

  get visibleSessions(): AttendanceSession[] {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();
    return this.allSessions
      .filter(s => {
        const d = parseDateOnly(s.sessionDate);
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

  formatDay(dateStr: string): string {
    const d = parseDateOnly(dateStr);
    return `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')}`;
  }

  monthLabel(): string {
    return this.currentMonth.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
  }

  isSessionEditable(session: AttendanceSession): boolean {
    if (session.status !== 'Open') return false;
    if (session.isAdministrativelyReopened) return true;
    return new Date() <= new Date(session.editDeadlineUtc);
  }

  onPositionChange(): void {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    this.currentMonth = new Date(assignment.academicYear, new Date().getFullYear() === assignment.academicYear ? new Date().getMonth() : 0, 1);
    this.loadSessions();
  }

  loadSessions(): void {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    this.isLoading = true;
    this.errorMsg = '';
    this.attendanceService.getSessions({
      courseId: assignment.courseId,
      commissionId: assignment.commissionId ?? undefined,
      academicYear: assignment.academicYear
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
          row.cells[sessionId] = { recordId: record.id, status: record.status };
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

  openNewSessionDialog(): void {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    const dialogRef = this.dialog.open(NewSessionDialogComponent, {
      width: '480px',
      disableClose: true,
      data: { teachingPositionId: assignment.teachingPositionId, defaultDate: new Date(this.currentMonth) }
    });

    dialogRef.afterClosed().subscribe((request) => {
      if (!request) return;
      this.isCreating = true;
      this.attendanceService.createSession(request).subscribe({
        next: () => {
          this.isCreating = false;
          this.successMsg = 'Día creado correctamente.';
          this.loadSessions();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.isCreating = false;
          this.errorMsg = err.message || 'Error al crear el día.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  closeSession(session: AttendanceSession, event: Event): void {
    event.stopPropagation();
    if (!confirm(`¿Cerrar la sesión del ${this.formatDay(session.sessionDate)}? Ya no se podrá editar salvo reapertura administrativa.`)) return;

    this.attendanceService.closeSession(session.id).subscribe({
      next: () => {
        this.loadSessions();
      },
      error: (err) => {
        this.errorMsg = err.message || 'Error al cerrar la sesión.';
        this.cdr.detectChanges();
      }
    });
  }

  saveAll(): void {
    if (this.isSaving) return;

    const requests = this.visibleSessions
      .filter(session => this.isSessionEditable(session))
      .map(session => {
        const records: SaveAttendanceRecordInput[] = [];
        for (const row of this.rows) {
          const cell = row.cells[session.id];
          if (cell?.status != null) {
            records.push({ enrollmentId: row.enrollmentId, status: cell.status });
          }
        }
        return { session, records };
      })
      .filter(item => item.records.length > 0);

    if (requests.length === 0) {
      this.errorMsg = 'No hay cambios para guardar en este mes.';
      return;
    }

    this.isSaving = true;
    this.errorMsg = '';
    const calls = requests.map(item => this.attendanceService.saveRecords(item.session.id, item.records));

    forkJoin(calls).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMsg = 'Asistencia guardada correctamente.';
        this.loadSessions();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMsg = err.message || 'Error al guardar la asistencia.';
        this.cdr.detectChanges();
      }
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
