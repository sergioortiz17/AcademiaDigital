import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, afterEach, describe, expect, it } from 'vitest';
import { SubjectService, PlanCourseInput } from './subject.service';
import { environment } from '../../../environments/environment';
describe('Plan course write contracts', () => {
  let service: SubjectService; let http: HttpTestingController;
  const input: PlanCourseInput = { courseId: 8, yearNumber: 2, semester: 1, sortOrder: 3,
    courseTypeId: null, isMandatory: true, credits: null, workloadHours: 80 };
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(SubjectService); http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('posts a new relation', () => {
    service.addPlanCourse(5, input).subscribe();
    const request = http.expectOne(`${environment.apiServer}v1/study-plans/5/courses`);
    expect(request.request.method).toBe('POST'); expect(request.request.body).toEqual(input); request.flush({ id: 12 });
  });
  it('updates using the relation id, not the course id', () => {
    service.updatePlanCourse(5, 12, input).subscribe();
    const request = http.expectOne(`${environment.apiServer}v1/study-plans/5/courses/12`);
    expect(request.request.method).toBe('PUT'); expect(request.request.body).toEqual(input); request.flush(null);
  });
  it('deletes the relation only', () => {
    service.removePlanCourse(5, 12).subscribe();
    const request = http.expectOne(`${environment.apiServer}v1/study-plans/5/courses/12`);
    expect(request.request.method).toBe('DELETE'); request.flush(null);
  });
});
