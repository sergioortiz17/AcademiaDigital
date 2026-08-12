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
        if (error.status === 401) {
          console.log('🚪 ACÁ TE ECHA PORQUE EL TOKEN ES INVÁLIDO/EXPIRÓ (auth.interceptor.ts)');
          this.store.dispatch(logout());
          localStorage.removeItem('academia-account');
          if (this.router.url !== '/auth/signin') {
            this.router.navigate(['/auth/signin']);
          }
        }
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
