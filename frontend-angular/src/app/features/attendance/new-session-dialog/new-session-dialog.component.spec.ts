import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { NewSessionDialogComponent } from './new-session-dialog.component';

describe('NewSessionDialogComponent date validation', () => {
  type Args = ConstructorParameters<typeof NewSessionDialogComponent>;
  const create = (data: Args[1]) => new NewSessionDialogComponent({ close: vi.fn() } as unknown as Args[0], data);

  beforeEach(() => { vi.useFakeTimers(); vi.setSystemTime(new Date(2026, 10, 21, 10, 0)); });
  afterEach(() => vi.useRealTimers());

  it('rejects a typed date older than 48 hours', () => {
    const dialog = create({ courseSectionId: 1 });
    dialog.scope = 'FullDay';
    dialog.sessionDate = new Date(2026, 10, 15);
    expect(dialog.dateError).toContain('48 horas');
    expect(dialog.isValid).toBe(false);
  });

  it('rejects future dates', () => {
    const dialog = create({ courseSectionId: 1 });
    dialog.sessionDate = new Date(2026, 10, 22);
    expect(dialog.dateError).toContain('futuros');
  });

  it('rejects days before the teacher assignment started', () => {
    const dialog = create({ courseSectionId: 1, assignmentStartedOn: '2026-11-21' });
    dialog.scope = 'FullDay';
    dialog.sessionDate = new Date(2026, 10, 20);
    expect(dialog.dateError).toContain('asignación empieza');
    dialog.sessionDate = new Date(2026, 10, 21);
    expect(dialog.isValid).toBe(true);
  });
});
