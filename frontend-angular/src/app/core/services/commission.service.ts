import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Commission {
  id: number;
  careerId: number;
  code: string;
  name: string;
  academicYear: number;
  yearNumber: number;
  shift: string;
  isActive: boolean;
}

export interface UpsertCommissionRequest {
  code: string;
  name: string;
  academicYear: number;
  yearNumber: number;
  shift: string;
}

@Injectable({ providedIn: 'root' })
export class CommissionService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getCommissions(careerId: number, academicYear?: number): Observable<Commission[]> {
    let params = new HttpParams();
    if (academicYear != null) params = params.set('academicYear', academicYear);
    return this.http.get<Commission[]>(`${this.base}v1/careers/${careerId}/commissions`, { params });
  }

  createCommission(careerId: number, request: UpsertCommissionRequest): Observable<Commission> {
    return this.http.post<Commission>(`${this.base}v1/careers/${careerId}/commissions`, request);
  }

  updateCommission(careerId: number, id: number, request: UpsertCommissionRequest): Observable<Commission> {
    return this.http.put<Commission>(`${this.base}v1/careers/${careerId}/commissions/${id}`, request);
  }

  deactivateCommission(careerId: number, id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}v1/careers/${careerId}/commissions/${id}`);
  }
}
