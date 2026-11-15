import { Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_ALERT } from '../interceptors/error.interceptor';

export interface CurrentTenant {
  slug: string;
  name: string;
}

/** Slug de la institución original: es la única que usa el logo y los textos históricos de ITSC. */
export const DEFAULT_TENANT_SLUG = 'itsc';

/**
 * Institución (tenant) del dominio actual. El backend la resuelve por el host del navegador
 * (header X-Tenant); acá solo se consulta su nombre para el título y el login.
 */
@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly subject = new BehaviorSubject<CurrentTenant | null>(null);
  readonly tenant$: Observable<CurrentTenant | null> = this.subject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get current(): CurrentTenant | null {
    return this.subject.value;
  }

  load(): void {
    this.http
      .get<CurrentTenant>(`${environment.apiServer}v1/tenant/current`, {
        context: new HttpContext().set(SKIP_ERROR_ALERT, true)
      })
      .subscribe({
        next: tenant => {
          this.subject.next(tenant);
          document.title = `${tenant.name} | Academia Digital`;
        },
        // Dominio sin institución registrada u API caída: la app sigue con el branding por defecto.
        error: () => {}
      });
  }
}
