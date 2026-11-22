import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import * as ExcelJS from 'exceljs';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CareerService, Career } from '../../core/services/career.service';
import { TeacherService, TeacherAssignment } from '../../core/services/teacher.service';
import {
  AttendanceService,
  AttendanceSession,
  AttendanceRecordStatus,
  SaveAttendanceRecordInput,
  parseDateOnly,
  translateAttendanceError
} from '../../core/services/attendance.service';
import { NewSessionDialogComponent } from './new-session-dialog/new-session-dialog.component';
import { GradebookService, SectionStudent } from '../../core/services/gradebook.service';
import { courseDisplayName, mostFrequentId } from '../../core/utils/academic-labels';

interface EditableCell {
  recordId: number | null;
  status: AttendanceRecordStatus | null;
  /** Estado guardado en el servidor, para saber qué cambió. */
  saved: AttendanceRecordStatus | null;
}

/** Orden al tocar una celda: vacía → Presente → Ausente → Tarde → vacía. */
const CYCLE: (AttendanceRecordStatus | null)[] = [null, 'Present', 'Absent', 'Late'];
const MONTHS = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto',
  'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
const WEEKDAYS = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];

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
  // La carrera filtra las materias del docente; al entrar queda elegida la de sus comisiones.
  careers: Career[] = [];
  selectedCareerId: number | null = null;

  assignments: TeacherAssignment[] = [];
  selectedTeachingPositionId: number | null = null;

  allSessions: AttendanceSession[] = [];
  /** Nómina de la comisión: los alumnos se ven aunque todavía no haya días cargados. */
  roster: SectionStudent[] = [];
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
    private readonly gradebookService: GradebookService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    forkJoin({
      careers: this.careerService.getCareers().pipe(catchError(() => of([] as Career[]))),
      assignments: this.teacherService.getMyAssignments(false)
    }).subscribe({
      next: ({ careers, assignments }) => {
        this.careers = careers;
        this.assignments = assignments;
        // Carrera del docente (la de la mayoría de sus comisiones); si no se sabe, la primera.
        this.selectedCareerId = mostFrequentId(assignments.map(a => a.careerId)) ?? careers[0]?.id ?? null;
        this.selectFirstAssignment();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.showError(err.message || 'No se pudieron cargar tus materias asignadas.');
      }
    });
  }

  /** Materias del docente en la carrera elegida (las comisiones sin carrera se muestran siempre). */
  get careerAssignments(): TeacherAssignment[] {
    return this.assignments
      .filter(a => this.selectedCareerId == null || a.careerId == null || a.careerId === this.selectedCareerId)
      // Anuales y 1.er cuatrimestre primero; dentro de cada período, por nombre.
      .sort((a, b) => ((a.isAnnual ? 0 : a.semester) - (b.isAnnual ? 0 : b.semester))
        || this.assignmentLabel(a).localeCompare(this.assignmentLabel(b), 'es'));
  }

  assignmentLabel(a: TeacherAssignment): string {
    const division = a.divisionName ? ` — ${a.divisionName}` : '';
    return `${courseDisplayName(a.courseName, a.semester, a.isAnnual)}${division}`;
  }

  onCareerChange(): void {
    if (!this.careerAssignments.some(a => a.courseSectionId === this.selectedTeachingPositionId)) {
      this.selectedTeachingPositionId = null;
      this.allSessions = [];
      this.rows = [];
      this.selectFirstAssignment();
    }
  }

  /** Al entrar el docente ya ve su primera materia, sin tener que elegirla. */
  private selectFirstAssignment(): void {
    const first = this.careerAssignments[0];
    if (!first || this.selectedTeachingPositionId != null) return;
    this.selectedTeachingPositionId = first.courseSectionId;
    this.onPositionChange();
  }

  private showError(message: string): void {
    this.errorMsg = message;
    this.cdr.detectChanges();
    setTimeout(() => { this.errorMsg = ''; this.cdr.detectChanges(); }, 6000);
  }

  get selectedAssignment(): TeacherAssignment | undefined {
    return this.assignments.find(a => a.courseSectionId === this.selectedTeachingPositionId);
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

  /** Día de la semana de la columna (Lun, Mar...). */
  weekday(dateStr: string): string {
    return WEEKDAYS[parseDateOnly(dateStr).getDay()];
  }

  monthLabel(): string {
    return `${MONTHS[this.currentMonth.getMonth()]} ${this.currentMonth.getFullYear()}`;
  }

  /** Letra que se muestra en la celda. */
  statusLetter(status: AttendanceRecordStatus | null): string {
    switch (status) {
      case 'Present': return 'P';
      case 'Absent': return 'A';
      case 'Late': return 'T';
      case 'Justified': return 'J';
      default: return '';
    }
  }

  statusTitle(status: AttendanceRecordStatus | null): string {
    switch (status) {
      case 'Present': return 'Presente (tocar para cambiar)';
      case 'Absent': return 'Ausente (tocar para cambiar)';
      case 'Late': return 'Tarde (tocar para cambiar)';
      case 'Justified': return 'Justificada';
      default: return 'Sin cargar (tocar para marcar presente)';
    }
  }

  /** Un toque rota el estado: vacía → P → A → T → vacía. Las justificadas no se tocan. */
  cycle(row: StudentRow, session: AttendanceSession): void {
    if (!this.isSessionEditable(session)) return;
    const cell = this.cellFor(row, session);
    if (cell.status === 'Justified') return;
    // Una celda ya guardada no vuelve a vacía (el servidor no borra registros): rota entre P, A y T.
    const options = cell.saved != null ? CYCLE.filter(status => status != null) : CYCLE;
    cell.status = options[(options.indexOf(cell.status) + 1) % options.length];
    this.selectStudent(row);
  }

  /** Marca presentes a todos los que todavía no tienen estado en ese día. */
  markAllPresent(session: AttendanceSession, event?: Event): void {
    event?.stopPropagation();
    if (!this.isSessionEditable(session)) return;
    for (const row of this.rows) {
      const cell = this.cellFor(row, session);
      if (cell.status == null) cell.status = 'Present';
    }
    this.cdr.markForCheck();
  }

  private cellFor(row: StudentRow, session: AttendanceSession): EditableCell {
    return row.cells[session.id] ??= { recordId: null, status: null, saved: null };
  }

  /** Celdas modificadas y sin guardar (en cualquier mes). */
  get pendingChanges(): number {
    let count = 0;
    for (const row of this.rows)
      for (const cell of Object.values(row.cells))
        if (cell.status !== cell.saved) count++;
    return count;
  }

  /** "Hoy" crea el día de clase de hoy con un toque (día completo). */
  get todayExists(): boolean {
    const today = this.todayDateOnly();
    return this.allSessions.some(s => s.sessionDate.slice(0, 10) === today);
  }

  addToday(): void {
    const assignment = this.selectedAssignment;
    if (!assignment || this.isCreating) return;
    if (this.todayExists) { this.showError('El día de hoy ya está cargado.'); return; }
    this.isCreating = true;
    this.attendanceService.createSession({
      courseSectionId: assignment.courseSectionId,
      sessionDate: this.todayDateOnly(),
      scope: 'FullDay',
      units: 1
    }).subscribe({
      next: () => {
        this.isCreating = false;
        this.currentMonth = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
        this.successMsg = 'Día de hoy agregado. Toque cada celda para marcar P, A o T.';
        this.loadSessions();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.isCreating = false;
        this.showError(translateAttendanceError(err.message, 'Error al crear el día.'));
      }
    });
  }

  private todayDateOnly(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
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
    // Por comisión: la misma materia en otro cuatrimestre o curso tiene su propia asistencia.
    forkJoin({
      sessions: this.attendanceService.getSessions({
        courseSectionId: assignment.courseSectionId,
        academicYear: assignment.academicYear
      }),
      roster: this.gradebookService.getSectionStudents(assignment.courseSectionId)
        .pipe(catchError(() => of([] as SectionStudent[])))
    }).subscribe({
      next: ({ sessions, roster }) => {
        this.allSessions = sessions;
        this.roster = roster;
        this.loadAllDetails();
      },
      error: (err) => {
        this.isLoading = false;
        this.showError(translateAttendanceError(err.message, 'Error al cargar las sesiones.'));
      }
    });
  }

  private loadAllDetails(): void {
    if (this.allSessions.length === 0) {
      this.rows = this.rosterRows();
      this.selectedEnrollmentId = this.rows[0]?.enrollmentId ?? null;
      this.isLoading = false;
      this.cdr.detectChanges();
      return;
    }

    const calls = this.allSessions.map(session =>
      this.attendanceService.getSession(session.id).pipe(catchError(() => of(null)))
    );

    forkJoin(calls).subscribe(details => {
      const rowsByEnrollment = new Map<number, StudentRow>(this.rosterRows().map(row => [row.enrollmentId, row]));

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
          row.cells[sessionId] = { recordId: record.id, status: record.status, saved: record.status };
        }
      });

      this.rows = [...rowsByEnrollment.values()].sort((a, b) => a.studentName.localeCompare(b.studentName));
      this.selectedEnrollmentId = this.rows[0]?.enrollmentId ?? null;
      this.isLoading = false;
      this.cdr.detectChanges();
    });
  }

  private rosterRows(): StudentRow[] {
    return this.roster
      .map(student => ({ ...student, cells: {} as Record<number, EditableCell> }))
      .sort((a, b) => a.studentName.localeCompare(b.studentName));
  }

  prevMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() - 1, 1);
  }

  nextMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + 1, 1);
  }

  selectStudent(row: StudentRow): void {
    this.selectedEnrollmentId = row.enrollmentId;
    this.cdr.markForCheck();
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
      data: { courseSectionId: assignment.courseSectionId, assignmentStartedOn: assignment.startedOn }
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
          this.showError(translateAttendanceError(err.message, 'Error al crear el día.'));
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
        this.showError(translateAttendanceError(err.message, 'Error al cerrar la sesión.'));
      }
    });
  }

  saveAll(): void {
    if (this.isSaving) return;

    // Se guardan los días con cambios de cualquier mes (no solo el que está a la vista).
    const requests = this.allSessions
      .filter(session => this.isSessionEditable(session))
      .map(session => {
        const records: SaveAttendanceRecordInput[] = [];
        for (const row of this.rows) {
          const cell = row.cells[session.id];
          if (cell?.status != null && cell.status !== 'Justified' && cell.status !== cell.saved) {
            records.push({ enrollmentId: row.enrollmentId, status: cell.status });
          }
        }
        return { session, records };
      })
      .filter(item => item.records.length > 0);

    if (requests.length === 0) {
      this.showError('No hay cambios para guardar.');
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
        this.showError(translateAttendanceError(err.message, 'Error al guardar la asistencia.'));
      }
    });
  }

  async downloadSummary(): Promise<void> {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    try {
      const workbook = new ExcelJS.Workbook();
      workbook.creator = 'Academia Digital';
      const worksheet = workbook.addWorksheet('Asistencia');
      const firstDayColumn = 8;
      const headerRowNumber = 12;
      const attendanceSessions = [...this.allSessions].sort((first, second) =>
        first.sessionDate.localeCompare(second.sessionDate)
        || (first.startTime ?? '').localeCompare(second.startTime ?? '')
      );
      const lastColumn = Math.max(12, firstDayColumn + attendanceSessions.length - 1);

      worksheet.mergeCells(1, 1, 1, lastColumn);
      const title = worksheet.getCell(1, 1);
      title.value = 'Planilla de asistencia';
      title.font = { name: 'Arial', size: 16, bold: true, color: { argb: 'FFFFFFFF' } };
      title.alignment = { horizontal: 'center', vertical: 'middle' };
      for (let column = 1; column <= lastColumn; column++) {
        worksheet.getCell(1, column).fill = {
          type: 'pattern',
          pattern: 'solid',
          fgColor: { argb: 'FF287DB2' }
        };
      }
      worksheet.getRow(1).height = 32;

      worksheet.mergeCells(2, 1, 2, lastColumn);
      worksheet.getCell(2, 1).value = 'P: Presente | T: Tardanza | A: Ausente | J: Justificada';
      worksheet.getCell(2, 1).font = { name: 'Arial', size: 10, italic: true, color: { argb: 'FF536575' } };

      const selectedCareer = this.careers.find(career => career.id === this.selectedCareerId);
      const courseName = [assignment.divisionName, assignment.divisionCode]
        .filter(value => !!value?.trim())
        .join(' ');
      const metadataRows: Array<[string, string | number]> = [
        ['Planilla de', 'Asistencia'],
        ['Año', assignment.academicYear],
        ['Curso', courseName],
        ['Materia', assignment.courseName],
        ['Carrera', selectedCareer?.name ?? ''],
        ['Turno', ''],
        ['Escuela', ''],
        ['Instituto', 'Instituto Técnico Superior Córdoba']
      ];

      metadataRows.forEach(([label, value], rowIndex) => {
        const rowNumber = rowIndex + 3;
        const labelCell = worksheet.getCell(rowNumber, 1);
        labelCell.value = label;
        worksheet.mergeCells(rowNumber, 2, rowNumber, lastColumn);
        const valueCell = worksheet.getCell(rowNumber, 2);
        valueCell.value = value;

        for (let column = 1; column <= lastColumn; column++) {
          const cell = worksheet.getCell(rowNumber, column);
          cell.border = {
            top: { style: 'thin', color: { argb: 'FF9AA7B1' } },
            left: { style: 'thin', color: { argb: 'FF9AA7B1' } },
            bottom: { style: 'thin', color: { argb: 'FF9AA7B1' } },
            right: { style: 'thin', color: { argb: 'FF9AA7B1' } }
          };
          cell.alignment = { vertical: 'middle', wrapText: true };
        }
        labelCell.font = { name: 'Arial', size: 10, bold: true, color: { argb: 'FF20384E' } };
        valueCell.font = { name: 'Arial', size: 10, color: { argb: 'FF20384E' } };
      });

      const headers = [
        'N°', 'Cohorte', 'Legajo', 'Apellido/s', 'Nombre/s', 'DNI', 'E-mail',
        ...attendanceSessions.map(session => {
          const dateParts = session.sessionDate.split('-');
          const date = `${dateParts[2]}/${dateParts[1]}`;
          const time = session.startTime?.slice(0, 5);
          return time ? `${date}\n${time}` : date;
        })
      ];
      const header = worksheet.getRow(headerRowNumber);
      headers.forEach((value, index) => {
        header.getCell(index + 1).value = value;
      });
      header.height = 32;
      header.eachCell({ includeEmpty: true }, cell => {
        cell.font = { name: 'Arial', size: 10, bold: true, color: { argb: 'FF20384E' } };
        cell.fill = {
          type: 'pattern',
          pattern: 'solid',
          fgColor: { argb: 'FFDCEAF4' }
        };
        cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
        cell.border = {
          top: { style: 'thin', color: { argb: 'FF647789' } },
          left: { style: 'thin', color: { argb: 'FF647789' } },
          bottom: { style: 'thin', color: { argb: 'FF647789' } },
          right: { style: 'thin', color: { argb: 'FF647789' } }
        };
      });

      [7, 12, 16, 22, 22, 14, 28].forEach((width, index) => {
        worksheet.getColumn(index + 1).width = width;
      });
      for (let column = firstDayColumn; column < firstDayColumn + attendanceSessions.length; column++) {
        worksheet.getColumn(column).width = 14;
      }

      const students = [...this.filteredRows]
        .sort((first, second) => first.studentName.localeCompare(second.studentName, 'es'));
      const statusCodes: Record<AttendanceRecordStatus, string> = {
        Present: 'P',
        Late: 'T',
        Absent: 'A',
        Justified: 'J'
      };
      students.forEach((student, index) => {
        const nameParts = student.studentName.trim().split(/\s+/).filter(Boolean);
        const surname = nameParts.length > 1 ? nameParts.pop() ?? '' : '';
        const givenNames = nameParts.join(' ') || student.studentName;

        const values: (string | number)[] = [
          index + 1,
          '',
          student.legajoNumber,
          surname,
          givenNames,
          student.dni,
          '',
          ...attendanceSessions.map(session => {
            const status = student.cells[session.id]?.status;
            return status ? statusCodes[status] : '';
          })
        ];
        const excelRow = worksheet.addRow(values);
        excelRow.height = 24;
        excelRow.eachCell({ includeEmpty: true }, cell => {
          cell.font = { name: 'Arial', size: 10, color: { argb: 'FF20384E' } };
          cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
          cell.border = {
            top: { style: 'thin', color: { argb: 'FF9AA7B1' } },
            left: { style: 'thin', color: { argb: 'FF9AA7B1' } },
            bottom: { style: 'thin', color: { argb: 'FF9AA7B1' } },
            right: { style: 'thin', color: { argb: 'FF9AA7B1' } }
          };
        });
      });

      worksheet.autoFilter = {
        from: `A${headerRowNumber}`,
        to: `${worksheet.getColumn(lastColumn).letter}${Math.max(headerRowNumber, headerRowNumber + students.length)}`
      };
      worksheet.views = [{ state: 'frozen', xSplit: 7, ySplit: headerRowNumber }];
      worksheet.pageSetup = { orientation: 'landscape', fitToPage: true, fitToWidth: 1, fitToHeight: 0 };

      const buffer = await workbook.xlsx.writeBuffer();
      const blob = new Blob([buffer as BlobPart], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      const month = String(this.currentMonth.getMonth() + 1).padStart(2, '0');
      const fileName = `asistencia_${assignment.courseName}_${assignment.divisionName}_${this.currentMonth.getFullYear()}-${month}`
        .replace(/[<>:"/\\|?*\u0000-\u001F]/g, '_');
      link.download = `${fileName}.xlsx`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.showError(error instanceof Error ? error.message : 'No se pudo generar el resumen de asistencia.');
    }
  }
}
