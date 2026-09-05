import { ChangeDetectorRef, Component, Inject, Input, OnInit, Optional } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Career, CareerService, StudyPlan, StudyPlanDiff } from '../../../core/services/career.service';

export interface StudyPlanDiffDialogData {
  career: Career;
  studyPlans: StudyPlan[];
}

/**
 * Git-diff-style comparison of two study plans (green = added, red = removed, and per-field
 * red-old/green-new for modified courses). Used in two ways:
 *
 *  1. As a dialog opened from CareerManagementComponent ("Comparar planes"): the admin picks plan
 *     A and plan B from the career's own study plans, and the component fetches
 *     GET api/v1/study-plans/{a}/diff/{b} itself. Pass `data` (MAT_DIALOG_DATA) for this mode.
 *
 *  2. Embedded read-only inside StudyPlanImportComponent's "Previsualizar cambios" step: the
 *     parent already has a computed StudyPlanDiff (from the diff-preview endpoint, CSV vs active
 *     plan) and passes it directly via the `diff` @Input — no picker, no extra fetch.
 */
@Component({
  selector: 'app-study-plan-diff',
  templateUrl: './study-plan-diff.component.html',
  styleUrls: ['./study-plan-diff.component.scss'],
  standalone: false
})
export class StudyPlanDiffComponent implements OnInit {
  @Input() diff: StudyPlanDiff | null = null;

  studyPlans: StudyPlan[] = [];
  planAId: number | null = null;
  planBId: number | null = null;

  isLoading = false;
  errorMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly cdr: ChangeDetectorRef,
    @Optional() public readonly dialogRef?: MatDialogRef<StudyPlanDiffComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public readonly data?: StudyPlanDiffDialogData
  ) {}

  /** True in picker mode (dialog opened without a ready-made diff). */
  get isPickerMode(): boolean {
    return !this.diff && !!this.data;
  }

  ngOnInit(): void {
    if (this.data) {
      this.studyPlans = [...this.data.studyPlans].sort((a, b) => b.versionNumber - a.versionNumber);
      if (this.studyPlans.length >= 2) {
        this.planAId = this.studyPlans[1].id;
        this.planBId = this.studyPlans[0].id;
        this.loadDiff();
      }
    }
  }

  close(): void {
    this.dialogRef?.close();
  }

  loadDiff(): void {
    if (!this.planAId || !this.planBId) return;
    if (this.planAId === this.planBId) {
      this.errorMsg = 'Elegí dos planes distintos para comparar.';
      this.diff = null;
      return;
    }

    this.isLoading = true;
    this.errorMsg = '';
    this.diff = null;

    this.careerService.diffStudyPlans(this.planAId, this.planBId).subscribe({
      next: (diff) => {
        this.diff = diff;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.error?.detail || err.error?.msg || 'Error al comparar los planes de estudio.';
        this.cdr.detectChanges();
      }
    });
  }

  get hasChanges(): boolean {
    return !!(
      this.diff &&
      (this.diff.addedCourses.length > 0 ||
        this.diff.removedCourses.length > 0 ||
        this.diff.modifiedCourses.length > 0)
    );
  }

  fieldLabel(field: string): string {
    const labels: Record<string, string> = {
      year_number: 'Año',
      semester: 'Cuatrimestre',
      course_type_code: 'Tipo de materia',
      workload_hours: 'Carga horaria',
      is_mandatory: 'Obligatoria',
      name: 'Nombre'
    };
    return labels[field] ?? field;
  }
}
