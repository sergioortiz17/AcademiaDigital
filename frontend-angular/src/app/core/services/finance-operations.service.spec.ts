import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { FinanceOperationsService } from './finance-operations.service';

describe('FinanceOperationsService HTTP contracts', () => {
  let service: FinanceOperationsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(FinanceOperationsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends delinquency filters through the monolith finance route', () => {
    service.getDelinquency({ careerId: 4, academicYear: 2026 }).subscribe(result => expect(result.studentCount).toBe(1));
    const request = http.expectOne(item => item.url === `${environment.apiServer}v1/finance/delinquency`);
    expect(request.request.params.get('careerId')).toBe('4');
    expect(request.request.params.get('academicYear')).toBe('2026');
    request.flush({ asOf: '2026-11-14', studentCount: 1, outstandingTotal: 100, overdueTotal: 100, students: [] });
  });

  it('registers an expense as multipart data without setting a physical file path', () => {
    const attachment = new File(['%PDF-1.4'], 'invoice.pdf', { type: 'application/pdf' });
    service.registerExpense({ categoryId: 2, supplierName: 'Proveedor', expenseDate: '2026-11-14', amount: 250, attachment }).subscribe();
    const request = http.expectOne(`${environment.apiServer}v1/finance/expenses`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    const uploaded = (request.request.body as FormData).get('attachment') as File;
    expect(uploaded.name).toBe('invoice.pdf');
    expect(uploaded.type).toBe('application/pdf');
    request.flush({ publicId: 'expense-id' });
  });

  it('uses a compensating reversal endpoint instead of editing the expense', () => {
    service.reverseExpense('8aebdf1e-a093-407c-aa03-f4a7fab57269', 'Carga duplicada').subscribe();
    const request = http.expectOne(`${environment.apiServer}v1/finance/expenses/8aebdf1e-a093-407c-aa03-f4a7fab57269/reverse`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ reason: 'Carga duplicada' });
    request.flush({ publicId: 'reversal-id' });
  });
});
