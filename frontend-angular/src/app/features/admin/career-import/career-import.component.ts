import { ChangeDetectorRef, Component } from '@angular/core';
import { CareerImportRowError, CareerImportResult, CareerService } from '../../../core/services/career.service';

@Component({
  selector: 'app-career-import',
  templateUrl: './career-import.component.html',
  styleUrls: ['./career-import.component.scss'],
  standalone: false
})
export class CareerImportComponent {
  // Career + StudyPlan metadata (sent as separate form fields, not part of the CSV)
  name = '';
  code = '';
  description = '';
  totalCredits: number | null = null;
  durationYears: number | null = null;
  studyPlanCode = '';
  studyPlanName = '';
  versionNumber = 1;
  effectiveFrom: Date | null = null;

  selectedFile: File | null = null;
  selectedFileName = '';

  isLoading = false;
  errorMsg = '';
  successMsg = '';
  rowErrors: CareerImportRowError[] = [];
  result: CareerImportResult | null = null;

  readonly csvTemplateColumns =
    'sort_order,course_code,name,year_number,semester,course_type_code,workload_hours,is_mandatory,prerequisites';

  constructor(
    private readonly careerService: CareerService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    this.selectedFile = file;
    this.selectedFileName = file ? file.name : '';
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
    anchor.download = 'plantilla_carrera.csv';
    anchor.click();
    window.URL.revokeObjectURL(url);
  }

  get isFormValid(): boolean {
    return !!(
      this.name.trim() &&
      this.code.trim() &&
      this.totalCredits !== null &&
      this.durationYears !== null &&
      this.studyPlanCode.trim() &&
      this.studyPlanName.trim() &&
      this.versionNumber &&
      this.selectedFile
    );
  }

  onSubmit(): void {
    if (!this.isFormValid || !this.selectedFile) {
      this.errorMsg = 'Completá todos los campos obligatorios y seleccioná un archivo CSV.';
      return;
    }

    this.isLoading = true;
    this.errorMsg = '';
    this.successMsg = '';
    this.rowErrors = [];
    this.result = null;

    const formData = new FormData();
    formData.append('Name', this.name.trim());
    formData.append('Code', this.code.trim());
    if (this.description.trim()) formData.append('Description', this.description.trim());
    formData.append('TotalCredits', String(this.totalCredits));
    formData.append('DurationYears', String(this.durationYears));
    formData.append('StudyPlanCode', this.studyPlanCode.trim());
    formData.append('StudyPlanName', this.studyPlanName.trim());
    formData.append('VersionNumber', String(this.versionNumber));
    if (this.effectiveFrom) formData.append('EffectiveFrom', this.toIsoDate(this.effectiveFrom));
    formData.append('File', this.selectedFile, this.selectedFile.name);

    this.careerService.importCareerCsv(formData).subscribe({
      next: (res) => {
        this.result = res;
        this.successMsg = `Carrera creada con éxito: ${res.coursesCreated} materias y ${res.prerequisitesCreated} correlatividades.`;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 400 && err.error?.errors) {
          this.rowErrors = err.error.errors;
          this.errorMsg = 'El archivo CSV tiene errores. Corregilos y volvé a intentar.';
        } else {
          this.errorMsg = err.error?.msg || 'Error al importar la carrera.';
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
