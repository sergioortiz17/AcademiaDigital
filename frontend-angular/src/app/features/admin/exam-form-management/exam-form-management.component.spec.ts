import { ChangeDetectorRef } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';
import { CareerService } from '../../../core/services/career.service';
import { SubjectService } from '../../../core/services/subject.service';
import { ExamFormManagementComponent } from './exam-form-management.component';

describe('Admin exam form draft', () => {
  let component: ExamFormManagementComponent;
  beforeEach(async () => {
    localStorage.clear();
    component = new ExamFormManagementComponent({} as CareerService, {
      getSubjectsByCareer: () => of([{ courseId: 11, courseName: 'Programacion', yearNumber: 1 }])
    } as unknown as SubjectService, { detectChanges: vi.fn() } as unknown as ChangeDetectorRef);
    component.careerId = 1; component.planId = 2;
    await component.changePlan();
  });
  afterEach(() => { component.ngOnDestroy(); vi.restoreAllMocks(); localStorage.clear(); });

  it('saves and reloads a draft for the chosen plan', async () => {
    component.rows[0].name = 'Programacion I';
    component.save();
    await component.changePlan();
    expect(component.rows[0].name).toBe('Programacion I');
    expect(component.dirty).toBe(false);
    component.planId = 3;
    await component.changePlan();
    expect(component.rows[0].name).toBe('Programacion');
  });
  it('cancels unsaved changes', () => {
    component.rows[0].name = 'Cambio'; component.cancel();
    expect(component.rows[0].name).toBe('Programacion');
  });
  it('rejects an empty label without saving', () => {
    component.rows[0].name = ' ';
    component.save();
    expect(component.error).not.toBe('');
    expect(localStorage.getItem(component.storageKey)).toBeNull();
  });
  it('allows removing and readding a plan subject without duplicates', () => {
    component.remove(component.rows[0]);
    component.newCourseId = 11; component.newYear = 2; component.add(); component.add();
    expect(component.rows).toHaveLength(1);
    expect(component.rows[0].year).toBe(2);
  });
  it('retains unsaved edits if browser storage fails', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('Full'); });
    component.rows[0].name = 'Cambio'; component.save();
    expect(component.error).not.toBe('');
    expect(component.dirty).toBe(true);
    expect(component.message).toBe('');
  });
});
