import { ChangeDetectorRef, Component, Inject, OnInit, Optional } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import {
  Career,
  CareerService,
  CsvRowError,
  StudyPlan,
  StudyPlanDiff,
  StudyPlanImportResult
} from '../../../core/services/career.service';

export interface StudyPlanImportDialogData {
  /** Preselected career (opened from a career's row). Undefined when opened from the general header button. */
  career?: Career;
  careers?: Career[];
}

/**
 * Imports a new StudyPlan (courses CSV) into an EXISTING career. Split out from the old
 * all-in-one career-import flow: creating a career (CareerCreateComponent) and importing a study
 * plan into it are now two separate admin actions, because a career can accumulate several study
 * plans over time (e.g. successive Resolución Ministerial versions).
 */
@Component({
  selector: 'app-study-plan-import',
  templateUrl: './study-plan-import.component.html',
  styleUrls: ['./study-plan-import.component.scss'],
  standalone: false
})
export class StudyPlanImportComponent implements OnInit {
  careers: Career[] = [];
  selectedCareerId: number | null = null;

  // StudyPlan metadata (sent as separate form fields, not part of the CSV)
  code = '';
  name = '';
  versionNumber = 1;
  effectiveFrom: Date | null = null;
  effectiveTo: Date | null = null;

  selectedFile: File | null = null;
  selectedFileName = '';

  isLoading = false;
  isPreviewLoading = false;
  errorMsg = '';
  successMsg = '';
  rowErrors: CsvRowError[] = [];
  result: StudyPlanImportResult | null = null;

  activeStudyPlan: StudyPlan | null = null;
  diffPreview: StudyPlanDiff | null = null;

  readonly csvTemplateColumns =
    'sort_order,course_code,name,year_number,semester,course_type_code,workload_hours,is_mandatory,prerequisites';

  constructor(
    private readonly careerService: CareerService,
    private readonly cdr: ChangeDetectorRef,
    @Optional() public readonly dialogRef?: MatDialogRef<StudyPlanImportComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public readonly data?: StudyPlanImportDialogData
  ) {}

  ngOnInit(): void {
    this.careers = this.data?.careers ?? [];
    if (this.data?.career) {
      const preselected = this.data.career;
      this.selectedCareerId = preselected.id;
      this.careers = [preselected, ...this.careers.filter((c) => c.id !== preselected.id)];
    }
    if (this.selectedCareerId) {
      this.loadActiveStudyPlan();
    }
  }

  /** True only when opened with a single, fixed career (row action) — hides the career picker. */
  get isCareerFixed(): boolean {
    return !!this.data?.career;
  }

  onCareerChange(): void {
    this.activeStudyPlan = null;
    this.diffPreview = null;
    if (this.selectedCareerId) this.loadActiveStudyPlan();
  }

  private loadActiveStudyPlan(): void {
    if (!this.selectedCareerId) return;
    this.careerService.getStudyPlans(this.selectedCareerId).subscribe({
      next: (plans) => {
        this.activeStudyPlan = plans.find((p) => p.status === 'Active') ?? null;
        this.cdr.detectChanges();
      },
      error: () => {
        this.activeStudyPlan = null;
      }
    });
  }

  close(refresh: boolean): void {
    this.dialogRef?.close(refresh);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    this.selectedFile = file;
    this.selectedFileName = file ? file.name : '';
    this.diffPreview = null;
  }

  downloadTemplate(): void {
    const sampleRows = [
      this.csvTemplateColumns,
      '1,ENF-01,Salud Pública y Epidemiología,1,1,FG,60,true,',
      '2,ENF-02,Morfofisiología Humana,1,1,FE,80,true,',
      '3,ENF-03,Práctica Profesionalizante I,1,2,PP,40,true,ENF-01;ENF-02'
    ];
    const blob = new Blob([sampleRows.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'plantilla_plan_estudios.csv';
    anchor.click();
    window.URL.revokeObjectURL(url);
  }

  get isFormValid(): boolean {
    return !!(
      this.selectedCareerId &&
      this.code.trim() &&
      this.name.trim() &&
      this.versionNumber &&
      this.selectedFile
    );
  }

  get canPreview(): boolean {
    return !!(this.selectedCareerId && this.activeStudyPlan && this.selectedFile);
  }

  previewChanges(): void {
    if (!this.canPreview || !this.selectedFile || !this.activeStudyPlan || !this.selectedCareerId) return;

    this.isPreviewLoading = true;
    this.errorMsg = '';
    this.rowErrors = [];
    this.diffPreview = null;

    const formData = new FormData();
    formData.append('File', this.selectedFile, this.selectedFile.name);

    this.careerService
      .previewStudyPlanDiff(this.selectedCareerId, this.activeStudyPlan.id, formData)
      .subscribe({
        next: (diff) => {
          this.diffPreview = diff;
          this.isPreviewLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.isPreviewLoading = false;
          if (err.status === 400 && err.error?.errors) {
            this.rowErrors = err.error.errors;
            this.errorMsg = 'El archivo CSV tiene errores. Corregilos y volvé a intentar.';
          } else {
            this.errorMsg = err.error?.detail || err.error?.msg || 'Error al previsualizar los cambios.';
          }
          this.cdr.detectChanges();
        }
      });
  }

  onSubmit(): void {
    if (!this.isFormValid || !this.selectedFile || !this.selectedCareerId) {
      this.errorMsg = 'Completá todos los campos obligatorios y seleccioná un archivo CSV.';
      return;
    }

    this.isLoading = true;
    this.errorMsg = '';
    this.successMsg = '';
    this.rowErrors = [];
    this.result = null;

    const formData = new FormData();
    formData.append('Code', this.code.trim());
    formData.append('Name', this.name.trim());
    formData.append('VersionNumber', String(this.versionNumber));
    if (this.effectiveFrom) formData.append('EffectiveFrom', this.toIsoDate(this.effectiveFrom));
    if (this.effectiveTo) formData.append('EffectiveTo', this.toIsoDate(this.effectiveTo));
    formData.append('File', this.selectedFile, this.selectedFile.name);

    this.careerService.importStudyPlanCsv(this.selectedCareerId, formData).subscribe({
      next: (res) => {
        this.result = res;
        this.successMsg = `Plan de estudios importado con éxito: ${res.coursesCreated} materia(s) nueva(s) y ${res.prerequisitesCreated} correlatividad(es). Activalo desde la carrera cuando quieras que entre en vigencia.`;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 400 && err.error?.errors) {
          this.rowErrors = err.error.errors;
          this.errorMsg = 'El archivo CSV tiene errores. Corregilos y volvé a intentar.';
        } else {
          this.errorMsg = err.error?.detail || err.error?.msg || 'Error al importar el plan de estudios.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  private toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
