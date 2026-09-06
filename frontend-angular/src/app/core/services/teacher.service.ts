import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Teacher {
  id: number;
  userId: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  dni: string | null;
  gender: string | null;
  birthDate: string | null;
  department: string | null;
  specializationArea: string | null;
  hireDate: string;
  isActive: boolean;
  phoneNumber: string | null;
  addressLine: string | null;
  city: string | null;
  province: string | null;
  postalCode: string | null;
  emergencyContactName: string | null;
  emergencyContactRelationship: string | null;
  emergencyContactPhone: string | null;
  deactivatedAt: string | null;
  deactivatedByUserId: number | null;
  deactivationReason: string | null;
}

export interface SaveTeacherRequest {
  userId?: number;
  employeeNumber: string;
  department?: string | null;
  specializationArea?: string | null;
  hireDate: string;
  phoneNumber?: string | null;
  addressLine?: string | null;
  city?: string | null;
  province?: string | null;
  postalCode?: string | null;
  emergencyContactName?: string | null;
  emergencyContactRelationship?: string | null;
  emergencyContactPhone?: string | null;
}

export interface TeacherAssignment {
  id: number;
  teacherId: number;
  teacherName: string;
  teachingPositionId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  commissionId: number | null;
  commissionCode: string | null;
  commissionName: string | null;
  academicYear: number;
  semester: number;
  positionType: string;
  maxStudents: number;
  startedOn: string;
  endedOn: string | null;
  isCurrent: boolean;
  assignmentReason: string | null;
  endReason: string | null;
  assignedByUserId: number | null;
  endedByUserId: number | null;
  createdAt: string;
  endedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class TeacherService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getTeachers(includeInactive = false): Observable<Teacher[]> {
    return this.http.get<Teacher[]>(`${this.base}v1/teachers`, {
      params: new HttpParams().set('includeInactive', includeInactive)
    });
  }

  getTeacher(id: number): Observable<Teacher> {
    return this.http.get<Teacher>(`${this.base}v1/teachers/${id}`);
  }

  createTeacher(request: SaveTeacherRequest): Observable<Teacher> {
    return this.http.post<Teacher>(`${this.base}v1/teachers`, request);
  }

  updateTeacher(id: number, request: SaveTeacherRequest): Observable<Teacher> {
    return this.http.put<Teacher>(`${this.base}v1/teachers/${id}`, request);
  }

  deactivateTeacher(id: number, reason?: string): Observable<void> {
    let params = new HttpParams();
    if (reason) params = params.set('reason', reason);
    return this.http.delete<void>(`${this.base}v1/teachers/${id}`, { params });
  }

  getAssignments(teacherId: number, includeEnded = false): Observable<TeacherAssignment[]> {
    return this.http.get<TeacherAssignment[]>(`${this.base}v1/teachers/${teacherId}/assignments`, {
      params: new HttpParams().set('includeEnded', includeEnded)
    });
  }

  getMyAssignments(includeEnded = false): Observable<TeacherAssignment[]> {
    return this.http.get<TeacherAssignment[]>(`${this.base}v1/teachers/me/assignments`, {
      params: new HttpParams().set('includeEnded', includeEnded)
    });
  }

  assignTeacher(teacherId: number, teachingPositionId: number, startedOn: string, reason?: string | null): Observable<TeacherAssignment> {
    return this.http.post<TeacherAssignment>(`${this.base}v1/teachers/${teacherId}/assignments`, {
      teachingPositionId,
      startedOn,
      reason: reason || null
    });
  }

  endAssignment(teacherId: number, assignmentId: number, endedOn: string, reason: string): Observable<TeacherAssignment> {
    return this.http.delete<TeacherAssignment>(`${this.base}v1/teachers/${teacherId}/assignments/${assignmentId}`, {
      body: { endedOn, reason }
    });
  }
}
