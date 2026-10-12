import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest';
import { NEVER, of, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { EnrollmentManagementComponent } from './enrollment-management.component';

describe('Enrollment activation feedback', () => {
  let component: EnrollmentManagementComponent;
  const period = { id: 7, careerName: 'Desarrollo de Software' };
  let service: { openPeriod: ReturnType<typeof vi.fn>; getCommissionCoverage: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    service = {
      openPeriod: vi.fn().mockReturnValue(of({ success: true, data: period })),
      getCommissionCoverage: vi.fn().mockReturnValue(of({ success: true, data: { gaps: [] } }))
    };
    type Args = ConstructorParameters<typeof EnrollmentManagementComponent>;
    component = new EnrollmentManagementComponent(
      service as unknown as Args[0], {} as Args[1], {} as Args[2],
      {} as Args[3], {} as Args[4], {} as Args[5], { detectChanges: vi.fn() } as unknown as Args[6]
    );
    component.openingForm.careerId = 1;
    component.openingForm.studyPlanId = 1;
    vi.spyOn(component, 'loadPeriods').mockImplementation(() => {});
    vi.spyOn(Swal, 'fire').mockResolvedValue({ isConfirmed: true, isDenied: false, isDismissed: false });
  });

  afterEach(() => { vi.restoreAllMocks(); vi.useRealTimers(); });

  it('confirms activation when no divisions are missing', () => {
    component.submitOpen();
    expect(Swal.fire).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ icon: 'success' }));
    expect(component.isSubmitting).toBe(false);
  });

  it('warns with the affected shift when divisions are missing', () => {
    service.getCommissionCoverage.mockReturnValue(of({ success: true, data: { gaps: [{ yearNumber: 2, shift: 'Noche' }] } }));
    component.submitOpen();
    expect(Swal.fire).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({
      icon: 'warning', titleText: 'Faltan divisiones', text: expect.stringContaining('Noche')
    }));
  });

  it('reports an unavailable coverage check without suggesting activation failed', () => {
    service.getCommissionCoverage.mockReturnValue(throwError(() => new Error('Network')));
    component.submitOpen();
    expect(Swal.fire).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({
      icon: 'warning', text: expect.stringContaining('no se pudo verificar')
    }));
  });

  it('reports a coverage request that never responds', () => {
    vi.useFakeTimers();
    service.getCommissionCoverage.mockReturnValue(NEVER);
    component.submitOpen();
    vi.advanceTimersByTime(10000);
    expect(Swal.fire).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ icon: 'warning' }));
  });

  it('still confirms activation when its response has no period id', () => {
    service.openPeriod.mockReturnValue(of({ success: true, data: {} }));
    component.submitOpen();
    expect(Swal.fire).toHaveBeenCalledTimes(1);
    expect(service.getCommissionCoverage).not.toHaveBeenCalled();
  });
  it('shows creation errors in a dialog and preserves the form for retry', () => {
    component.showOpenForm = true;
    service.openPeriod.mockReturnValue(throwError(() => ({ error: { msg: 'El periodo ya existe.' } })));
    component.submitOpen();
    expect(Swal.fire).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({
      icon: 'error', text: 'El periodo ya existe.'
    }));
    expect(component.showOpenForm).toBe(true);
    expect(component.openingForm.careerId).toBe(1);
    expect(component.isSubmitting).toBe(false);
    expect(service.getCommissionCoverage).not.toHaveBeenCalled();
  });

});
