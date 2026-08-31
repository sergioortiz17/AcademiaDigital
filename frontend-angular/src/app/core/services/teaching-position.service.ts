import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TeachingPosition {
  id: number;
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
  isVacant: boolean;
  isActive: boolean;
  teacherId: number | null;
  teacherName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface TeachingPositionFilters {
  academicYear?: number;
  semester?: number;
  isVacant?: boolean;
  includeInactive?: boolean;
}

export interface SaveTeachingPositionRequest {
  courseId: number;
  commissionId: number;
  academicYear: number;
  semester: number;
  positionType: string;
  maxStudents: number;
}

@Injectable({ providedIn: 'root' })
export class TeachingPositionService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getTeachingPositions(filters: TeachingPositionFilters = {}): Observable<TeachingPosition[]> {
    let params = new HttpParams();
    if (filters.academicYear != null) params = params.set('academicYear', filters.academicYear);
    if (filters.semester != null) params = params.set('semester', filters.semester);
    if (filters.isVacant != null) params = params.set('isVacant', filters.isVacant);
    if (filters.includeInactive != null) params = params.set('includeInactive', filters.includeInactive);
    return this.http.get<TeachingPosition[]>(`${this.base}v1/teaching-positions`, { params });
  }

  getTeachingPosition(id: number): Observable<TeachingPosition> {
    return this.http.get<TeachingPosition>(`${this.base}v1/teaching-positions/${id}`);
  }

  createTeachingPosition(request: SaveTeachingPositionRequest): Observable<TeachingPosition> {
    return this.http.post<TeachingPosition>(`${this.base}v1/teaching-positions`, request);
  }

  updateTeachingPosition(id: number, request: SaveTeachingPositionRequest): Observable<TeachingPosition> {
    return this.http.put<TeachingPosition>(`${this.base}v1/teaching-positions/${id}`, request);
  }

  deactivateTeachingPosition(id: number, reason: string): Observable<void> {
    return this.http.delete<void>(`${this.base}v1/teaching-positions/${id}`, {
      params: new HttpParams().set('reason', reason)
    });
  }
}
