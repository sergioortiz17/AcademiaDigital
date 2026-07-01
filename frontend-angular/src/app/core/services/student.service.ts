import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Student {
  id: number;
  userId: number;
  userEmail: string;
  userName: string;
  careerId: number;
  careerName: string;
  legajoNumber: string;
  enrollmentDate: string;
  status: string;
  currentStudyPlanId: number;
  currentStudyPlanName: string;
}

@Injectable({
  providedIn: 'root'
})
export class StudentService {

  private baseURL = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getStudentByUserId(studentId: number): Observable<Student> {
    return this.http.get<Student>(
      `${this.baseURL}v1/students/${studentId}`
    );
  }
}