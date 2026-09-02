import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export function parseDateOnly(dateStr: string): Date {
  const [year, month, day] = dateStr.split('-').map(Number);
  return new Date(year, month - 1, day);
}

const ATTENDANCE_ERROR_TRANSLATIONS: Record<string, string> = {
  'The teacher is not assigned to this course and commission.':
    'No estás asignado a esta materia/comisión para la fecha seleccionada.',
  'The teacher cannot manage this attendance session.':
    'No tenés permisos para gestionar esta sesión de asistencia.',
  'An attendance session already exists for this course, commission and time.':
    'Ya existe un día cargado para esa fecha y ese horario.',
  'The idempotency key was already used with a different attendance session.':
    'Ocurrió un conflicto al crear el día. Intentá nuevamente.',
  'Attendance sessions cannot be created in the future.':
    'No se pueden crear días de asistencia con fecha futura.',
  'Session date must belong to the teaching position academic year.':
    'La fecha debe pertenecer al año académico de la materia.',
  'Attendance units must be between 1 and 12.':
    'Las unidades deben estar entre 1 y 12.',
  'Class-hour attendance requires a valid start and end time.':
    'Para "Hora cátedra" completá la hora de inicio y de fin.',
  'Full-day attendance does not accept times and always uses one unit.':
    '"Día completo" no admite horarios.',
  'The attendance session is closed.':
    'La sesión está cerrada.',
  'The 48-hour attendance edit window has expired.':
    'Venció el plazo de 48 horas para editar esta asistencia.',
  'The attendance session is already closed.':
    'La sesión ya está cerrada.',
  'Only a closed attendance session can be reopened.':
    'Solo se puede reabrir una sesión cerrada.',
  'A reopening reason of at least three characters is required.':
    'El motivo de reapertura debe tener al menos 3 caracteres.',
  'Only an absence or late arrival can be justified.':
    'Solo se puede justificar una ausencia o una tardanza.',
  'Attendance requires an active teaching position with a commission.':
    'La materia no tiene una comisión activa asignada.'
};

export function translateAttendanceError(message: string | undefined | null, fallback: string): string {
  if (!message) return fallback;
  return ATTENDANCE_ERROR_TRANSLATIONS[message] ?? message;
}

export type AttendanceScope = 'ClassHour' | 'FullDay';
export type AttendanceSessionStatus = 'Open' | 'Closed';
export type AttendanceRecordStatus = 'Present' | 'Late' | 'Absent' | 'Justified';

export interface AttendanceSession {
  id: number;
  idempotencyKey: string;
  teachingPositionId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  commissionId: number;
  commissionCode: string;
  commissionName: string;
  academicYear: number;
  semester: number;
  sessionDate: string;
  startTime: string | null;
  endTime: string | null;
  scope: AttendanceScope;
  units: number;
  status: AttendanceSessionStatus;
  editDeadlineUtc: string;
  isAdministrativelyReopened: boolean;
  recordCount: number;
  reopeningCount: number;
  createdAt: string;
  createdByUserId: number;
  closedAt: string | null;
  closedByUserId: number | null;
}

export interface AttendanceJustification {
  id: number;
  category: string;
  reason: string;
  evidenceUrl: string | null;
  createdAt: string;
  createdByUserId: number;
}

export interface AttendanceRecord {
  id: number | null;
  enrollmentId: number;
  studentId: number;
  studentName: string;
  legajoNumber: string;
  dni: string;
  status: AttendanceRecordStatus | null;
  notes: string | null;
  updatedAt: string | null;
  justification: AttendanceJustification | null;
}

export interface AttendanceSessionDetail {
  session: AttendanceSession;
  records: AttendanceRecord[];
}

export interface AttendanceSummaryItem {
  courseId: number;
  courseCode: string;
  courseName: string;
  commissionId: number;
  commissionCode: string;
  commissionName: string;
  academicYear: number;
  semester: number;
  minimumAttendancePercentage: number | null;
  earnedUnits: number;
  possibleUnits: number;
  attendancePercentage: number | null;
  isAtRisk: boolean;
  presentCount: number;
  lateCount: number;
  absentCount: number;
  justifiedCount: number;
}

export interface StudentAttendanceSummary {
  studentId: number;
  studentName: string;
  legajoNumber: string;
  items: AttendanceSummaryItem[];
}

export interface AttendanceSessionFilters {
  academicYear?: number;
  courseId?: number;
  commissionId?: number;
}

export interface CreateAttendanceSessionRequest {
  teachingPositionId: number;
  sessionDate: string;
  startTime?: string | null;
  endTime?: string | null;
  scope: AttendanceScope;
  units?: number;
}

export interface SaveAttendanceRecordInput {
  enrollmentId: number;
  status: AttendanceRecordStatus;
  notes?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getSessions(filters: AttendanceSessionFilters = {}): Observable<AttendanceSession[]> {
    let params = new HttpParams();
    if (filters.academicYear != null) params = params.set('academicYear', filters.academicYear);
    if (filters.courseId != null) params = params.set('courseId', filters.courseId);
    if (filters.commissionId != null) params = params.set('commissionId', filters.commissionId);
    return this.http.get<AttendanceSession[]>(`${this.base}v1/attendance/sessions`, { params });
  }

  getSession(id: number): Observable<AttendanceSessionDetail> {
    return this.http.get<AttendanceSessionDetail>(`${this.base}v1/attendance/sessions/${id}`);
  }

  createSession(request: CreateAttendanceSessionRequest): Observable<AttendanceSession> {
    const idempotencyKey = crypto.randomUUID();
    return this.http.post<AttendanceSession>(
      `${this.base}v1/attendance/sessions`,
      request,
      { headers: { 'Idempotency-Key': idempotencyKey } }
    );
  }

  saveRecords(sessionId: number, records: SaveAttendanceRecordInput[]): Observable<AttendanceSessionDetail> {
    return this.http.put<AttendanceSessionDetail>(
      `${this.base}v1/attendance/sessions/${sessionId}/records`,
      { records }
    );
  }

  closeSession(sessionId: number): Observable<AttendanceSession> {
    return this.http.post<AttendanceSession>(`${this.base}v1/attendance/sessions/${sessionId}/close`, {});
  }

  reopenSession(sessionId: number, reason: string): Observable<AttendanceSession> {
    return this.http.post<AttendanceSession>(`${this.base}v1/attendance/sessions/${sessionId}/reopen`, { reason });
  }

  justifyRecord(
    recordId: number,
    category: string,
    reason: string,
    evidenceUrl?: string | null
  ): Observable<AttendanceJustification> {
    return this.http.post<AttendanceJustification>(
      `${this.base}v1/attendance/records/${recordId}/justifications`,
      { category, reason, evidenceUrl: evidenceUrl || null }
    );
  }

  getMySummary(): Observable<StudentAttendanceSummary> {
    return this.http.get<StudentAttendanceSummary>(`${this.base}v1/attendance/me/summary`);
  }

  exportSession(sessionId: number, format: 'csv' | 'pdf'): Observable<Blob> {
    return this.http.get(`${this.base}v1/attendance/sessions/${sessionId}/export`, {
      params: new HttpParams().set('format', format),
      responseType: 'blob'
    });
  }
}
