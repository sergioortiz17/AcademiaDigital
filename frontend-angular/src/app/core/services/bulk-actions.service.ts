import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BulkGraduateResult {
  graduated: number;
  failed: { id: number; reason: string }[];
}

export interface BulkEnrollResult {
  students: number;
  created: number;
  alreadyEnrolled: number;
  courses: string[];
}

@Injectable({ providedIn: 'root' })
export class BulkActionsService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  graduate(studentIds: number[], graduatedOn: string, reason: string): Observable<BulkGraduateResult> {
    return this.http.post<BulkGraduateResult>(`${this.base}v1/admin/bulk/graduate`, { studentIds, graduatedOn, reason });
  }

  enrollDivision(divisionId: number, semester: number): Observable<BulkEnrollResult> {
    return this.http.post<BulkEnrollResult>(`${this.base}v1/admin/bulk/enroll-division`, { divisionId, semester });
  }
}
