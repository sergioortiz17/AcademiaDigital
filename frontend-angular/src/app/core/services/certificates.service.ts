import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CertificateRequest {
  id: number;
  userId: number;
  certificateType: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  createdAt: string;
  updatedAt: string | null;
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

  getAllCertificates(): Observable<{ success: boolean; requests: CertificateRequest[] }> {
    return this.http.get<{ success: boolean; requests: CertificateRequest[] }>(`${this.base}v1/certificates/all`);
  }
}
