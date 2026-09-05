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
        // TEMPORAL: logout/redirect automático ante 401 deshabilitado por demo
        // (2026-09-02) para no bloquear la navegación mientras se investiga el
        // root cause (requests admin sin cancelar en ngOnDestroy que resuelven
        // tarde con 401 tras navegar). Se reactiva en el fix de fondo (v3, tarea 8).
        const message = error.error?.msg || error.message || 'An error occurred';
        return throwError(() => new Error(message));
      })
    );
  }

  private addAuth(request: HttpRequest<any>): HttpRequest<any> {
    const token = this.getToken();
    return token
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;
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
