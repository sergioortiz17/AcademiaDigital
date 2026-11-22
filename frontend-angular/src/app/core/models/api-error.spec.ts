import { describe, expect, it } from 'vitest';
import { HttpErrorResponse } from '@angular/common/http';
import { toApiError } from './api-error';

describe('toApiError', () => {
  it('prefers field errors over the generic validation title', () => {
    const error = toApiError(new HttpErrorResponse({
      status: 400,
      error: { title: 'Se produjo uno o más errores de validación.', errors: { 'Grades[0].Score': ['La nota debe estar entre 0 y 100.'] } }
    }));
    expect(error.message).toBe('La nota debe estar entre 0 y 100.');
  });

  it('keeps the backend message when there are no field errors', () => {
    const error = toApiError(new HttpErrorResponse({ status: 409, error: { success: false, msg: 'La planilla ya existe.' } }));
    expect(error.message).toBe('La planilla ya existe.');
  });

  it('falls back to a readable message by status', () => {
    expect(toApiError(new HttpErrorResponse({ status: 0 })).message).toContain('No se pudo conectar');
  });
});
