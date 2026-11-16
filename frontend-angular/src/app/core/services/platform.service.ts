import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_ALERT } from '../interceptors/error.interceptor';

export interface TenantSummary {
  id: string;
  slug: string;
  name: string;
  isActive: boolean;
  isDefault: boolean;
  createdAt: string;
  domains: string[];
  teacherEmailDomain?: string | null;
}

export interface CreateTenantPayload {
  slug: string;
  name: string;
  domains: string[];
  adminEmail: string;
  adminName: string;
  adminLastName: string;
  adminDni: string;
  adminPassword: string;
  /** Opcional: los emails de este dominio se registran como Profesor (pendiente de alta). */
  teacherEmailDomain?: string | null;
}

/** Pedido del formulario "Pide tu plataforma". */
export interface PlatformRequestPayload {
  institutionName: string;
  requestedSlug?: string | null;
  contactName: string;
  contactEmail: string;
  contactPhone?: string | null;
  location?: string | null;
  studentsEstimate?: number | null;
  message?: string | null;
  /** Trampa para bots: campo oculto que una persona deja vacío. */
  website?: string | null;
}

export enum PlatformRequestStatus { Pending = 0, Approved = 1, Rejected = 2 }

export interface PlatformRequestItem extends PlatformRequestPayload {
  id: string;
  status: PlatformRequestStatus;
  createdAt: string;
  reviewedAt?: string | null;
  tenantSlug?: string | null;
}

const KEY_STORAGE = 'academia-platform-key';

/**
 * API de plataforma (alta de instituciones). No usa la sesión de ningún usuario: se autentica con
 * la clave de operador (header X-Platform-Key), que se guarda solo en sessionStorage (se pierde
 * al cerrar la pestaña).
 */
@Injectable({ providedIn: 'root' })
export class PlatformService {
  private readonly baseUrl = `${environment.apiServer}v1/platform/tenants`;
  private readonly requestsUrl = `${environment.apiServer}v1/platform/requests`;

  constructor(private readonly http: HttpClient) {}

  get hasKey(): boolean {
    return !!sessionStorage.getItem(KEY_STORAGE);
  }

  setKey(key: string): void {
    sessionStorage.setItem(KEY_STORAGE, key.trim());
  }

  clearKey(): void {
    sessionStorage.removeItem(KEY_STORAGE);
  }

  list(): Observable<TenantSummary[]> {
    return this.http.get<TenantSummary[]>(this.baseUrl, this.options());
  }

  create(payload: CreateTenantPayload): Observable<TenantSummary> {
    return this.http.post<TenantSummary>(this.baseUrl, payload, this.options());
  }

  /** Formulario público de la landing: no usa la clave de operador. */
  submitRequest(payload: PlatformRequestPayload): Observable<{ success: boolean; msg: string }> {
    return this.http.post<{ success: boolean; msg: string }>(this.requestsUrl, payload, {
      context: new HttpContext().set(SKIP_ERROR_ALERT, true)
    });
  }

  listRequests(status?: PlatformRequestStatus): Observable<PlatformRequestItem[]> {
    const query = status === undefined ? '' : `?status=${status}`;
    return this.http.get<PlatformRequestItem[]>(`${this.requestsUrl}${query}`, this.options());
  }

  reviewRequest(id: string, status: PlatformRequestStatus, tenantSlug?: string): Observable<PlatformRequestItem> {
    return this.http.patch<PlatformRequestItem>(`${this.requestsUrl}/${id}`, { status, tenantSlug: tenantSlug ?? null }, this.options());
  }

  private options() {
    return {
      headers: new HttpHeaders({ 'X-Platform-Key': sessionStorage.getItem(KEY_STORAGE) ?? '' }),
      // Los errores (clave inválida, conflicto) los muestra la propia pantalla.
      context: new HttpContext().set(SKIP_ERROR_ALERT, true)
    };
  }
}
