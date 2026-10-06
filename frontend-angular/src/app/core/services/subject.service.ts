import { Injectable } from '@angular/core';

import { HttpClient, HttpContext } from '@angular/common/http';

import { Observable } from 'rxjs';

import { SKIP_ERROR_ALERT } from '../interceptors/error.interceptor';
import { environment } from '../../../environments/environment';

export interface StudyPlan {

  id: number;
  careerId: number;
  code:string;
  name: string;
  versionNumber:number;
  status:string;
  effectiveFrom:string;
  effectiveTo:string;
  isActive: boolean;
}
    
export interface CourseApprovalRule {
    minimumRegularGrade: number | null;
    minimumPromotionGrade: number | null;
    minimumFinalExamGrade: number;
    minimumAttendancePercentage: number | null;
    requiresFinalExam: boolean;
    allowsPromotion: boolean;
}

export interface Subject{
    id:number;
    studyPlanId:number;
    courseId:number;
    courseCode:string;
    courseName:string;
    yearNumber:number;
    semester:number;
    isAnnual:boolean;
    sortOrder:number;
    isMandatory:boolean;
    credits:number | null;
    workloadHours:number | null;
    courseType: string | null;
    courseTypeId?: number | null;
    approvalRule: CourseApprovalRule | null;
}

export interface PlanCourseInput {
  courseId: number;
  yearNumber: number;
  semester: number;
  sortOrder: number;
  courseTypeId: number | null;
  isMandatory: boolean;
  credits: number | null;
  workloadHours: number | null;
}

@Injectable({
    providedIn:'root'
})

export class SubjectService{

    private baseURL=environment.apiServer;

    constructor(private http:HttpClient){}

    getStudyPlansByCareer(careerId:number):Observable<StudyPlan[]>{

    return this.http.get<StudyPlan[]>(
      `${this.baseURL}v1/careers/${careerId}/study-plans`
    );
    }

    getSubjectsByCareer(studyPlanId:number):Observable<Subject[]>{

        return this.http.get<Subject[]>(
            `${this.baseURL}v1/study-plans/${studyPlanId}/courses`
        );
    }

    // Upsert de la regla de aprobación de una materia del plan (admin-only en el backend).
    setApprovalRule(studyPlanId:number, studyPlanCourseId:number, rule: CourseApprovalRule):Observable<void>{
        return this.http.put<void>(
            `${this.baseURL}v1/study-plans/${studyPlanId}/courses/${studyPlanCourseId}/approval-rule`,
            rule
        );
    }

    addPlanCourse(planId: number, input: PlanCourseInput): Observable<Subject> {
      return this.http.post<Subject>(`${this.baseURL}v1/study-plans/${planId}/courses`, input,
        { context: new HttpContext().set(SKIP_ERROR_ALERT, true) });
    }
    updatePlanCourse(planId: number, id: number, input: PlanCourseInput): Observable<void> {
      return this.http.put<void>(`${this.baseURL}v1/study-plans/${planId}/courses/${id}`, input,
        { context: new HttpContext().set(SKIP_ERROR_ALERT, true) });
    }
    removePlanCourse(planId: number, id: number): Observable<void> {
      return this.http.delete<void>(`${this.baseURL}v1/study-plans/${planId}/courses/${id}`,
        { context: new HttpContext().set(SKIP_ERROR_ALERT, true) });
    }

}