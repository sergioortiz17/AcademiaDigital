import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ExamTableService } from './exam-table.service';
import { environment } from '../../../environments/environment';

describe('ExamTableService HTTP contracts', () => {
  let service: ExamTableService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ExamTableService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('loads the authenticated student exam tables without an envelope', () => {
    service.getMine().subscribe(items => expect(items).toEqual([]));
    http.expectOne(`${environment.apiServer}v1/exam-tables/me`).flush([]);
  });
  it('submits the existing registration contract', () => {
    service.register(12, 81).subscribe();
    const request = http.expectOne(`${environment.apiServer}v1/exam-tables/12/registrations`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ enrollmentId: 81 });
    request.flush({ id: 30 });
  });
  it('reads every history page to avoid missing older regularized courses', () => {
    service.getAcademicEnrollments(9).subscribe(items => expect(items.map(item => item.enrollmentId)).toEqual([81, 82]));
    const first = http.expectOne(request => request.url.endsWith('/students/9/academic-history') && request.params.get('page') === '1');
    first.flush({ items: [{ enrollmentId: 81 }], page: 1, pageSize: 1, total: 2 });
    const second = http.expectOne(request => request.params.get('page') === '2');
    second.flush({ items: [{ enrollmentId: 82 }], page: 2, pageSize: 1, total: 2 });
  });
});
