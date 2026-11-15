import { ErrorHandler, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

/** Máximo de reportes por pestaña abierta, para no inundar el log ante un error en bucle. */
const MAX_REPORTS_PER_PAGE = 10;
/** Un mismo mensaje se reporta como mucho una vez por minuto. */
const DEDUPE_WINDOW_MS = 60_000;

/**
 * ErrorHandler global: además de mostrar el error en la consola, lo envía al backend
 * (POST /api/v1/client-errors), que lo registra junto con los errores del servidor para
 * verlo en Grafana. Solo reporta en producción.
 *
 * Usa fetch y no HttpClient a propósito: así no pasa por los interceptores (que mostrarían
 * un snackbar o desloguearían) y un fallo al reportar nunca genera otro error en cadena.
 * Nunca envía datos de formularios ni el token completo: solo mensaje, stack y URL.
 */
@Injectable()
export class ClientErrorHandler implements ErrorHandler {
  private sent = 0;
  private readonly lastSentAt = new Map<string, number>();

  handleError(error: unknown): void {
    console.error(error);

    if (!environment.production) return;

    try {
      this.report(error);
    } catch {
      // El reporte es best effort: nunca debe romper la aplicación.
    }
  }

  private report(error: unknown): void {
    if (this.sent >= MAX_REPORTS_PER_PAGE) return;

    const err = error instanceof Error ? error : new Error(String(error));
    const message = err.message || 'Error sin mensaje';

    const now = Date.now();
    const last = this.lastSentAt.get(message);
    if (last !== undefined && now - last < DEDUPE_WINDOW_MS) return;
    this.lastSentAt.set(message, now);
    this.sent++;

    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    const token = this.currentToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    // Sin query string: puede traer tokens (por ejemplo, el de recuperar contraseña).
    const url = `${location.origin}${location.pathname}`;

    void fetch(`${environment.apiServer}v1/client-errors`, {
      method: 'POST',
      headers,
      keepalive: true,
      body: JSON.stringify({
        message: message.slice(0, 1_000),
        stack: (err.stack ?? '').slice(0, 4_000),
        url: url.slice(0, 500)
      })
    }).catch(() => undefined);
  }

  /** Token de la sesión, solo para que el backend asocie el error al usuario. */
  private currentToken(): string | null {
    try {
      const raw = localStorage.getItem('academia-account');
      return raw ? (JSON.parse(raw)?.token ?? null) : null;
    } catch {
      return null;
    }
  }
}
