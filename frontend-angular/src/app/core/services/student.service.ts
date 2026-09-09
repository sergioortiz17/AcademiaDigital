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
  divisionId: number | null;
  divisionName: string | null;
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
  divisionId: number;
  academicYear: number;
  yearNumber: number;
  reason?: string | null;
}

export interface AcademicAssignment {
  id: number;
  studentId: number;
  careerId: number;
  studyPlanId: number;
  divisionId: number | null;
}

/** Una correlativa faltante para una materia (del cálculo real de elegibilidad del backend). */
export interface MissingPrerequisite {
  courseId: number;
  code: string;
  name: string;
  prerequisiteType: string;   // 'Strict' | 'Soft'
  requiredStatus: string;
  currentStatus: string | null;
}

/** Materia del plan con su estado de elegibilidad calculado por el backend
 *  (misma fuente de verdad que la validación real de inscripción). */
export interface EligibleCourse {
  courseId: number;
  studyPlanCourseId: number;
  code: string;
  name: string;
  yearNumber: number;
  semester: number;
  eligibilityStatus: 'Eligible' | 'EligibleWithWarning' | 'BlockedByStrictPrerequisite' | 'AlreadyApproved' | 'AlreadyEnrolled';
  missingPrerequisites: MissingPrerequisite[];
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

  /** Materias del plan del alumno LOGUEADO con su estado de elegibilidad (calculado por el backend,
   *  misma data que la validación real). El backend resuelve el studentId desde el JWT. */
  getMyEligibleCourses(careerId?: number): Observable<EligibleCourse[]> {
    let params = new HttpParams();
    if (careerId != null) params = params.set('careerId', careerId);
    return this.http.get<EligibleCourse[]>(`${this.baseURL}v1/students/me/eligible-courses`, { params });
  }

  /** Historial académico del alumno LOGUEADO: materias del plan con su estado real por materia
   *  (EnrollmentStatus sin agrupar). El backend resuelve el studentId desde el JWT. Solo lectura. */
  getMyAcademicProgress(careerId?: number): Observable<StudentAcademicProgress> {
    let params = new HttpParams();
    if (careerId != null) params = params.set('careerId', careerId);
    return this.http.get<StudentAcademicProgress>(`${this.baseURL}v1/students/me/academic-progress`, { params });
  }
}

export interface StudentCourseProgress {
  courseId: number;
  code: string;
  name: string;
  yearNumber: number;
  semester: number;
  academicStatus: string;
  // Estado real sin agrupar: Enrolled | Regularized | Failed | Approved | Promoted | null
  enrollmentStatus: string | null;
  finalGrade: number | null;
  academicYear: number | null;
}

export interface StudentAcademicProgress {
  studentId: number;
  careerId: number;
  careerName: string;
  studyPlanId: number;
  studyPlanName: string;
  totalCourses: number;
  approvedCourses: number;
  inProgressCourses: number;
  pendingCourses: number;
  progressPercentage: number;
  courses: StudentCourseProgress[];
}
