import { ErrorHandler, NgModule, Optional, SkipSelf } from '@angular/core';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ErrorInterceptor } from './interceptors/error.interceptor';
import { ClientErrorHandler } from './services/client-error-handler';

@NgModule({
  providers: [
    // Errores de JavaScript no capturados: consola + reporte al backend (Grafana).
    { provide: ErrorHandler, useClass: ClientErrorHandler },
    // El orden importa: AuthInterceptor corre primero (agrega token, reintenta 5xx,
    // desloguea en 401) y ErrorInterceptor queda por dentro para traducir el error
    // final a un mensaje y mostrarlo en el snackbar.
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true }
  ]
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() parentModule: CoreModule) {
    if (parentModule) {
      throw new Error('CoreModule is already loaded. Import only in AppModule.');
    }
  }
}
