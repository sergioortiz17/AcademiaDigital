import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ChangeDetectorRef } from '@angular/core';
import { of, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { ExamEnrollmentComponent } from './exam-enrollment.component';
import { ExamTableService } from '../../../core/services/exam-table.service';
import { StudentService } from '../../../core/services/student.service';

const table = (id: number, courseId = id) => ({
  examTable: { id, courseId, courseName: `Materia ${courseId}`, courseCode: 'M', academicYear: 2099,
    callNumber: id, examDateUtc: '2099-12-30T10:00:00Z', registrationDeadlineUtc: '2099-12-29T10:00:00Z',
    location: 'Aula 1', status: 'Open' },
  canRegister: true, registrationId: null, attemptNumber: null, result: null
});

describe('Real exam enrollment integration', () => {
  let component: ExamEnrollmentComponent;
  let exams: { getMine: ReturnType<typeof vi.fn>; getAcademicEnrollments: ReturnType<typeof vi.fn>; register: ReturnType<typeof vi.fn> };
  let students: { getMyAcademicProgress: ReturnType<typeof vi.fn> };
  beforeEach(async () => {
    exams = {
      getMine: vi.fn().mockReturnValue(of([table(1), table(2)])),
      getAcademicEnrollments: vi.fn().mockReturnValue(of([
        { enrollmentId: 81, courseId: 1, status: 'Regularized', academicYear: 2025, semester: 1 },
        { enrollmentId: 82, courseId: 2, status: 'Regularized', academicYear: 2025, semester: 1 }
      ])),
      register: vi.fn().mockImplementation((id: number) => of({ id: id + 100, attemptNumber: 1 }))
    };
    students = { getMyAcademicProgress: vi.fn().mockReturnValue(of({ studentId: 9, careerName: 'Software', courses: [
      { courseId: 1, yearNumber: 1, enrollmentStatus: 'Regularized' },
      { courseId: 2, yearNumber: 2, enrollmentStatus: 'Regularized' }
    ] })) };
    component = new ExamEnrollmentComponent({ detectChanges: vi.fn() } as unknown as ChangeDetectorRef,
      exams as unknown as ExamTableService, students as unknown as StudentService);
    vi.spyOn(Swal, 'fire').mockResolvedValue({ isConfirmed: true, isDenied: false, isDismissed: false });
    await component.load();
  });
  afterEach(() => { component.ngOnDestroy(); vi.restoreAllMocks(); });

  it('resolves the student history and real course enrollment ids', () => {
    expect(exams.getAcademicEnrollments).toHaveBeenCalledWith(9);
    expect(component.years.map(group => group.year)).toEqual([1, 2]);
    expect(component.tables[0].enrollmentId).toBe(81);
  });

  it('posts the selected exam and enrollment ids only after confirmation', async () => {
    component.select(component.tables[0]);
    exams.getMine.mockReturnValue(of([{ ...table(1), registrationId: 101, canRegister: false }, table(2)]));
    await component.submit();
    expect(exams.register).toHaveBeenCalledExactlyOnceWith(1, 81);
    expect(component.registrations[0].registrationId).toBe(101);
    expect(component.selections.size).toBe(0);
  });

  it('does not register when confirmation is cancelled', async () => {
    vi.mocked(Swal.fire).mockResolvedValue({ isConfirmed: false, isDenied: false, isDismissed: true });
    component.select(component.tables[0]);
    await component.submit();
    expect(exams.register).not.toHaveBeenCalled();
    expect(component.selections.size).toBe(1);
    expect(component.submitting).toBe(false);
  });

  it('blocks empty submissions and ineligible courses', async () => {
    component.tables[0].enrollmentId = null;
    component.select(component.tables[0]);
    await component.submit();
    expect(exams.register).not.toHaveBeenCalled();
    expect(component.validationMessage).not.toBe('');
  });

  it('prevents duplicate sends while confirmation is open', async () => {
    component.select(component.tables[0]);
    const first = component.submit();
    await component.submit();
    await first;
    expect(exams.register).toHaveBeenCalledTimes(1);
  });

  it('retains failed selections and reports partial success', async () => {
    component.select(component.tables[0]);
    component.select(component.tables[1]);
    exams.register.mockImplementation((id: number) => id === 1
      ? of({ id: 101, attemptNumber: 1 }) : throwError(() => ({ error: { detail: 'Plazo vencido' } })));
    exams.getMine.mockReturnValue(of([{ ...table(1), registrationId: 101, canRegister: false }, table(2)]));
    await component.submit();
    expect(component.selections.has(1)).toBe(false);
    expect(component.selections.has(2)).toBe(true);
    expect(Swal.fire).toHaveBeenLastCalledWith(expect.objectContaining({
      icon: 'warning', text: expect.stringContaining('Plazo vencido')
    }));
  });

  it('blocks submission if loading failed', async () => {
    component.select(component.tables[0]);
    exams.getMine.mockReturnValue(throwError(() => new Error('Sin conexion')));
    await component.load();
    await component.submit();
    expect(component.loadError).toBe('Sin conexion');
    expect(exams.register).not.toHaveBeenCalled();
  });

  it('keeps only one call selected per course', async () => {
    exams.getMine.mockReturnValue(of([table(1), table(3, 1)]));
    await component.load();
    component.select(component.tables[0]);
    component.select(component.tables[1]);
    expect(component.selectedSubjects).toHaveLength(1);
    expect(component.selectedSubjects[0].examTable.id).toBe(3);
  });
});
