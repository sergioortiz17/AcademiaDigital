import { describe, expect, it } from 'vitest';
import { parseGradeInput } from './grades.component';

describe('parseGradeInput', () => {
  it('accepts decimals with comma or dot', () => {
    expect(parseGradeInput('7,5', 10)).toEqual({ kind: 'score', score: 7.5 });
    expect(parseGradeInput('8.25', 10)).toEqual({ kind: 'score', score: 8.25 });
  });

  it('reads A as absent and blank as empty', () => {
    expect(parseGradeInput('a', 10)).toEqual({ kind: 'absent' });
    expect(parseGradeInput('Ausente', 10)).toEqual({ kind: 'absent' });
    expect(parseGradeInput('  ', 10)).toEqual({ kind: 'empty' });
  });

  it('rejects grades out of range or that are not numbers', () => {
    expect(parseGradeInput('11', 10).kind).toBe('invalid');
    expect(parseGradeInput('7,555', 10).kind).toBe('invalid');
    expect(parseGradeInput('siete', 10).kind).toBe('invalid');
  });
});
