import { ChangeDetectorRef } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { CareerService } from '../../../core/services/career.service';
import { CourseService } from '../../../core/services/course.service';
import { Subject, SubjectService } from '../../../core/services/subject.service';
import { ExamFormManagementComponent } from './exam-form-management.component';

const subject = (id: number): Subject => ({ id, courseId: id + 10, courseName: 'Materia ' + id,
  courseCode: 'M', studyPlanId: 2, yearNumber: 1, semester: 1, sortOrder: id, isAnnual: true,
  isMandatory: false, credits: 5, workloadHours: 80, courseType: null, approvalRule: null });
describe('Admin official study plan integration', () => {
  let component: ExamFormManagementComponent;
  let server: Subject[];
  let api: { getSubjectsByCareer: ReturnType<typeof vi.fn>; addPlanCourse: ReturnType<typeof vi.fn>;
    updatePlanCourse: ReturnType<typeof vi.fn>; removePlanCourse: ReturnType<typeof vi.fn> };
  beforeEach(async () => {
    server = [subject(1), subject(2)];
    api = {
      getSubjectsByCareer: vi.fn().mockImplementation(() => of(structuredClone(server))),
      updatePlanCourse: vi.fn().mockImplementation((_plan, id, input) => {
        Object.assign(server.find(row => row.id === id)!, input); return of(undefined);
      }),
      removePlanCourse: vi.fn().mockImplementation((_plan, id) => { server = server.filter(row => row.id !== id); return of(undefined); }),
      addPlanCourse: vi.fn().mockImplementation((_plan, input) => {
        const added = { ...subject(3), ...input }; server.push(added); return of(added);
      })
    };
    component = new ExamFormManagementComponent({} as CareerService, api as unknown as SubjectService,
      { detectChanges: vi.fn() } as unknown as ChangeDetectorRef, {} as CourseService);
    component.careerId = 1; component.planId = 2;
    component.catalog = [{ id: 13, careerId: 1, name: 'Materia 3', code: 'M3', isActive: true, description: null }];
    vi.spyOn(Swal, 'fire').mockResolvedValue({ isConfirmed: true, isDenied: false, isDismissed: false });
    await component.changePlan();
  });
  afterEach(() => { component.ngOnDestroy(); vi.restoreAllMocks(); });
  it('updates by study-plan-course id and preserves untouched metadata', async () => {
    component.rows[0].year = 2; await component.save();
    expect(api.updatePlanCourse).toHaveBeenCalledExactlyOnceWith(2, 1, expect.objectContaining({
      courseId: 11, yearNumber: 2, credits: 5, workloadHours: 80, isMandatory: false, courseTypeId: null
    }));
    expect(component.dirty).toBe(false);
  });
  it('creates catalog subjects and deletes removed plan rows', async () => {
    component.newCourseId = 13; component.add(); component.remove(component.rows[0]); await component.save();
    expect(api.addPlanCourse).toHaveBeenCalledWith(2, expect.objectContaining({ courseId: 13 }));
    expect(api.removePlanCourse).toHaveBeenCalledWith(2, 1);
    expect(component.rows.map(row => row.courseId)).toEqual([12, 13]);
  });
  it('does not write when confirmation is cancelled', async () => {
    vi.mocked(Swal.fire).mockResolvedValue({ isConfirmed: false, isDenied: false, isDismissed: true });
    component.rows[0].year = 2; await component.save();
    expect(api.updatePlanCourse).not.toHaveBeenCalled(); expect(component.dirty).toBe(true);
  });
  it('blocks changes when a course type id cannot be preserved', async () => {
    server[0].courseType = 'Practica'; await component.changePlan();
    component.rows[0].year = 2; await component.save();
    expect(api.updatePlanCourse).not.toHaveBeenCalled(); expect(component.error).not.toBe('');
  });
  it('detects concurrent server changes before writing', async () => {
    component.rows[0].year = 2; server[0].credits = 10; await component.save();
    expect(api.updatePlanCourse).not.toHaveBeenCalled(); expect(component.requiresReload).toBe(true);
  });
  it('keeps failed changes pending and does not repeat successful writes on retry', async () => {
    component.rows[0].year = 2; component.remove(component.rows[1]);
    api.removePlanCourse.mockReturnValue(throwError(() => new Error('Materia en uso')));
    await component.save();
    expect(component.error).toContain('Materia en uso'); expect(component.dirty).toBe(true);
    await component.save(); expect(api.updatePlanCourse).toHaveBeenCalledTimes(1);
  });
  it('blocks duplicate submissions', async () => {
    component.rows[0].year = 2; await Promise.all([component.save(), component.save()]);
    expect(api.updatePlanCourse).toHaveBeenCalledTimes(1);
  });
});
