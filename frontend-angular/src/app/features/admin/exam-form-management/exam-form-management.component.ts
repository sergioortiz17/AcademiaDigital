import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, forkJoin, Observable, Subject as DestroySubject, takeUntil, timeout } from 'rxjs';
import Swal from 'sweetalert2';
import { Career, CareerService } from '../../../core/services/career.service';
import { Course, CourseService } from '../../../core/services/course.service';
import { PlanCourseInput, StudyPlan, Subject, SubjectService } from '../../../core/services/subject.service';

export interface FormRow { courseId: number; name: string; year: number; semester: number; order: number; }
@Component({
  selector: 'app-exam-form-management', standalone: true, imports: [CommonModule, FormsModule],
  templateUrl: './exam-form-management.component.html', styleUrl: './exam-form-management.component.scss'
})
export class ExamFormManagementComponent implements OnInit, OnDestroy {
  careers: Career[] = [];
  plans: StudyPlan[] = [];
  subjects: Subject[] = [];
  catalog: Course[] = [];
  careerId = 0;
  planId = 0;
  rows: FormRow[] = [];
  saved: FormRow[] = [];
  editingYears = new Set<number>();
  adding = false;
  newCourseId = 0;
  newYear = 1;
  newSemester = 1;
  loading = false;
  saving = false;
  requiresReload = false;
  error = '';
  message = '';
  private readonly destroy$ = new DestroySubject<void>();
  private destroyed = false;
  constructor(private readonly careersApi: CareerService, private readonly subjectsApi: SubjectService,
    private readonly cdr: ChangeDetectorRef, private readonly coursesApi: CourseService) {}
  ngOnInit(): void { void this.loadCareers(); }
  ngOnDestroy(): void { this.destroyed = true; this.destroy$.next(); this.destroy$.complete(); }
  get career(): Career | undefined { return this.careers.find(item => item.id === this.careerId); }
  get plan(): StudyPlan | undefined { return this.plans.find(item => item.id === this.planId); }
  get years(): number[] { return [...new Set(this.rows.map(row => row.year))].sort((a, b) => a - b); }
  get availableSubjects(): Course[] { return this.catalog.filter(item => item.isActive && !this.rows.some(row => row.courseId === item.id)); }
  get hasSoftwarePlan(): boolean { return /desarrollo de software/i.test(this.career?.name ?? '') && /2023/.test(this.plan?.name ?? ''); }
  get dirty(): boolean { return JSON.stringify(this.rows) !== JSON.stringify(this.saved); }
  rowsFor(year: number): FormRow[] { return this.rows.filter(row => row.year === year); }
  canUpdate(row: FormRow): boolean {
    const source = this.subjects.find(item => item.courseId === row.courseId);
    return !source?.courseType || source.courseTypeId != null;
  }
  private mapRows(items: Subject[]): FormRow[] {
    return items.map(item => ({ courseId: item.courseId, name: item.courseName,
      year: item.yearNumber, semester: item.semester, order: item.sortOrder }));
  }
  private async fetchSubjects(): Promise<Subject[]> {
    return firstValueFrom(this.subjectsApi.getSubjectsByCareer(this.planId).pipe(timeout(15000), takeUntil(this.destroy$)));
  }
  async loadCareers(): Promise<void> {
    await this.run(async () => { this.careers = await firstValueFrom(this.careersApi.getCareers().pipe(timeout(15000), takeUntil(this.destroy$))); });
  }
  async changeCareer(): Promise<void> {
    this.planId = 0; this.plans = []; this.rows = []; this.saved = []; this.subjects = []; this.catalog = []; this.editingYears.clear(); this.adding = false;
    if (!this.careerId) return;
    await this.run(async () => {
      const data = await firstValueFrom(forkJoin({ plans: this.subjectsApi.getStudyPlansByCareer(this.careerId),
        catalog: this.coursesApi.getCoursesByCareer(this.careerId) }).pipe(timeout(15000), takeUntil(this.destroy$)));
      this.plans = data.plans; this.catalog = data.catalog;
    });
  }
  async changePlan(): Promise<void> {
    this.rows = []; this.saved = []; this.subjects = []; this.editingYears.clear(); this.adding = false;
    this.requiresReload = false;
    if (!this.planId) return;
    await this.run(async () => {
      this.subjects = await this.fetchSubjects();
      this.rows = this.mapRows(this.subjects); this.saved = structuredClone(this.rows);
    });
  }
  remove(row: FormRow): void { if (!this.saving) { this.rows = this.rows.filter(item => item !== row); this.message = ''; } }
  add(): void {
    if (this.saving) return;
    const course = this.availableSubjects.find(item => item.id === this.newCourseId);
    if (!course || !Number.isInteger(this.newYear) || this.newYear < 1 || this.newYear > 20 || ![1, 2].includes(this.newSemester)) return;
    const original = this.saved.find(row => row.courseId === course.id);
    this.rows.push(original ? { ...original } : { courseId: course.id, name: course.name, year: this.newYear,
      semester: this.newSemester, order: Math.max(0, ...this.rows.map(row => row.order)) + 1 });
    this.editingYears.add(original?.year ?? this.newYear); this.adding = false; this.newCourseId = 0; this.message = '';
  }
  cancel(): void { if (!this.saving) { this.rows = structuredClone(this.saved); this.editingYears.clear(); this.adding = false; this.error = ''; this.message = ''; } }
  private input(row: FormRow): PlanCourseInput {
    const original = this.subjects.find(item => item.courseId === row.courseId);
    return { courseId: row.courseId, yearNumber: row.year, semester: row.semester, sortOrder: row.order,
      courseTypeId: original?.courseTypeId ?? null, isMandatory: original?.isMandatory ?? true,
      credits: original?.credits ?? null, workloadHours: original?.workloadHours ?? null };
  }
  async save(): Promise<void> {
    if (this.saving || this.loading || this.requiresReload || !this.planId || !this.dirty) return;
    this.error = ''; this.message = '';
    if (!this.rows.length || new Set(this.rows.map(row => row.courseId)).size !== this.rows.length ||
      this.rows.some(row => !Number.isInteger(row.year) || row.year < 1 || row.year > 20 ||
        ![1, 2].includes(row.semester) || !Number.isInteger(row.order) || row.order < 0)) {
      this.error = 'Revisá año (1 a 20), semestre (1 o 2) y orden (entero no negativo). Debe quedar al menos una materia.'; return;
    }
    const desired = structuredClone(this.rows);
    const removed = this.saved.filter(row => !desired.some(item => item.courseId === row.courseId));
    const changed = desired.filter(row => JSON.stringify(row) !== JSON.stringify(this.saved.find(item => item.courseId === row.courseId)));
    if (changed.some(row => !this.canUpdate(row))) {
      this.error = 'No se puede conservar el tipo de una materia con el contrato actual. Esa fila permanece sin editar.'; return;
    }
    this.saving = true;
    try {
      const confirmation = await Swal.fire({ icon: 'warning', titleText: 'Guardar cambios del plan académico',
        text: `Se guardarán ${changed.length} altas o modificaciones y se quitarán ${removed.length} materias del plan ${this.plan?.name ?? ''}. Estos cambios afectan al plan oficial.`,
        showCancelButton: true, confirmButtonText: 'Guardar cambios', cancelButtonText: 'Volver', confirmButtonColor: '#00579c', heightAuto: false });
      if (!confirmation.isConfirmed || this.destroyed) return;
      // Confirmar que el plan no cambio desde que se abrio el editor.
      const latest = await this.fetchSubjects();
      if (JSON.stringify(latest) !== JSON.stringify(this.subjects)) {
        this.requiresReload = true;
        this.error = 'El plan cambió en el servidor. Recargá las materias y revisá los cambios antes de guardar.'; return;
      }
      const failures: string[] = [];
      let successes = 0;
      for (const row of [...changed, ...removed]) {
        if (this.destroyed) return;
        const source = this.subjects.find(item => item.courseId === row.courseId);
        const request: Observable<unknown> = removed.includes(row) ? this.subjectsApi.removePlanCourse(this.planId, source!.id)
          : source ? this.subjectsApi.updatePlanCourse(this.planId, source.id, this.input(row))
          : this.subjectsApi.addPlanCourse(this.planId, this.input(row));
        try { await firstValueFrom(request.pipe(timeout(20000), takeUntil(this.destroy$))); successes++; }
        catch (error) { failures.push(`${row.name}: ${this.errorText(error)}`); }
      }
      if (this.destroyed) return;
      try {
        this.subjects = await this.fetchSubjects();
        this.saved = this.mapRows(this.subjects);
        this.rows = failures.length ? desired : structuredClone(this.saved);
        if (!failures.length) { this.editingYears.clear(); this.adding = false; }
      } catch {
        this.requiresReload = true;
        failures.push('No se pudo verificar el estado final. Recargá las materias antes de volver a guardar.');
      }
      this.message = `${successes} cambio(s) confirmado(s) por el servidor.`;
      this.error = failures.join(' ');
    } catch (error) { this.error = this.errorText(error); }
    finally { if (!this.destroyed) { this.saving = false; this.cdr.detectChanges(); } }
  }
  private errorText(error: unknown): string {
    const value = error as { error?: { detail?: string; msg?: string }; message?: string; name?: string };
    if (value?.name === 'TimeoutError') return 'No se pudo confirmar la operación a tiempo; revisá el estado del plan.';
    return value?.error?.detail || value?.error?.msg || value?.message || 'No se pudo completar la operación.';
  }
  private async run(action: () => Promise<void>): Promise<void> {
    this.loading = true; this.error = ''; this.message = '';
    try { await action(); }
    catch (error) { if (!this.destroyed) this.error = this.errorText(error); }
    finally { if (!this.destroyed) { this.loading = false; this.cdr.detectChanges(); } }
  }
}
