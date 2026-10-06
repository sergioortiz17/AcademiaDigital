import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Course {
  id: number;
  careerId: number;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CourseService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getCoursesByCareer(careerId: number): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.base}v1/careers/${careerId}/courses`);
  }
}
