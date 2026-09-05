import { Injectable } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface Career {
  success: boolean;
  id: number;
  name: string;
  code: string;
  description: string;
  totalCredits: number;
  durationYears: number;
  isActive: boolean;
  createdAt: string;
  courseCount?: number;
}

export interface CreateCareerPayload {
  name: string;
  code: string;
  description?: string;
  totalCredits: number;
  durationYears: number;
}

export interface StudyPlan {
  id: number;
  careerId: number;
  code: string;
  name: string;
  versionNumber: number;
  status: 'Draft' | 'Active' | 'Archived';
  effectiveFrom: string | null;
  effectiveTo: string | null;
  isActive: boolean;
}

export interface CsvRowError {
  row: number;
  error: string;
}

export interface StudyPlanImportResult {
  success: boolean;
  studyPlanId?: number;
  coursesCreated?: number;
  prerequisitesCreated?: number;
  errors?: CsvRowError[];
}

export interface FieldChange {
  field: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface PrerequisiteChanges {
  added: string[];
  removed: string[];
}

export interface CourseDiffItem {
  courseCode: string;
  name: string;
  yearNumber: number;
  semester: number;
  courseTypeCode: string | null;
  workloadHours: number | null;
  isMandatory: boolean;
  prerequisites: string[];
}

export interface ModifiedCourseDiff {
  courseCode: string;
  name: string;
  fieldChanges: FieldChange[];
  prerequisiteChanges: PrerequisiteChanges | null;
}

export interface StudyPlanDiff {
  studyPlanAId: number;
  studyPlanBId: number | null;
  addedCourses: CourseDiffItem[];
  removedCourses: CourseDiffItem[];
  modifiedCourses: ModifiedCourseDiff[];
  unchangedCourseCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class CareerService {
  private baseURL = environment.apiServer;

  constructor(private http: HttpClient) {}

  getCareers(): Observable<Career[]> {
    return this.http.get<Career[]>(`${this.baseURL}v1/careers`);
  }

  createCareer(payload: CreateCareerPayload): Observable<Career> {
    return this.http.post<Career>(`${this.baseURL}v1/careers`, payload);
  }

  deleteCareer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseURL}v1/careers/${id}`);
  }

  getStudyPlans(careerId: number): Observable<StudyPlan[]> {
    return this.http.get<StudyPlan[]>(`${this.baseURL}v1/careers/${careerId}/study-plans`);
  }

  importStudyPlanCsv(careerId: number, formData: FormData): Observable<StudyPlanImportResult> {
    return this.http.post<StudyPlanImportResult>(
      `${this.baseURL}v1/careers/${careerId}/study-plans/import`,
      formData
    );
  }

  previewStudyPlanDiff(careerId: number, studyPlanId: number, formData: FormData): Observable<StudyPlanDiff> {
    return this.http.post<StudyPlanDiff>(
      `${this.baseURL}v1/careers/${careerId}/study-plans/${studyPlanId}/diff-preview`,
      formData
    );
  }

  diffStudyPlans(planAId: number, planBId: number): Observable<StudyPlanDiff> {
    return this.http.get<StudyPlanDiff>(`${this.baseURL}v1/study-plans/${planAId}/diff/${planBId}`);
  }
}
