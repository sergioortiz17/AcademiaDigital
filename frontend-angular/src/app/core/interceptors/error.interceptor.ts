import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
  HttpContextToken
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { NotificationService } from '../services/notification.service';
import { toApiError } from '../models/api-error';

/**
 * Permite que un request puntual se maneje su error a mano, sin alerta automática:
 *
 *   this.http.get(url, { context: new HttpContext().set(SKIP_ERROR_ALERT, true) })
 */
export const SKIP_ERROR_ALERT = new HttpContextToken<boolean>(() => false);

/** El 401 ya dispara logout + redirect en AuthInterceptor: no hace falta alertar. */
const SILENT_STATUSES = new Set([401]);

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private notifications: NotificationService) {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((res: HttpErrorResponse) => {
        const apiError = toApiError(res);

        if (!request.context.get(SKIP_ERROR_ALERT) && !SILENT_STATUSES.has(apiError.status)) {
          this.notifications.error(apiError.message);
        }

        return throwError(() => apiError);
      })
    );
  }
}
