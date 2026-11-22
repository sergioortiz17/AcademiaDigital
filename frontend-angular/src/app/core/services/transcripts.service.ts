import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TranscriptStudent {
  studentId: number; lastName: string; firstName: string; dni: string | null; cuil: string | null; gender: string | null;
  birthDate: string | null; addressLine: string | null; city: string | null; province: string | null; phone: string | null; email: string;
  legajoNumber: string; registryBook: string | null; registryFolio: string | null; status: string; admissionYear: number | null;
  graduatedOn: string | null; careerName: string; studyPlanName: string | null;
}

export interface TranscriptCourse {
  courseId: number; courseCode: string; courseName: string; yearNumber: number; period: string; condition: string;
  finalGrade: number | null; approvedOn: string | null; examTableId: number | null; actBook: string | null; actFolio: string | null;
}

export interface Transcript {
  student: TranscriptStudent;
  courses: TranscriptCourse[];
  approvedCount: number;
  average: number | null;
}

@Injectable({ providedIn: 'root' })
export class TranscriptsService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  get(studentId: number): Observable<Transcript> {
    return this.http.get<Transcript>(`${this.base}v1/admin/transcripts/${studentId}`);
  }

  setRegistry(studentId: number, book: string | null, folio: string | null): Observable<void> {
    return this.http.put<void>(`${this.base}v1/admin/transcripts/${studentId}/registry`, { book, folio });
  }

  setExamAct(examTableId: number, book: string | null, folio: string | null): Observable<void> {
    return this.http.put<void>(`${this.base}v1/admin/exam-tables/${examTableId}/act`, { book, folio });
  }
}
