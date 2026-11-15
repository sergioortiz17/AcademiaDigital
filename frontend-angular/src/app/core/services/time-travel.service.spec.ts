import { TestBed } from '@angular/core/testing';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TimeTravelService } from './time-travel.service';
import { ErrorInterceptor } from '../interceptors/error.interceptor';
import { NotificationService } from './notification.service';
import { environment } from '../../../environments/environment';

const url = `${environment.apiServer}v1/devtools/time`;

// Con el ErrorInterceptor real en el medio, como en la app: el error que llega al
// servicio es un ApiError, no un HttpErrorResponse.
describe('TimeTravelService', () => {
  let service: TimeTravelService;
  let http: HttpTestingController;
  const notifications = { error: vi.fn() };

  beforeEach(() => {
    notifications.error.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
        { provide: NotificationService, useValue: notifications }
      ]
    });
    service = TestBed.inject(TimeTravelService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  for (const status of [403, 404]) {
    it(`deja de consultar después de un ${status} y no muestra alerta`, () => {
      const results: unknown[] = [];
      service.getStatus().subscribe(r => results.push(r));
      http.expectOne(url).flush({ success: false, msg: 'deshabilitado' }, { status, statusText: 'x' });

      service.getStatus().subscribe(r => results.push(r));
      http.expectNone(url);

      expect(results).toEqual([null, null]);
      expect(notifications.error).not.toHaveBeenCalled();
    });
  }

  it('sigue consultando después de un error transitorio (500)', () => {
    service.getStatus().subscribe();
    http.expectOne(url).flush({}, { status: 500, statusText: 'x' });

    service.getStatus().subscribe();
    http.expectOne(url).flush({ realNow: 'a', effectiveNow: 'a', enabled: false, fakeNow: null });
  });
});
