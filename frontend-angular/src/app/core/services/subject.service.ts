import { Injectable } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { Observable } from 'rxjs';

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
    credits:number;
    workloadHours:number;
    courseType: null;
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

}