import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import {
  AttendanceService,
  AttendanceSessionDetail,
  AttendanceRecord,
  AttendanceRecordStatus
} from '../../../core/services/attendance.service';
import { ReopenSessionDialogComponent } from '../attendance-management/reopen-session-dialog/reopen-session-dialog.component';
import {
  JustifyAttendanceDialogComponent,
  JustifyAttendanceDialogResult
} from '../attendance-management/justify-attendance-dialog/justify-attendance-dialog.component';

interface EditableRecord extends AttendanceRecord {
  localStatus: AttendanceRecordStatus;
  localNotes: string;
}

@Component({
  selector: 'app-attendance-session-detail',
  templateUrl: './attendance-session-detail.component.html',
  styleUrls: ['./attendance-session-detail.component.scss'],
  standalone: false
})
export class AttendanceSessionDetailComponent implements OnInit {
  sessionId!: number;
  detail: AttendanceSessionDetail | null = null;
  rows: EditableRecord[] = [];

  isLoading = false;
  isSaving = false;
  errorMsg = '';
  successMsg = '';
  processingRecordId: number | null = null;

  statusOptions: { value: AttendanceRecordStatus; label: string }[] = [
    { value: 'Present', label: 'Presente' },
    { value: 'Late', label: 'Tarde' },
    { value: 'Absent', label: 'Ausente' }
  ];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly attendanceService: AttendanceService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.sessionId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadDetail();
  }

  get canEdit(): boolean {
    if (!this.detail) return false;
    const session = this.detail.session;
    if (session.status !== 'Open') return false;
    if (session.isAdministrativelyReopened) return true;
    return new Date() <= new Date(session.editDeadlineUtc);
  }

  loadDetail(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.attendanceService.getSession(this.sessionId).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.rows = detail.records.map(record => ({
          ...record,
          localStatus: record.status ?? 'Present',
          localNotes: record.notes ?? ''
        }));
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudo cargar la sesión.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/app/admin/attendance']);
  }

  saveRecords(): void {
    if (!this.canEdit || this.isSaving) return;
    this.isSaving = true;
    this.errorMsg = '';

    const records = this.rows.map(row => ({
      enrollmentId: row.enrollmentId,
      status: row.localStatus,
      notes: row.localNotes.trim() || null
    }));

    this.attendanceService.saveRecords(this.sessionId, records).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.rows = detail.records.map(record => ({
          ...record,
          localStatus: record.status ?? 'Present',
          localNotes: record.notes ?? ''
        }));
        this.isSaving = false;
        this.successMsg = 'Asistencia guardada correctamente.';
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMsg = err.message || 'Error al guardar la asistencia.';
        this.cdr.detectChanges();
      }
    });
  }

  closeSession(): void {
    if (!this.detail || this.detail.session.status !== 'Open') return;
    if (!confirm('¿Cerrar esta sesión? Ya no se podrán editar los registros salvo reapertura administrativa.')) return;

    this.attendanceService.closeSession(this.sessionId).subscribe({
      next: () => {
        this.loadDetail();
      },
      error: (err) => {
        this.errorMsg = err.message || 'Error al cerrar la sesión.';
        this.cdr.detectChanges();
      }
    });
  }

  reopenSession(): void {
    if (!this.detail || this.detail.session.status !== 'Closed') return;

    const dialogRef = this.dialog.open(ReopenSessionDialogComponent, { width: '450px', disableClose: true });
    dialogRef.afterClosed().subscribe((reason: string | null) => {
      if (!reason) return;
      this.attendanceService.reopenSession(this.sessionId, reason).subscribe({
        next: () => this.loadDetail(),
        error: (err) => {
          this.errorMsg = err.message || 'Error al reabrir la sesión.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  canJustify(row: EditableRecord): boolean {
    return row.id != null
      && (row.status === 'Absent' || row.status === 'Late')
      && !row.justification;
  }

  justify(row: EditableRecord): void {
    if (!this.canJustify(row) || this.processingRecordId != null) return;

    const dialogRef = this.dialog.open(JustifyAttendanceDialogComponent, {
      width: '450px',
      disableClose: true,
      data: { record: row }
    });

    dialogRef.afterClosed().subscribe((result: JustifyAttendanceDialogResult | null) => {
      if (!result) return;
      this.processingRecordId = row.id;
      this.attendanceService.justifyRecord(row.id!, result.category, result.reason, result.evidenceUrl).subscribe({
        next: () => {
          this.processingRecordId = null;
          this.loadDetail();
        },
        error: (err) => {
          this.processingRecordId = null;
          this.errorMsg = err.message || 'Error al justificar la inasistencia.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  exportSession(format: 'csv' | 'pdf'): void {
    this.attendanceService.exportSession(this.sessionId, format).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `asistencia_sesion_${this.sessionId}.${format}`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.errorMsg = 'Error al exportar la sesión.';
        this.cdr.detectChanges();
      }
    });
  }
}
