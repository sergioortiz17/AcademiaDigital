import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CertificateRequest {
  id: number;
  userId: number;
  username?: string;
  email?: string;
  certificateType: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  createdAt: string;
  updatedAt: string | null;
  kind?: string;
  studentCareerId?: number | null;
  examRegistrationId?: number | null;
  reviewedAt?: string | null;
  reviewedByUserId?: number | null;
  rejectionReason?: string | null;
}

export const CERTIFICATE_TYPES = [
  'Certificado de alumno regular',
  'Certificado de materias aprobadas',
  'Certificado de promedio',
  'Constancia de inscripción',
  'Constancia de egreso'
];

@Injectable({ providedIn: 'root' })
export class CertificatesService {
  private base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getMyCertificates(): Observable<{ success: boolean; requests: CertificateRequest[] }> {
    return this.http.get<{ success: boolean; requests: CertificateRequest[] }>(`${this.base}v1/certificates/my`);
  }

  requestCertificate(certificateType: string): Observable<{ success: boolean; request: CertificateRequest }> {
    return this.http.post<{ success: boolean; request: CertificateRequest }>(`${this.base}v1/certificates/request`, { certificateType });
  }

  getAllCertificates(search?: string, status?: string | null): Observable<{ success: boolean; requests: CertificateRequest[] }> {
    let params = new HttpParams();
    if (search?.trim()) params = params.set('search', search.trim());
    if (status) params = params.set('status', status);
    return this.http.get<{ success: boolean; requests: CertificateRequest[] }>(`${this.base}v1/certificates/all`, { params });
  }

  approveCertificate(id: number): Observable<CertificateRequest> {
    return this.http.post<CertificateRequest>(`${this.base}v1/certificates/${id}/approve`, {});
  }

  rejectCertificate(id: number, reason: string): Observable<CertificateRequest> {
    return this.http.post<CertificateRequest>(`${this.base}v1/certificates/${id}/reject`, { reason });
  }
}
