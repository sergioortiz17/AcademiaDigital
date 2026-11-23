import { describe, expect, it } from 'vitest';
import { isDemoUser } from './demo-user';

describe('isDemoUser', () => {
  it('solo acepta cuentas del dominio de demo', () => {
    expect(isDemoUser('alumno@demo.com')).toBe(true);
    expect(isDemoUser('  Alumno@DEMO.com ')).toBe(true);
    expect(isDemoUser('alumno@academiadigital.local')).toBe(false);
    expect(isDemoUser('alumno@demo.com.ar')).toBe(false);
    expect(isDemoUser('demo.com@gmail.com')).toBe(false);
    expect(isDemoUser(null)).toBe(false);
  });
});
