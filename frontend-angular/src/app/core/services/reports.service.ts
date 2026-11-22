import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type StudentReportKind = 'Students' | 'Applicants';
export type StudentStatusFilter = 'Regular' | 'Libre' | 'Graduated' | 'Withdrawn';

export interface StudentReportFilter {
  kind: StudentReportKind;
  status: StudentStatusFilter | null;
  careerId: number | null;
  from: string | null;
  to: string | null;
}

export interface StudentReportRow {
  legajoNumber: string | null;
  lastName: string;
  firstName: string;
  dni: string | null;
  cuil: string | null;
  gender: string | null;
  birthDate: string | null;
  email: string;
  phone: string | null;
  addressLine: string | null;
  city: string | null;
  province: string | null;
  postalCode: string | null;
  careerName: string | null;
  status: string;
  registeredOn: string;
  enrolledOn: string | null;
  graduatedOn: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  healthInsurance: string | null;
  registryBook: string | null;
  registryFolio: string | null;
  documentType: string | null;
  alternatePhone: string | null;
  neighborhood: string | null;
  district: string | null;
  country: string | null;
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getStudents(filter: StudentReportFilter): Observable<StudentReportRow[]> {
    let params = new HttpParams().set('kind', filter.kind);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.careerId != null) params = params.set('careerId', filter.careerId);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    return this.http.get<StudentReportRow[]>(`${this.base}v1/admin/reports/students`, { params });
  }
}
