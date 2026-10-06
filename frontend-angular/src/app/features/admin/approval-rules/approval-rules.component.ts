import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subject as RxSubject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CareerService, Career } from '../../../core/services/career.service';
import { SubjectService, StudyPlan, Subject, CourseApprovalRule } from '../../../core/services/subject.service';

// Fila editable: la materia + un borrador de su regla (con defaults si todavía no tiene).
interface RuleRow {
  subject: Subject;
  draft: CourseApprovalRule;
  saving: boolean;
  savedOk: boolean;
  error: string;
}

const DEFAULT_RULE: CourseApprovalRule = {
  minimumRegularGrade: 6,
  minimumPromotionGrade: 7,
  minimumFinalExamGrade: 6,
  minimumAttendancePercentage: 75,
  requiresFinalExam: true,
  allowsPromotion: true
};

@Component({
  selector: 'app-approval-rules',
  templateUrl: './approval-rules.component.html',
  styleUrls: ['./approval-rules.component.scss'],
  standalone: false
})
export class ApprovalRulesComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new RxSubject<void>();

  careers: Career[] = [];
  selectedCareerId: number | null = null;

  studyPlan: StudyPlan | null = null;
  rows: RuleRow[] = [];

  isLoading = false;
  errorMsg = '';
  loadedOnce = false;

  constructor(
    private readonly careerService: CareerService,
    private readonly subjectService: SubjectService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().pipe(takeUntil(this.destroy$)).subscribe({
      next: (careers) => {
        this.careers = careers;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudieron cargar las carreras.';
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onCareerChange(): void {
    if (this.selectedCareerId == null) return;
    this.isLoading = true;
    this.errorMsg = '';
    this.rows = [];
    this.studyPlan = null;

    this.subjectService.getStudyPlansByCareer(this.selectedCareerId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (plans) => {
        // Preferimos el plan activo; si no hay marcado, tomamos el primero.
        const plan = plans.find(p => p.isActive) ?? plans[0] ?? null;
        this.studyPlan = plan;
        if (!plan) {
          this.isLoading = false;
          this.loadedOnce = true;
          this.errorMsg = 'La carrera no tiene un plan de estudios.';
          this.cdr.detectChanges();
          return;
        }
        this.loadSubjects(plan.id);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.message || 'No se pudo cargar el plan de estudios.';
        this.cdr.detectChanges();
      }
    });
  }

  private loadSubjects(studyPlanId: number): void {
    this.subjectService.getSubjectsByCareer(studyPlanId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (subjects) => {
        this.rows = subjects.map(subject => ({
          subject,
          draft: subject.approvalRule
            ? { ...subject.approvalRule }
            : { ...DEFAULT_RULE },
          saving: false,
          savedOk: false,
          error: ''
        }));
        this.loadedOnce = true;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.message || 'No se pudieron cargar las materias.';
        this.cdr.detectChanges();
      }
    });
  }

  private rowValid(row: RuleRow): string | null {
    const d = row.draft;
    if (d.minimumFinalExamGrade == null || d.minimumFinalExamGrade < 1 || d.minimumFinalExamGrade > 10)
      return 'La nota mínima de mesa final debe estar entre 1 y 10.';
    if (d.minimumRegularGrade != null && (d.minimumRegularGrade < 1 || d.minimumRegularGrade > 10))
      return 'La nota mínima para regularizar debe estar entre 1 y 10.';
    if (d.minimumPromotionGrade != null && (d.minimumPromotionGrade < 1 || d.minimumPromotionGrade > 10))
      return 'La nota mínima de promoción debe estar entre 1 y 10.';
    if (d.allowsPromotion && d.minimumPromotionGrade == null)
      return 'Si permite promoción, indicá la nota mínima de promoción.';
    if (d.minimumRegularGrade != null && d.minimumPromotionGrade != null && d.minimumPromotionGrade < d.minimumRegularGrade)
      return 'La nota de promoción no puede ser menor que la de regularización.';
    return null;
  }

  save(row: RuleRow): void {
    if (row.saving || this.studyPlan == null) return;
    const validationError = this.rowValid(row);
    if (validationError) { row.error = validationError; row.savedOk = false; this.cdr.detectChanges(); return; }

    row.saving = true;
    row.error = '';
    row.savedOk = false;
    this.subjectService.setApprovalRule(this.studyPlan.id, row.subject.id, row.draft)
      .pipe(takeUntil(this.destroy$)).subscribe({
        next: () => {
          row.saving = false;
          row.savedOk = true;
          row.subject.approvalRule = { ...row.draft };
          setTimeout(() => { row.savedOk = false; this.cdr.detectChanges(); }, 3000);
          this.cdr.detectChanges();
        },
        error: (err) => {
          row.saving = false;
          row.error = err?.error?.detail || err.message || 'No se pudo guardar la regla.';
          this.cdr.detectChanges();
        }
      });
  }
}
