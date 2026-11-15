import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AdminService } from './admin.service';
import { isTeacherEmail } from './tenant.service';
import { environment } from '../../../environments/environment';

describe('Aspirantes: contrato HTTP', () => {
  let service: AdminService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lista los pendientes filtrando por rol y búsqueda', () => {
    service.getApplicants('pending', ' ana ', 2, 3, 50).subscribe();
    const req = http.expectOne(r => r.url === `${environment.apiServer}v1/admin/applicants`);
    expect(req.request.params.get('status')).toBe('pending');
    expect(req.request.params.get('search')).toBe('ana');
    expect(req.request.params.get('role')).toBe('2');
    expect(req.request.params.get('page')).toBe('3');
    expect(req.request.params.get('pageSize')).toBe('50');
    req.flush({ success: true, items: [], total: 0, page: 3, pageSize: 50 });
  });

  it('da de alta en masa enviando los ids', () => {
    service.approveApplicants([4, 5]).subscribe(res => expect(res.approvedIds).toEqual([4, 5]));
    const req = http.expectOne(`${environment.apiServer}v1/admin/applicants/approve`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userIds: [4, 5] });
    req.flush({ success: true, approvedIds: [4, 5], skipped: 0 });
  });
});

describe('isTeacherEmail', () => {
  it('coincide solo con el dominio exacto, sin importar mayúsculas', () => {
    expect(isTeacherEmail('maria.quintar@itscordoba.edu.ar', 'itscordoba.edu.ar')).toBe(true);
    expect(isTeacherEmail('MARIA@ITSCORDOBA.EDU.AR', 'itscordoba.edu.ar')).toBe(true);
    expect(isTeacherEmail('maria@sub.itscordoba.edu.ar', 'itscordoba.edu.ar')).toBe(false);
    expect(isTeacherEmail('maria@gmail.com', 'itscordoba.edu.ar')).toBe(false);
    expect(isTeacherEmail('maria@itscordoba.edu.ar', null)).toBe(false);
  });
});
