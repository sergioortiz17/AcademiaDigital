import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EnrollmentPeriodDto {
  id: number;
  careerId: number;
  careerName: string;
  studyPlanId: number;
  studyPlanName: string;
  academicYear: number;
  semester: number;
  quotasMorning: number;
  quotasAfternoon: number;
  quotasEvening: number;
  enrolledMorning: number;
  enrolledAfternoon: number;
  enrolledEvening: number;
  isActive: boolean;
  startDate: string;
  endDate: string | null;
}

export interface EnrolledStudentDto {
  studentId: number;
  fullName: string;
  dni: string;
  shift: string | null;
  enrollmentDate: string;
  courseNames: string[];
}

export interface OpenPeriodRequest {
  careerId: number;
  studyPlanId: number;
  academicYear: number;
  semester: number;
  quotasMorning: number;
  quotasAfternoon: number;
  quotasEvening: number;
}

export interface CreateEnrollmentRequest {
  enrollmentPeriodId: number;
  shift: string;
  studyPlanCourseIds: number[];
}

@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private readonly baseURL = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getAllPeriods(): Observable<{ success: boolean; data: EnrollmentPeriodDto[] }> {
    return this.http.get<{ success: boolean; data: EnrollmentPeriodDto[] }>(
      `${this.baseURL}v1/enrollments/periods`
    );
  }

  getActivePeriod(careerId: number): Observable<{ success: boolean; data: EnrollmentPeriodDto | null }> {
    return this.http.get<{ success: boolean; data: EnrollmentPeriodDto | null }>(
      `${this.baseURL}v1/enrollments/periods/active?careerId=${careerId}`
    );
  }

  getEnrolledStudents(periodId: number): Observable<{ success: boolean; total: number; data: EnrolledStudentDto[] }> {
    return this.http.get<{ success: boolean; total: number; data: EnrolledStudentDto[] }>(
      `${this.baseURL}v1/enrollments/periods/${periodId}/students`
    );
  }

  openPeriod(request: OpenPeriodRequest): Observable<{ success: boolean; data: EnrollmentPeriodDto }> {
    return this.http.post<{ success: boolean; data: EnrollmentPeriodDto }>(
      `${this.baseURL}v1/enrollments/periods`,
      request
    );
  }

  updateQuotas(periodId: number, quotasMorning: number, quotasAfternoon: number, quotasEvening: number): Observable<{ success: boolean; data: EnrollmentPeriodDto }> {
    return this.http.put<{ success: boolean; data: EnrollmentPeriodDto }>(
      `${this.baseURL}v1/enrollments/periods/${periodId}/quotas`,
      { quotasMorning, quotasAfternoon, quotasEvening }
    );
  }

  closePeriod(periodId: number): Observable<{ success: boolean; msg: string }> {
    return this.http.put<{ success: boolean; msg: string }>(
      `${this.baseURL}v1/enrollments/periods/${periodId}/close`,
      {}
    );
  }

  removeStudentFromPeriod(periodId: number, studentId: number): Observable<{ success: boolean; msg: string }> {
    return this.http.delete<{ success: boolean; msg: string }>(
      `${this.baseURL}v1/enrollments/periods/${periodId}/students/${studentId}`
    );
  }

  enroll(request: CreateEnrollmentRequest): Observable<{ success: boolean; msg: string }> {
    return this.http.post<{ success: boolean; msg: string }>(
      `${this.baseURL}v1/enrollments`,
      request
    );
  }
}
