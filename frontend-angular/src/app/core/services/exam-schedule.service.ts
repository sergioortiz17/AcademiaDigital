import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TribunalSuggestion { teacherId: number; teacherName: string; role: 'President' | 'Vocal'; }

export interface ExamProposalRow {
  courseId: number; courseCode: string; courseName: string; yearNumber: number;
  date: string | null; reason: string | null; tribunal: TribunalSuggestion[];
}

export interface ExamScheduleRequest {
  careerId: number; academicYear: number; from: string; to: string; yearNumbers: number[]; blockedDates: string[];
}

export interface CreateExamTableInput {
  courseId: number; academicYear: number; callNumber: number; examDateUtc: string; registrationDeadlineUtc: string;
  location: string; tribunal: { teacherId: number; role: 'President' | 'Vocal' }[];
}

@Injectable({ providedIn: 'root' })
export class ExamScheduleService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  propose(request: ExamScheduleRequest): Observable<ExamProposalRow[]> {
    return this.http.post<ExamProposalRow[]>(`${this.base}v1/exam-tables/schedule-proposal`, request);
  }

  create(input: CreateExamTableInput, idempotencyKey: string): Observable<unknown> {
    return this.http.post(`${this.base}v1/exam-tables`, input, { headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }) });
  }
}
