import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import {
  EnrollmentService,
  EnrollmentPeriodDto,
  OpenPeriodRequest,
  PeriodCommissionCoverageDto,
  CommissionCoverageGap
} from '../../../core/services/enrollment. service';
import { CareerService, Career } from '../../../core/services/career.service';
import { SubjectService, StudyPlan } from '../../../core/services/subject.service';

@Component({
  selector: 'app-enrollment-management',
  templateUrl: './enrollment-management.component.html',
  styleUrls: ['./enrollment-management.component.scss'],
  standalone: false
})
export class EnrollmentManagementComponent implements OnInit {
  periods: EnrollmentPeriodDto[] = [];
  careers: Career[] = [];
  studyPlans: StudyPlan[] = [];

  showOpenForm = false;
  openingForm: OpenPeriodRequest = {
    careerId: 0,
    studyPlanId: 0,
    academicYear: new Date().getFullYear(),
    semester: 1,
    quotasMorning: 0,
    quotasAfternoon: 0,
    quotasEvening: 0
  };


  editingPeriod: EnrollmentPeriodDto | null = null;
  editQuotas = { quotasMorning: 0, quotasAfternoon: 0, quotasEvening: 0 };

  // Parte 11: cobertura de comisiones por período (aviso persistente). Solo se consultan los activos.
  coverageByPeriod: Record<number, CommissionCoverageGap[]> = {};
  private readonly shiftLabels: Record<string, string> = { 'Mañana': 'Mañana', 'Tarde': 'Tarde', 'Noche': 'Noche' };

  isSubmitting = false;
  loadingPeriods = false;
  errorMsg = '';
  successMsg = '';

  constructor(
    private readonly enrollmentService: EnrollmentService,
    private readonly careerService: CareerService,
    private readonly subjectService: SubjectService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPeriods();
    this.careerService.getCareers().subscribe({
      next: c => {
        this.careers = c;
        this.cdr.detectChanges();
      }
    });
  }

  loadPeriods(): void {
    this.loadingPeriods = true;
    this.enrollmentService.getAllPeriods().subscribe({
      next: res => {
        this.periods = res.data;
        this.loadingPeriods = false;
        this.cdr.detectChanges();
        // Aviso persistente: consultar cobertura de comisiones de cada período ACTIVO.
        this.coverageByPeriod = {};
        this.periods.filter(p => p.isActive).forEach(p => this.loadCoverage(p.id));
      },
      error: err => {
        console.error(err);
        this.loadingPeriods = false;
        this.cdr.detectChanges();
      }
    });
  }

  /** Consulta (read-only) qué comisiones faltan para un período y guarda los faltantes. */
  private loadCoverage(periodId: number): void {
    this.enrollmentService.getCommissionCoverage(periodId).subscribe({
      next: res => {
        this.coverageByPeriod[periodId] = res.data.gaps;
        this.cdr.detectChanges();
      },
      error: () => { /* el aviso es best-effort; si falla, no rompe la pantalla */ }
    });
  }

  gapsFor(periodId: number): CommissionCoverageGap[] {
    return this.coverageByPeriod[periodId] ?? [];
  }

  shiftLabel(shift: string): string {
    return this.shiftLabels[shift] ?? shift;
  }

  /** Atajo al alta de comisiones (Parte 7), precargando carrera + año del período. */
  goToCreateCommission(period: EnrollmentPeriodDto): void {
    this.router.navigate(['/app/admin/commissions'], {
      queryParams: { careerId: period.careerId, academicYear: period.academicYear }
    });
  }

  onCareerChange(): void {
    this.openingForm.studyPlanId = 0;
    this.studyPlans = [];
    if (!this.openingForm.careerId) return;
    this.subjectService.getStudyPlansByCareer(this.openingForm.careerId).subscribe({
      next: plans => {
        this.studyPlans = plans.filter(p => p.isActive);
        this.cdr.detectChanges();
      }
    });
  }

  canOpen(): boolean {
    return (
      this.openingForm.careerId > 0 &&
      this.openingForm.studyPlanId > 0 &&
      this.openingForm.academicYear > 2000 &&
      (this.openingForm.semester === 1 || this.openingForm.semester === 2) &&
      (this.openingForm.quotasAfternoon >= 0 && this.openingForm.quotasEvening >= 0)
    );
  }

  submitOpen(): void {
    if (!this.canOpen()) return;
    this.isSubmitting = true;
    this.errorMsg = '';
    this.enrollmentService.openPeriod(this.openingForm).subscribe({
      next: (res) => {
        this.showOpenForm = false;
        this.resetForm();
        this.isSubmitting = false;
        this.successMsg = 'Período de inscripción activado correctamente.';
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        this.loadPeriods();
        // Chequeo inmediato: avisar en el momento si el período recién abierto ya tiene faltantes.
        if (res?.data?.id) this.checkCoverageNow(res.data);
        this.cdr.detectChanges();
      },
      error: err => {
        this.errorMsg = err.error?.msg || err.error?.title || 'No se pudo abrir el período.';
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  closePeriod(period: EnrollmentPeriodDto): void {
    if (!confirm(`¿Cerrar el período de inscripción de ${period.careerName}?`)) return;
    this.enrollmentService.closePeriod(period.id).subscribe({
      next: () => {
        const idx = this.periods.findIndex(p => p.id === period.id);
        if (idx > -1) this.periods[idx] = { ...this.periods[idx], isActive: false };
        this.cdr.detectChanges();
      },
      error: err => alert(err.error?.msg || 'No se pudo cerrar el período.')
    });
  }

  viewStudents(period: EnrollmentPeriodDto): void {
    this.router.navigate(['/app/admin/enrollments', period.id, 'students']);
  }

  viewReports(period: EnrollmentPeriodDto): void {
    this.router.navigate(['/app/admin/enrollments', period.id, 'reports']);
  }

  activatePeriod(period: EnrollmentPeriodDto): void {
    if (!confirm(`¿Reactivar el período de inscripción de ${period.careerName}?`)) return;
    this.enrollmentService.activatePeriod(period.id).subscribe({
      next: () => {
        const idx = this.periods.findIndex(p => p.id === period.id);
        if (idx > -1) this.periods[idx] = { ...this.periods[idx], isActive: true, endDate: null };
        this.loadCoverage(period.id);
        this.checkCoverageNow(period);
        this.cdr.detectChanges();
      },
      error: err => alert(err.message || 'No se pudo activar el período.')
    });
  }

  /**
   * Chequeo inmediato al abrir/activar: consulta la cobertura y, si faltan comisiones, muestra un
   * aviso puntual en el momento (además del banner persistente por período).
   */
  private checkCoverageNow(period: EnrollmentPeriodDto): void {
    this.enrollmentService.getCommissionCoverage(period.id).subscribe({
      next: res => {
        this.coverageByPeriod[period.id] = res.data.gaps;
        if (res.data.gaps.length > 0) {
          const detalle = res.data.gaps.map(g => `${g.yearNumber}° año / ${this.shiftLabel(g.shift)}`).join(', ');
          this.errorMsg = `⚠️ Faltan comisiones para: ${detalle}. Los alumnos que se inscriban en esos turnos ` +
            `van a quedar sin comisión hasta que las crees o los asignes a mano.`;
        }
        this.cdr.detectChanges();
      },
      error: () => { /* best-effort */ }
    });
  }

  deletePeriod(period: EnrollmentPeriodDto): void {
    if (!confirm(`¿Eliminar el período de inscripción de ${period.careerName} ${period.academicYear} - ${this.semesterLabel(period.semester)}?\n\nEsta acción no se puede deshacer.`)) return;
    this.enrollmentService.deletePeriod(period.id).subscribe({
      next: () => {
        this.periods = this.periods.filter(p => p.id !== period.id);
        this.cdr.detectChanges();
      },
      error: err => alert(err.message || 'No se pudo eliminar el período.')
    });
  }

  startEdit(period: EnrollmentPeriodDto): void {
    this.editingPeriod = period;
    this.editQuotas = {
      quotasMorning: period.quotasMorning,
      quotasAfternoon: period.quotasAfternoon,
      quotasEvening: period.quotasEvening
    };
  }

  cancelEdit(): void {
    this.editingPeriod = null;
  }

  saveQuotas(): void {
    if (!this.editingPeriod) return;
    this.enrollmentService.updateQuotas(
      this.editingPeriod.id,
      this.editQuotas.quotasMorning,
      this.editQuotas.quotasAfternoon,
      this.editQuotas.quotasEvening
    ).subscribe({
      next: res => {
        const idx = this.periods.findIndex(p => p.id === res.data.id);
        if (idx > -1) this.periods[idx] = res.data;
        this.editingPeriod = null;
        this.cdr.detectChanges();
      },
      error: err => alert(err.error?.msg || 'No se pudo actualizar los cupos.')
    });
  }

  resetForm(): void {
    this.openingForm = {
      careerId: 0,
      studyPlanId: 0,
      academicYear: new Date().getFullYear(),
      semester: 1,
      quotasMorning: 0,
      quotasAfternoon: 0,
      quotasEvening: 0
    };
    this.studyPlans = [];
  }

  semesterLabel(s: number): string {
    return s === 1 ? '1° Semestre' : '2° Semestre';
  }
}
