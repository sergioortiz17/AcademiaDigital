import { ChangeDetectorRef, Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { CareerService } from '../../../core/services/career.service';

/**
 * Standalone "create career" form: only the Career metadata (Name, Code, Description,
 * TotalCredits, DurationYears), no study plan, no CSV. Split out from the old all-in-one
 * career-import flow — a career can now have zero, one, or several study plans imported
 * separately over time (see StudyPlanImportComponent).
 */
@Component({
  selector: 'app-career-create',
  templateUrl: './career-create.component.html',
  styleUrls: ['./career-create.component.scss'],
  standalone: false
})
export class CareerCreateComponent {
  name = '';
  code = '';
  description = '';
  totalCredits: number | null = null;
  durationYears: number | null = null;

  isLoading = false;
  errorMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly cdr: ChangeDetectorRef,
    public readonly dialogRef: MatDialogRef<CareerCreateComponent>
  ) {}

  get isFormValid(): boolean {
    return !!(this.name.trim() && this.code.trim() && this.totalCredits !== null && this.durationYears !== null);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  onSubmit(): void {
    if (!this.isFormValid) {
      this.errorMsg = 'Completá todos los campos obligatorios.';
      return;
    }

    this.isLoading = true;
    this.errorMsg = '';

    this.careerService
      .createCareer({
        name: this.name.trim(),
        code: this.code.trim(),
        description: this.description.trim() || undefined,
        totalCredits: this.totalCredits as number,
        durationYears: this.durationYears as number
      })
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMsg = err.error?.msg || err.error?.title || 'Error al crear la carrera.';
          this.cdr.detectChanges();
        }
      });
  }
}
