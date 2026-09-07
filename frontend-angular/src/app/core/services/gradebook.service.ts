import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type GradebookStatus = 'Draft' | 'Submitted' | 'Approved' | 'Published' | 'Closed';

export interface GradebookEvaluation {
  id: number;
  name: string;
  weightPercentage: number;
  maximumScore: number;
  displayOrder: number;
  isRecovery: boolean;
}

export interface GradeEntry {
  revisionId: number | null;
  evaluationId: number;
  score: number | null;
  version: number | null;
  notes: string | null;
  updatedAt: string | null;
}

export interface GradebookStudent {
  enrollmentId: number;
  studentId: number;
  studentName: string;
  legajoNumber: string;
  dni: string;
  grades: GradeEntry[];
  average: number | null;
  resultStatus: string | null;
}

export interface Gradebook {
  id: number;
  idempotencyKey: string;
  courseSectionId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  divisionId: number;
  divisionCode: string;
  divisionName: string;
  academicYear: number;
  semester: number;
  status: GradebookStatus;
  evaluationCount: number;
  currentGradeCount: number;
  reopeningCount: number;
  createdAt: string;
  submittedAt: string | null;
  approvedAt: string | null;
  publishedAt: string | null;
  closedAt: string | null;
}

export interface GradebookDetail {
  gradebook: Gradebook;
  evaluations: GradebookEvaluation[];
  students: GradebookStudent[];
}

export interface StudentPublishedGradebook {
  gradebookId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  academicYear: number;
  semester: number;
  status: GradebookStatus;
  evaluations: GradebookEvaluation[];
  grades: GradeEntry[];
  average: number;
  resultStatus: string;
  publishedAt: string;
}

export interface GradebookFilters {
  academicYear?: number;
  courseId?: number;
  divisionId?: number;
}

export interface CreateGradebookEvaluationInput {
  name: string;
  weightPercentage: number;
  maximumScore: number;
  isRecovery: boolean;
}

export interface SaveGradeEntryInput {
  evaluationId: number;
  enrollmentId: number;
  score: number;
  notes?: string | null;
}

@Injectable({ providedIn: 'root' })
export class GradebookService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getGradebooks(filters: GradebookFilters = {}): Observable<Gradebook[]> {
    let params = new HttpParams();
    if (filters.academicYear != null) params = params.set('academicYear', filters.academicYear);
    if (filters.courseId != null) params = params.set('courseId', filters.courseId);
    if (filters.divisionId != null) params = params.set('divisionId', filters.divisionId);
    return this.http.get<Gradebook[]>(`${this.base}v1/gradebooks`, { params });
  }

  getGradebook(id: number): Observable<GradebookDetail> {
    return this.http.get<GradebookDetail>(`${this.base}v1/gradebooks/${id}`);
  }

  createGradebook(courseSectionId: number, evaluations: CreateGradebookEvaluationInput[]): Observable<Gradebook> {
    return this.http.post<Gradebook>(
      `${this.base}v1/gradebooks`,
      { courseSectionId, evaluations },
      { headers: { 'Idempotency-Key': crypto.randomUUID() } }
    );
  }

  saveGrades(gradebookId: number, grades: SaveGradeEntryInput[]): Observable<GradebookDetail> {
    return this.http.put<GradebookDetail>(`${this.base}v1/gradebooks/${gradebookId}/grades`, { grades });
  }

  submitGradebook(id: number): Observable<Gradebook> {
    return this.http.post<Gradebook>(`${this.base}v1/gradebooks/${id}/submit`, {});
  }

  approveGradebook(id: number): Observable<Gradebook> {
    return this.http.post<Gradebook>(`${this.base}v1/gradebooks/${id}/approve`, {});
  }

  publishGradebook(id: number): Observable<Gradebook> {
    return this.http.post<Gradebook>(`${this.base}v1/gradebooks/${id}/publish`, {});
  }

  closeGradebook(id: number): Observable<Gradebook> {
    return this.http.post<Gradebook>(`${this.base}v1/gradebooks/${id}/close`, {});
  }

  reopenGradebook(id: number, reason: string): Observable<Gradebook> {
    return this.http.post<Gradebook>(`${this.base}v1/gradebooks/${id}/reopen`, { reason });
  }

  /**
   * Atajo administrativo: marca una materia (inscripción) como aprobada por fuera del circuito
   * formal de planilla + mesa. Fija la nota final y el estado, registrando auditoría en el backend.
   */
  adminApproveEnrollment(enrollmentId: number, finalGrade: number, reason: string, promote = false): Observable<unknown> {
    return this.http.post<unknown>(
      `${this.base}v1/enrollments/${enrollmentId}/admin-approve`,
      { finalGrade, reason, promote }
    );
  }

  getMyGrades(courseId?: number): Observable<StudentPublishedGradebook[]> {
    let params = new HttpParams();
    if (courseId != null) params = params.set('courseId', courseId);
    return this.http.get<StudentPublishedGradebook[]>(`${this.base}v1/gradebooks/me`, { params });
  }
}
