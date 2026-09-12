import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

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
 */
@Injectable({ providedIn: 'root' })
export class TimeTravelService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getStatus(): Observable<TimeTravelStatus | null> {
    return this.http.get<TimeTravelStatus>(`${this.base}v1/devtools/time`).pipe(
      catchError(() => of(null))
    );
  }
}
