import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type StudentDocumentStatus = 'Submitted' | 'Approved' | 'Rejected' | 'Expired';

export interface DocumentRequirement {
  id: number;
  code: string;
  name: string;
  description: string | null;
  careerId: number | null;
  isRequired: boolean;
  isActive: boolean;
}

export interface StudentDocument {
  id: number;
  studentId: number;
  documentRequirementId: number;
  requirementName: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  status: StudentDocumentStatus;
  submittedAt: string;
  reviewedAt: string | null;
  observation: string | null;
}

export interface AdmissionDocuments {
  requirements: DocumentRequirement[];
  documents: StudentDocument[];
}

export interface StudentDocumentReviewItem {
  document: StudentDocument;
  studentName: string;
  legajoNumber: string;
  dni: string | null;
}

/** Requisitos de ingreso: el alumno sube sus documentos y Administración los revisa. */
@Injectable({ providedIn: 'root' })
export class AdmissionDocumentsService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getMine(): Observable<AdmissionDocuments> {
    return this.http.get<AdmissionDocuments>(`${this.base}v1/students/me/admission-documents`);
  }

  upload(requirementId: number, file: File): Observable<StudentDocument> {
    const form = new FormData();
    form.append('requirementId', String(requirementId));
    form.append('file', file, file.name);
    return this.http.post<StudentDocument>(`${this.base}v1/students/me/admission-documents`, form);
  }

  downloadMine(documentId: number): Observable<Blob> {
    return this.http.get(`${this.base}v1/students/me/admission-documents/${documentId}/file`, { responseType: 'blob' });
  }

  getForReview(status: StudentDocumentStatus | null): Observable<StudentDocumentReviewItem[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<StudentDocumentReviewItem[]>(`${this.base}v1/admin/student-documents`, { params });
  }

  download(studentId: number, documentId: number): Observable<Blob> {
    return this.http.get(`${this.base}v1/students/${studentId}/documents/${documentId}/file`, { responseType: 'blob' });
  }

  review(studentId: number, documentId: number, status: 'Approved' | 'Rejected', observation: string | null): Observable<StudentDocument> {
    return this.http.patch<StudentDocument>(`${this.base}v1/students/${studentId}/documents/${documentId}/status`, { status, observation });
  }
}

/** Texto del estado de un documento para mostrar en pantalla. */
export function documentStatusLabel(status: StudentDocumentStatus | null | undefined): string {
  switch (status) {
    case 'Submitted': return 'En revisión';
    case 'Approved': return 'Aprobado';
    case 'Rejected': return 'Rechazado';
    case 'Expired': return 'Reemplazado';
    default: return 'Pendiente de entrega';
  }
}

/** Abre o descarga un archivo recibido como Blob. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
