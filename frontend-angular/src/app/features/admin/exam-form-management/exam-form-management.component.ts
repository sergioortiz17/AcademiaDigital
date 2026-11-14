import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, Subject as DestroySubject, takeUntil, timeout } from 'rxjs';
import { Career, CareerService } from '../../../core/services/career.service';
import { StudyPlan, Subject, SubjectService } from '../../../core/services/subject.service';

export interface FormRow { courseId: number; name: string; year: number; regular: boolean; }
@Component({
  selector: 'app-exam-form-management', standalone: true, imports: [CommonModule, FormsModule],
  templateUrl: './exam-form-management.component.html', styleUrl: './exam-form-management.component.scss'
})
export class ExamFormManagementComponent implements OnInit, OnDestroy {
  careers: Career[] = [];
  plans: StudyPlan[] = [];
  subjects: Subject[] = [];
  careerId = 0;
  planId = 0;
  rows: FormRow[] = [];
  saved: FormRow[] = [];
  editingYears = new Set<number>();
  adding = false;
  newCourseId = 0;
  newYear = 1;
  loading = false;
  error = '';
  message = '';
  private readonly destroy$ = new DestroySubject<void>();
  private destroyed = false;
  constructor(private readonly careersApi: CareerService, private readonly subjectsApi: SubjectService,
    private readonly cdr: ChangeDetectorRef) {}
  ngOnInit(): void { void this.loadCareers(); }
  ngOnDestroy(): void { this.destroyed = true; this.destroy$.next(); this.destroy$.complete(); }
  get career(): Career | undefined { return this.careers.find(item => item.id === this.careerId); }
  get plan(): StudyPlan | undefined { return this.plans.find(item => item.id === this.planId); }
  get years(): number[] { return [...new Set(this.rows.map(row => row.year))].sort((a, b) => a - b); }
  get availableSubjects(): Subject[] { return this.subjects.filter(item => !this.rows.some(row => row.courseId === item.courseId)); }
  get hasSoftwarePlan(): boolean { return /desarrollo de software/i.test(this.career?.name ?? '') && /2023/.test(this.plan?.name ?? ''); }
  get dirty(): boolean { return JSON.stringify(this.rows) !== JSON.stringify(this.saved); }
  get storageKey(): string { return `academia:exam-form:v1:${this.careerId}:${this.planId}`; }
  rowsFor(year: number): FormRow[] { return this.rows.filter(row => row.year === year); }

  async loadCareers(): Promise<void> {
    await this.run(async () => { this.careers = await firstValueFrom(this.careersApi.getCareers().pipe(timeout(15000), takeUntil(this.destroy$))); });
  }
  async changeCareer(): Promise<void> {
    this.planId = 0; this.plans = []; this.rows = []; this.saved = []; this.subjects = []; this.editingYears.clear(); this.adding = false;
    if (!this.careerId) return;
    await this.run(async () => { this.plans = await firstValueFrom(this.subjectsApi.getStudyPlansByCareer(this.careerId).pipe(timeout(15000), takeUntil(this.destroy$))); });
  }
  async changePlan(): Promise<void> {
    this.rows = []; this.saved = []; this.subjects = []; this.editingYears.clear(); this.adding = false;
    if (!this.planId) return;
    await this.run(async () => {
      this.subjects = await firstValueFrom(this.subjectsApi.getSubjectsByCareer(this.planId).pipe(timeout(15000), takeUntil(this.destroy$)));
      this.rows = this.subjects.map(item => ({ courseId: item.courseId, name: item.courseName, year: item.yearNumber, regular: true }));
      try {
        const raw = localStorage.getItem(this.storageKey);
        if (raw) {
          const parsed: unknown = JSON.parse(raw);
          if (this.validDraft(parsed)) this.rows = parsed;
          else this.error = 'El borrador guardado no es compatible con este plan. Se cargaron las materias originales.';
        }
      } catch { this.error = 'No se pudo leer el borrador local. Se cargaron las materias del plan.'; }
      this.saved = structuredClone(this.rows);
    });
  }
  private validDraft(value: unknown): value is FormRow[] {
    return Array.isArray(value) && value.length > 0 && value.every(row => row &&
      this.subjects.some(subject => subject.courseId === row.courseId) && typeof row.name === 'string' && !!row.name.trim() &&
      row.name.length <= 200 && Number.isInteger(row.year) && row.year >= 1 && row.year <= 20 && typeof row.regular === 'boolean') &&
      new Set(value.map(row => row.courseId)).size === value.length;
  }
  remove(row: FormRow): void { this.rows = this.rows.filter(item => item !== row); this.message = ''; }
  add(): void {
    const subject = this.availableSubjects.find(item => item.courseId === this.newCourseId);
    if (!subject || !Number.isInteger(this.newYear) || this.newYear < 1 || this.newYear > 20) return;
    this.rows.push({ courseId: subject.courseId, name: subject.courseName, year: this.newYear, regular: true });
    this.editingYears.add(this.newYear); this.adding = false; this.newCourseId = 0; this.message = '';
  }
  cancel(): void { this.rows = structuredClone(this.saved); this.editingYears.clear(); this.adding = false; this.error = ''; this.message = ''; }
  save(): void {
    this.error = ''; this.message = '';
    if (!this.planId || !this.validDraft(this.rows)) {
      this.error = 'Completá los nombres y los años (1 a 20). El formulario debe contener al menos una materia del plan, sin duplicados.'; return;
    }
    const normalized = this.rows.map(row => ({ ...row, name: row.name.trim() }));
    try {
      localStorage.setItem(this.storageKey, JSON.stringify(normalized));
      this.rows = normalized; this.saved = structuredClone(normalized); this.editingYears.clear(); this.adding = false;
      this.message = 'Borrador guardado en este navegador. Todavía no se publica en la vista del alumno.';
    } catch { this.error = 'No se pudo guardar el borrador en este navegador. Tus cambios siguen disponibles en pantalla.'; }
  }
  private async run(action: () => Promise<void>): Promise<void> {
    this.loading = true; this.error = ''; this.message = '';
    try { await action(); }
    catch { if (!this.destroyed) this.error = 'No se pudieron cargar los datos. Volvé a seleccionar la carrera o el plan para reintentar.'; }
    finally { if (!this.destroyed) { this.loading = false; this.cdr.detectChanges(); } }
  }
}
