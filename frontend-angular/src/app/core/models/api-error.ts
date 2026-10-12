import { HttpErrorResponse } from '@angular/common/http';

/**
 * Error normalizado que viaja desde el interceptor hacia los componentes.
 *
 * Extiende Error (por eso `err.message` sigue funcionando) pero además conserva
 * `status` y el body crudo en `error`, para que el código que ya lee
 * `err.error?.msg` no se rompa.
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly error: any,
    /** Errores de validación por campo, si el backend los envió. */
    public readonly fieldErrors: string[] = []
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/** Mensajes por defecto cuando el backend no manda `msg`. */
const STATUS_MESSAGES: Record<number, string> = {
  0: 'No se pudo conectar con el servidor. Verificá tu conexión.',
  400: 'Los datos enviados no son válidos.',
  401: 'Tu sesión expiró. Iniciá sesión nuevamente.',
  403: 'No tenés permisos para realizar esta acción.',
  404: 'No se encontró el recurso solicitado.',
  409: 'La operación entra en conflicto con el estado actual.',
  422: 'Los datos enviados no son válidos.',
  423: 'La cuenta está bloqueada temporalmente.',
  429: 'Demasiados intentos. Esperá unos segundos e intentá de nuevo.',
  500: 'Ocurrió un error interno. Intentá nuevamente más tarde.',
  502: 'El servidor no está disponible en este momento.',
  503: 'El servidor no está disponible en este momento.',
  504: 'El servidor tardó demasiado en responder.'
};

/**
 * Traduce cualquier HttpErrorResponse a un ApiError con mensaje legible.
 *
 * Contempla las formas que devuelve el back:
 *  - ExceptionMiddleware      -> { success: false, msg }
 *  - ModelState inválido      -> { success: false, msg, errors: string[] }
 *  - ProblemDetails de ASP.NET-> { title, detail, errors: { campo: string[] } }
 */
export function toApiError(res: HttpErrorResponse): ApiError {
  const body = res.error;
  const fieldErrors = extractFieldErrors(body);

  const message =
    firstString(body?.msg) ??
    firstString(body?.detail) ??
    firstString(body?.title) ??
    fieldErrors[0] ??
    (typeof body === 'string' && body.trim() ? body : undefined) ??
    STATUS_MESSAGES[res.status] ??
    'Ocurrió un error inesperado.';

  return new ApiError(message, res.status, body, fieldErrors);
}

function firstString(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value.trim() : undefined;
}

function extractFieldErrors(body: any): string[] {
  const errors = body?.errors;
  if (!errors) return [];
  // ModelState normalizado: string[]
  if (Array.isArray(errors)) return errors.filter((e: unknown) => typeof e === 'string');
  // ProblemDetails: { campo: string[] }
  if (typeof errors === 'object') {
    return Object.values(errors).flat().filter((e: unknown): e is string => typeof e === 'string');
  }
  return [];
}
