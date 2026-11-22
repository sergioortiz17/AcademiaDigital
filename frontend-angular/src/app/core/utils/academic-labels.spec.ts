import { describe, expect, it } from 'vitest';
import { courseDisplayName, mostFrequentId, periodLabel } from './academic-labels';

describe('academic labels', () => {
  it('labels annual and four-month courses', () => {
    expect(periodLabel(1, true)).toBe('Anual');
    expect(periodLabel(1)).toBe('1.er cuatrimestre');
    expect(periodLabel(2, false)).toBe('2.º cuatrimestre');
  });

  it('shows the course name with its period and without the code', () => {
    expect(courseDisplayName('Base de datos', 1, true)).toBe('Base de datos · Anual');
    expect(courseDisplayName('Competencias Comunicacionales I', 2)).toBe('Competencias Comunicacionales I · 2.º cuatrimestre');
  });

  it('picks the most frequent id ignoring nulls', () => {
    expect(mostFrequentId([3, null, 5, 3])).toBe(3);
    expect(mostFrequentId([null])).toBeNull();
  });
});
