import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Student {
  id: number;
  userId: number;
  userEmail: string;
  userName: string;
  careerId: number;
  careerName: string;
  legajoNumber: string;
  enrollmentDate: string;
  status: string;
  currentStudyPlanId: number;
  currentStudyPlanName: string;
}

export interface StudentListItem {
  id: number;
  userId: number;
  dni: string | null;
  fullName: string;
  legajoNumber: string;
  status: string;
  careerId: number;
  careerName: string;
  academicYear: number | null;
  yearNumber: number | null;
  commissionId: number | null;
  commissionName: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

/** Debe coincidir con CreateAcademicAssignmentRequest del backend. */
export interface CreateAcademicAssignmentRequest {
  careerId: number;
  studyPlanId: number;
  commissionId: number;
  academicYear: number;
  yearNumber: number;
  reason?: string | null;
}

export interface AcademicAssignment {
  id: number;
  studentId: number;
  careerId: number;
  studyPlanId: number;
  commissionId: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class StudentService {

  private baseURL = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getStudentByUserId(studentId: number): Observable<Student> {
    return this.http.get<Student>(
      `${this.baseURL}v1/students/${studentId}`
    );
  }

  /** Ficha completa del alumno (incluye currentStudyPlanId, necesario para asignar comisión). */
  getStudent(studentId: number): Observable<Student> {
    return this.http.get<Student>(`${this.baseURL}v1/students/${studentId}`);
  }

  /** Listado paginado de alumnos (admin). Sirve para el selector de "Asignar comisión". */
  searchStudents(search?: string, careerId?: number, page = 1, pageSize = 50): Observable<PagedResult<StudentListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (careerId != null) params = params.set('careerId', careerId);
    return this.http.get<PagedResult<StudentListItem>>(`${this.baseURL}v1/students`, { params });
  }

  /** Crea el vínculo alumno-comisión (StudentAcademicAssignment). Backend valida carrera y deduplica. */
  assignAcademic(studentId: number, request: CreateAcademicAssignmentRequest): Observable<AcademicAssignment> {
    return this.http.post<AcademicAssignment>(`${this.baseURL}v1/students/${studentId}/academic-assignments`, request);
  }
}
