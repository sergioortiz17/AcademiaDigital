import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Career, CareerService, StudyPlan } from '../../../core/services/career.service';
import { StudyPlanImportComponent } from '../study-plan-import/study-plan-import.component';
import { CareerCreateComponent } from '../career-create/career-create.component';
import { StudyPlanDiffComponent } from '../study-plan-diff/study-plan-diff.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-career-management',
  templateUrl: './career-management.component.html',
  styleUrls: ['./career-management.component.scss'],
  standalone: false
})
export class CareerManagementComponent implements OnInit {
  careers: Career[] = [];
  isLoading = false;
  errorMsg = '';
  successMsg = '';

  displayedColumns = ['name', 'code', 'durationYears', 'courseCount', 'isActive', 'actions'];

  constructor(
    private readonly careerService: CareerService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCareers();
  }

  loadCareers(): void {
    this.isLoading = true;
    this.errorMsg = '';

    this.careerService.getCareers().subscribe({
      next: (careers) => {
        this.careers = careers;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.error?.msg || 'Error al cargar las carreras.';
        this.cdr.detectChanges();
      }
    });
  }

  openCreateCareerDialog(): void {
    const dialogRef = this.dialog.open(CareerCreateComponent, {
      width: '600px',
      maxWidth: '95vw',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe((created) => {
      if (created) {
        this.successMsg = 'Carrera creada con éxito.';
        this.loadCareers();
        this.clearMessagesAfterDelay();
      }
    });
  }

  /** General "Importar plan" button in the header: no career preselected, admin picks one. */
  openImportDialog(): void {
    this.openStudyPlanImportDialog(null);
  }

  /** Per-row "Importar plan de estudios" action: the career is fixed, no picker shown. */
  openImportDialogForCareer(career: Career): void {
    this.openStudyPlanImportDialog(career);
  }

  private openStudyPlanImportDialog(career: Career | null): void {
    const dialogRef = this.dialog.open(StudyPlanImportComponent, {
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      disableClose: true,
      data: { career: career ?? undefined, careers: this.careers }
    });

    dialogRef.afterClosed().subscribe((refresh) => {
      if (refresh) {
        this.successMsg = 'Plan de estudios importado con éxito.';
        this.loadCareers();
        this.clearMessagesAfterDelay();
      }
    });
  }

  /** Only meaningful when the career already has 2+ study plans to compare. */
  openComparePlansDialog(career: Career): void {
    this.careerService.getStudyPlans(career.id).subscribe({
      next: (studyPlans: StudyPlan[]) => {
        if (studyPlans.length < 2) {
          this.errorMsg = 'Esta carrera necesita al menos dos planes de estudio para poder compararlos.';
          this.cdr.detectChanges();
          this.clearMessagesAfterDelay();
          return;
        }
        this.dialog.open(StudyPlanDiffComponent, {
          width: '900px',
          maxWidth: '95vw',
          maxHeight: '90vh',
          data: { career, studyPlans }
        });
      },
      error: (err) => {
        this.errorMsg = err.error?.msg || 'Error al cargar los planes de estudio de la carrera.';
        this.cdr.detectChanges();
      }
    });
  }

  deleteCareer(career: Career): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '450px',
      disableClose: true,
      data: {
        title: 'Acción importante',
        action: 'ELIMINAR',
        username: `${career.name} (${career.code})`
      }
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (!result) return;

      this.careerService.deleteCareer(career.id).subscribe({
        next: () => {
          this.successMsg = `Carrera ${career.name} eliminada.`;
          this.loadCareers();
          this.clearMessagesAfterDelay();
        },
        error: (err) => {
          this.errorMsg = err.error?.msg || 'Error al eliminar la carrera.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  private clearMessagesAfterDelay(): void {
    setTimeout(() => {
      this.successMsg = '';
      this.errorMsg = '';
      this.cdr.detectChanges();
    }, 4000);
  }
}
