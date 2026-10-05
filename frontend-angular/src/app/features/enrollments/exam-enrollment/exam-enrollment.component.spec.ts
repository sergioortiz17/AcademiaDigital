import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ChangeDetectorRef } from '@angular/core';
import Swal from 'sweetalert2';
import { ExamEnrollmentComponent } from './exam-enrollment.component';

describe('Exam enrollment preview', () => {
  let component: ExamEnrollmentComponent;
  beforeEach(() => {
    component = new ExamEnrollmentComponent({ detectChanges: vi.fn() } as unknown as ChangeDetectorRef);
    vi.spyOn(Swal, 'fire').mockResolvedValue({ isConfirmed: true, isDenied: false, isDismissed: false });
  });
  afterEach(() => vi.restoreAllMocks());

  it('allows only one condition per subject, and permits deselecting it', () => {
    component.select('1-0', 'Regular');
    component.select('1-0', 'Libre');
    expect(component.selectedSubjects).toHaveLength(1);
    expect(component.selectedSubjects[0].condition).toBe('Libre');
    component.select('1-0', 'Libre');
    expect(component.selectedSubjects).toHaveLength(0);
  });

  it('requires a selection before displaying the preview', async () => {
    await component.submit();
    expect(component.validationMessage).not.toBe('');
    expect(Swal.fire).not.toHaveBeenCalled();
    expect(component.draft).toEqual([]);
  });

  it('keeps a snapshot of selections from different years after confirmation', async () => {
    component.select('1-0', 'Regular');
    component.select('3-0', 'Libre');
    await component.submit();
    component.select('1-0', 'Libre');
    expect(component.draft).toHaveLength(2);
    expect(component.draft[0].condition).toBe('Regular');
    expect(component.showMyEnrollments).toBe(true);
  });

  it('preserves selections and does not save when the preview is cancelled', async () => {
    vi.mocked(Swal.fire).mockResolvedValue({ isConfirmed: false, isDenied: false, isDismissed: true });
    component.select('2-0', 'Regular');
    await component.submit();
    expect(component.selectedSubjects).toHaveLength(1);
    expect(component.draft).toEqual([]);
    expect(component.showMyEnrollments).toBe(false);
  });
});
