import { ChangeDetectorRef, Component } from '@angular/core';
import { EnrollmentService, MyEnrollmentPeriodDto } from '../../core/services/enrollment. service';

@Component({
  selector: 'app-enrollments',
  templateUrl: './enrollments.component.html',
  styleUrls: ['./enrollments.component.scss'],
  standalone: false
})
export class EnrollmentsComponent {
  showMyEnrollments = false;

  myEnrollments: MyEnrollmentPeriodDto[] = [];
  loadingMyEnrollments = false;
  myEnrollmentsError = '';

  constructor(
    private readonly enrollmentService: EnrollmentService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  openMyEnrollments(): void {
    if (this.loadingMyEnrollments) return;
    this.loadingMyEnrollments = true;
    this.myEnrollmentsError = '';
    this.enrollmentService.getMyEnrollments().subscribe({
      next: res => {
        this.myEnrollments = res.data;
        this.loadingMyEnrollments = false;
        this.showMyEnrollments = true;   // panel opens only after data is ready
        this.cdr.markForCheck();
      },
      error: () => {
        this.myEnrollmentsError = 'No se pudieron cargar tus inscripciones.';
        this.loadingMyEnrollments = false;
        this.showMyEnrollments = true;
        this.cdr.markForCheck();
      }
    });
  }

  backToPlan(): void {
    this.showMyEnrollments = false;
    this.myEnrollments = [];
    this.myEnrollmentsError = '';
  }

  printEnrollment(item: MyEnrollmentPeriodDto): void {
    const courseList = item.courseNames.map(c => `<li>${c}</li>`).join('');
    const html = `
      <!DOCTYPE html><html><head>
      <title>Constancia de inscripción</title>
      <style>
        body { font-family: Arial, sans-serif; padding: 32px; max-width: 700px; margin: 0 auto; }
        h2 { color: #1565c0; border-bottom: 2px solid #1565c0; padding-bottom: 8px; }
        .meta { color: #555; margin-bottom: 16px; }
        ul { line-height: 2; }
        .footer { margin-top: 48px; color: #888; font-size: 12px; }
      </style></head><body>
      <h2>Instituto Técnico Superior Córdoba</h2>
      <h3>Constancia de Inscripción</h3>
      <div class="meta">
        <p><strong>Año académico:</strong> ${item.academicYear} — ${this.semesterLabel(item.semester)}</p>
        <p><strong>Turno:</strong> ${item.shift ?? '-'}</p>
        <p><strong>Fecha de inscripción:</strong> ${new Date(item.enrollmentDate).toLocaleDateString('es-AR')}</p>
      </div>
      <p><strong>Materias inscriptas:</strong></p>
      <ul>${courseList}</ul>
      <div class="footer">Documento generado el ${new Date().toLocaleDateString('es-AR')}</div>
      </body></html>`;

    const blob = new Blob([html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const win = window.open(url, '_blank');
    if (win) {
      win.addEventListener('load', () => {
        win.print();
        URL.revokeObjectURL(url);
      });
    }
  }

  cancelEnrollment(item: MyEnrollmentPeriodDto): void {
    if (!item.periodId) {
      alert('No se puede dar de baja: período no encontrado.');
      return;
    }
    if (!confirm(`¿Cancelar tu inscripción del ${item.academicYear} - ${this.semesterLabel(item.semester)}?\nSe eliminarán todas las materias inscriptas.`)) return;
    this.enrollmentService.cancelMyEnrollment(item.periodId).subscribe({
      next: () => {
        this.myEnrollments = this.myEnrollments.filter(e => e.periodId !== item.periodId);
        this.cdr.markForCheck();
      },
      error: err => alert(err.message || 'No se pudo cancelar la inscripción.')
    });
  }

  semesterLabel(s: number): string {
    return s === 1 ? '1° Semestre' : '2° Semestre';
  }
}
