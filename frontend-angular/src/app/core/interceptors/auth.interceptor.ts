import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { logout } from '../../store/account/account.actions';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private router: Router, private store: Store) {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.getToken();

    if (token) {
      request = request.clone({
        setHeaders: { Authorization: token }
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
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
