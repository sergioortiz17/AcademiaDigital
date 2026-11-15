import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { logout } from '../../store/account/account.actions';

const RETRYABLE_STATUSES = new Set([502, 503, 504]);
const MAX_RETRIES = 2;
const RETRY_DELAY_MS = 800;

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private router: Router, private store: Store) {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return this.doRequest(this.addAuth(request), next, 0);
  }

  private doRequest(request: HttpRequest<any>, next: HttpHandler, attempt: number): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (RETRYABLE_STATUSES.has(error.status) && attempt < MAX_RETRIES) {
          return timer(RETRY_DELAY_MS).pipe(
            switchMap(() => this.doRequest(this.addAuth(request), next, attempt + 1))
          );
        }
        
        if (error.status === 401 && !this.isLoginRequest(request) && !this.isPlatformRequest(request)) {
          this.store.dispatch(logout());
          localStorage.removeItem('academia-account');
          if (this.router.url !== '/auth/signin') {
            this.router.navigate(['/auth/signin']);
          }
        }
        const message = error.error?.msg || error.message || 'An error occurred';
        return throwError(() => error);
      })
    );
  }

  private isLoginRequest(request: HttpRequest<any>): boolean {
    return request.url.endsWith('/v1/users/login');
  }

  /** La API de plataforma usa su propia clave: un 401 ahí no es una sesión vencida. */
  private isPlatformRequest(request: HttpRequest<any>): boolean {
    return request.url.includes('/v1/platform/');
  }

  /**
   * Multitenant: X-Tenant lleva el dominio del navegador (la institución). El backend lo usa para
   * elegir la base de datos; no es un secreto, el aislamiento lo da la base separada por institución.
   */
  private addAuth(request: HttpRequest<any>): HttpRequest<any> {
    const token = this.getToken();
    const setHeaders: Record<string, string> = { 'X-Tenant': window.location.hostname };
    if (token) setHeaders['Authorization'] = `Bearer ${token}`;
    return request.clone({ setHeaders });
  }

  private getToken(): string | null {
    try {
      const raw = localStorage.getItem('academia-account');
      if (raw) {
        const state = JSON.parse(raw);
        return state.token || null;
      }
    } catch {
      // ignore
    }
    return null;
  }
}
