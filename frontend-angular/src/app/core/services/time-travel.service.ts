import { Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_ALERT } from '../interceptors/error.interceptor';

export interface TimeTravelStatus {
  realNow: string;
  effectiveNow: string;
  enabled: boolean;
  fakeNow: string | null;
}

/**
 * Consulta el reloj simulable de DevTools ("Volver al futuro"). Si la función está
 * deshabilitada en este ambiente (falta ALLOW_TIME_TRAVEL en el backend), el endpoint
 * devuelve 403/404 y acá lo tratamos como "no hay simulación" en vez de romper el header.
 *
 * En producción la función está siempre apagada: tras el primer 403/404 se deja de
 * consultar (el header hacía polling cada 30 s) y nunca se muestra la alerta de error.
 */
@Injectable({ providedIn: 'root' })
export class TimeTravelService {
  private readonly base = environment.apiServer;
  /** true cuando el backend respondió que la función está deshabilitada en este ambiente. */
  private disabled = false;

  constructor(private readonly http: HttpClient) {}

  getStatus(): Observable<TimeTravelStatus | null> {
    if (this.disabled) return of(null);

    return this.http.get<TimeTravelStatus>(`${this.base}v1/devtools/time`, {
      context: new HttpContext().set(SKIP_ERROR_ALERT, true)
    }).pipe(
      catchError((error: unknown) => {
        // El ErrorInterceptor reemplaza el HttpErrorResponse por un ApiError: los dos tienen
        // `status`, así que se lee el campo en lugar de comprobar el tipo.
        const status = (error as { status?: number } | null)?.status;
        if (status === 403 || status === 404) {
          this.disabled = true;
        }
        return of(null);
      })
    );
  }
}
