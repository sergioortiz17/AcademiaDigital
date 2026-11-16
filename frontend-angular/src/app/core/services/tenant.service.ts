import { Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_ALERT } from '../interceptors/error.interceptor';

export interface CurrentTenant {
  slug: string;
  name: string;
  /** Dominio de correo docente (ej. itscordoba.edu.ar): esos emails se registran como Profesor. */
  teacherEmailDomain?: string | null;
}

/** true si el email es del dominio docente exacto (sin subdominios), igual que el backend. */
export function isTeacherEmail(email: string | null | undefined, domain: string | null | undefined): boolean {
  if (!email || !domain) return false;
  const at = email.trim().lastIndexOf('@');
  return at > 0 && email.trim().slice(at + 1).toLowerCase() === domain.toLowerCase();
}

/** Slug de la institución original: es la única que usa el logo y los textos históricos de ITSC. */
export const DEFAULT_TENANT_SLUG = 'itsc';

export interface FaviconSet {
  svg: string;
  ico: string;
  appleTouch: string;
}

/** Íconos de pestaña del ITSC (public/). Los de Academia Digital son los de index.html. */
export const ITSC_FAVICON: FaviconSet = {
  svg: 'favicon-itsc.svg',
  ico: 'favicon-itsc.ico',
  appleTouch: 'apple-touch-icon-itsc.png'
};

/** Cambia los íconos de la pestaña (SVG, ICO y el de la pantalla de inicio del celular). */
export function applyFavicon(set: FaviconSet, doc: Document = document): void {
  const hrefs: Record<string, string> = {
    'link[rel="icon"][type="image/svg+xml"]': set.svg,
    'link[rel="alternate icon"]': set.ico,
    'link[rel="apple-touch-icon"]': set.appleTouch
  };
  for (const [selector, href] of Object.entries(hrefs)) {
    doc.querySelector<HTMLLinkElement>(selector)?.setAttribute('href', href);
  }
}

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
          // El ITSC usa su logo; las demás instituciones, la landing y el panel, el de Academia Digital.
          if (tenant.slug === DEFAULT_TENANT_SLUG) applyFavicon(ITSC_FAVICON);
        },
        // Dominio sin institución registrada u API caída: la app sigue con el branding por defecto.
        error: () => {}
      });
  }
}
