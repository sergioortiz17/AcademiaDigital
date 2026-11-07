import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { EMPTY, Observable, expand, reduce } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_ALERT } from '../interceptors/error.interceptor';
import { PagedResult } from './student.service';

export interface ExamTable {
  id: number;
  courseId: number;
  courseName: string;
  courseCode: string;
  academicYear: number;
  callNumber: number;
  examDateUtc: string;
  registrationDeadlineUtc: string;
  location: string;
  status: 'Open' | 'Grading' | 'Published';
}
export interface StudentExamTable {
  examTable: ExamTable;
  canRegister: boolean;
  registrationId: number | null;
  attemptNumber: number | null;
  result: { outcome: 'Passed' | 'Failed' | 'Absent'; grade: number | null } | null;
}
export interface AcademicEnrollment {
  enrollmentId: number;
  courseId: number;
  academicYear: number;
  semester: number;
  status: string;
  enrollmentDate: string;
}
export interface ExamRegistration {
  id: number;
  enrollmentId: number;
  studentId: number;
  attemptNumber: number;
  registeredAt: string;
}

@Injectable({ providedIn: 'root' })
export class ExamTableService {
  private readonly base = environment.apiServer;
  constructor(private readonly http: HttpClient) {}

  getMine(): Observable<StudentExamTable[]> {
    return this.http.get<StudentExamTable[]>(`${this.base}v1/exam-tables/me`, {
      context: new HttpContext().set(SKIP_ERROR_ALERT, true)
    });
  }

  getAcademicEnrollments(studentId: number): Observable<AcademicEnrollment[]> {
    const page = (number: number) => this.http.get<PagedResult<AcademicEnrollment>>(
      `${this.base}v1/students/${studentId}/academic-history`, {
        params: new HttpParams().set('page', number).set('pageSize', 100),
        context: new HttpContext().set(SKIP_ERROR_ALERT, true)
      });
    return page(1).pipe(
      expand(result => result.items.length && result.page * result.pageSize < result.total
        ? page(result.page + 1) : EMPTY),
      reduce((all, result) => [...all, ...result.items], [] as AcademicEnrollment[])
    );
  }

  register(tableId: number, enrollmentId: number): Observable<ExamRegistration> {
    return this.http.post<ExamRegistration>(`${this.base}v1/exam-tables/${tableId}/registrations`,
      { enrollmentId }, { context: new HttpContext().set(SKIP_ERROR_ALERT, true) });
  }
}
