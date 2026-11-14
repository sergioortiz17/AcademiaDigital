import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DelinquentStudent {
  studentId: number;
  studentName: string;
  dni: string;
  legajoNumber: string;
  careerName: string;
  commissionName?: string;
  academicYear: number;
  outstandingAmount: number;
  overdueAmount: number;
  overdueInstallments: number;
  partialInstallments: number;
  maximumDaysOverdue: number;
  triggeredAlerts: string[];
}

export interface DelinquencyDashboard {
  asOf: string;
  studentCount: number;
  outstandingTotal: number;
  overdueTotal: number;
  students: DelinquentStudent[];
}

export interface DelinquencyFilter {
  careerId?: number | null;
  commissionId?: number | null;
  academicYear?: number | null;
  asOf?: string | null;
}

export interface DelinquencyRule {
  id: number;
  name: string;
  minimumOutstandingAmount: number;
  minimumDaysOverdue: number;
  isActive: boolean;
}

export interface ExpenseCategory {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
}

export interface ExpenseEntry {
  publicId: string;
  kind: number;
  category: string;
  supplierName: string;
  expenseDate: string;
  amount: number;
  currency: string;
  observation?: string;
  hasAttachment: boolean;
  attachmentSha256?: string;
  createdAtUtc: string;
}

export interface ExpenseLedger {
  expenseTotal: number;
  reversalTotal: number;
  netTotal: number;
  entries: ExpenseEntry[];
}

@Injectable({ providedIn: 'root' })
export class FinanceOperationsService {
  private readonly base = `${environment.apiServer}v1/finance`;

  constructor(private readonly http: HttpClient) {}

  getDelinquency(filter: DelinquencyFilter): Observable<DelinquencyDashboard> {
    return this.http.get<DelinquencyDashboard>(`${this.base}/delinquency`, { params: this.params(filter) });
  }

  exportDelinquency(format: 'pdf' | 'excel', filter: DelinquencyFilter): Observable<Blob> {
    return this.http.get(`${this.base}/delinquency/export`, {
      params: this.params({ ...filter, format }), responseType: 'blob'
    });
  }

  getDelinquencyRules(): Observable<DelinquencyRule[]> {
    return this.http.get<DelinquencyRule[]>(`${this.base}/delinquency/rules`);
  }

  createDelinquencyRule(value: Omit<DelinquencyRule, 'id'>): Observable<DelinquencyRule> {
    return this.http.post<DelinquencyRule>(`${this.base}/delinquency/rules`, value);
  }

  getExpenseCategories(activeOnly = false): Observable<ExpenseCategory[]> {
    return this.http.get<ExpenseCategory[]>(`${this.base}/expenses/categories`, {
      params: new HttpParams().set('activeOnly', activeOnly)
    });
  }

  createExpenseCategory(code: string, name: string): Observable<ExpenseCategory> {
    return this.http.post<ExpenseCategory>(`${this.base}/expenses/categories`, { code, name });
  }

  getExpenses(filter: { from?: string; to?: string; categoryId?: number | null; supplier?: string }): Observable<ExpenseLedger> {
    return this.http.get<ExpenseLedger>(`${this.base}/expenses`, { params: this.params(filter) });
  }

  registerExpense(value: {
    categoryId: number; supplierName: string; expenseDate: string; amount: number;
    purchaseNote?: string; observation?: string; attachment?: File | null;
  }): Observable<ExpenseEntry> {
    const body = new FormData();
    body.set('categoryId', String(value.categoryId));
    body.set('supplierName', value.supplierName);
    body.set('expenseDate', value.expenseDate);
    body.set('amount', String(value.amount));
    if (value.purchaseNote) body.set('purchaseNote', value.purchaseNote);
    if (value.observation) body.set('observation', value.observation);
    if (value.attachment) body.set('attachment', value.attachment, value.attachment.name);
    return this.http.post<ExpenseEntry>(`${this.base}/expenses`, body);
  }

  reverseExpense(publicId: string, reason: string): Observable<ExpenseEntry> {
    return this.http.post<ExpenseEntry>(`${this.base}/expenses/${publicId}/reverse`, { reason });
  }

  downloadExpenseAttachment(publicId: string): Observable<Blob> {
    return this.http.get(`${this.base}/expenses/${publicId}/attachment`, { responseType: 'blob' });
  }

  private params(source: object): HttpParams {
    let params = new HttpParams();
    Object.entries(source).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') params = params.set(key, String(value));
    });
    return params;
  }
}
