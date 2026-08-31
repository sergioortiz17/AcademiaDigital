import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export function parseDateOnly(dateStr: string): Date {
  const [year, month, day] = dateStr.split('-').map(Number);
  return new Date(year, month - 1, day);
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

  exportSession(sessionId: number, format: 'csv' | 'pdf'): Observable<Blob> {
    return this.http.get(`${this.base}v1/attendance/sessions/${sessionId}/export`, {
      params: new HttpParams().set('format', format),
      responseType: 'blob'
    });
  }
}
