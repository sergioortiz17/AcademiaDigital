import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { TeachingPositionService, TeachingPosition } from '../../../core/services/teaching-position.service';
import { GradebookService, GradebookDetail, GradebookStudent } from '../../../core/services/gradebook.service';
import { CareerService, Career } from '../../../core/services/career.service';
import { ReopenGradebookDialogComponent } from './reopen-gradebook-dialog/reopen-gradebook-dialog.component';

const RESULT_LABELS: Record<string, string> = {
  Promoted: 'Promocionado',
  Regularized: 'Regular',
  Failed: 'Libre'
};

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Borrador',
  Submitted: 'Enviada',
  Approved: 'Aprobada',
  Published: 'Publicada',
  Closed: 'Cerrada'
};

@Component({
  selector: 'app-gradebook-management',
  templateUrl: './gradebook-management.component.html',
  styleUrls: ['./gradebook-management.component.scss'],
  standalone: false
})
export class GradebookManagementComponent implements OnInit, OnDestroy {
  // Emite al destruirse el componente para cancelar las subscripciones HTTP en vuelo
  // (evita que un 401 tardío de una request sin cancelar expulse al usuario tras navegar).
  private readonly destroy$ = new Subject<void>();
  // Carrera/Sede/Año decorativos (sin datos reales del backend), consistentes con el mockup.
  careers: Career[] = [];
  selectedCareerId: number | null = null;
  sedes = ['Sede Centro', 'Sede Norte'];
  selectedSede: string | null = null;
  planYears = [
    { value: 1, label: 'Primero' },
    { value: 2, label: 'Segundo' },
    { value: 3, label: 'Tercero' }
  ];
  selectedPlanYear: number | null = null;

  positions: TeachingPosition[] = [];
  selectedTeachingPositionId: number | null = null;

  detail: GradebookDetail | null = null;
  selectedStudent: GradebookStudent | null = null;
  searchTerm = '';

  isLoading = false;
  isProcessing = false;
  errorMsg = '';
  successMsg = '';
  noGradebookYet = false;

  constructor(
    private readonly teachingPositionService: TeachingPositionService,
    private readonly gradebookService: GradebookService,
    private readonly careerService: CareerService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().pipe(takeUntil(this.destroy$)).subscribe(careers => {
      this.careers = careers;
      this.selectedCareerId = careers[0]?.id ?? null;
      this.cdr.detectChanges();
    });

    this.teachingPositionService.getTeachingPositions({ includeInactive: false }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (positions) => {
        this.positions = positions;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudieron cargar los cargos docentes.';
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get selectedPosition(): TeachingPosition | undefined {
    return this.positions.find(p => p.id === this.selectedTeachingPositionId);
  }

  get filteredStudents(): GradebookStudent[] {
    if (!this.detail) return [];
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.detail.students;
    return this.detail.students.filter(s => s.studentName.toLowerCase().includes(term));
  }

  resultLabel(status: string | null): string {
    if (!status) return '—';
    return RESULT_LABELS[status] ?? status;
  }

  statusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  onPositionChange(): void {
    const position = this.selectedPosition;
    if (!position) return;

    this.isLoading = true;
    this.errorMsg = '';
    this.detail = null;
    this.selectedStudent = null;
    this.noGradebookYet = false;

    this.gradebookService.getGradebooks({
      courseId: position.courseId,
      commissionId: position.commissionId ?? undefined,
      academicYear: position.academicYear
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (gradebooks) => {
        if (gradebooks.length === 0) {
          this.noGradebookYet = true;
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }
        this.loadDetail(gradebooks[0].id);
      },
      error: (err) => {
        this.errorMsg = err.message || 'Error al buscar la planilla.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadDetail(id: number): void {
    this.isLoading = true;
    this.gradebookService.getGradebook(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.selectedStudent = detail.students.find(s => s.enrollmentId === this.selectedStudent?.enrollmentId) ?? detail.students[0] ?? null;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudo cargar la planilla.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectStudent(row: GradebookStudent): void {
    this.selectedStudent = row;
  }

  approve(): void {
    if (!this.detail || this.isProcessing) return;
    if (!confirm('¿Aprobar esta planilla?')) return;
    this.runTransition(this.gradebookService.approveGradebook(this.detail.gradebook.id), 'Planilla aprobada.');
  }

  publish(): void {
    if (!this.detail || this.isProcessing) return;
    if (!confirm('¿Publicar esta planilla? El alumno va a poder ver sus notas.')) return;
    this.runTransition(this.gradebookService.publishGradebook(this.detail.gradebook.id), 'Planilla publicada.');
  }

  close(): void {
    if (!this.detail || this.isProcessing) return;
    if (!confirm('¿Cerrar esta planilla? Se va a calcular la condición final de todos los alumnos y no se podrá editar más.')) return;
    this.runTransition(this.gradebookService.closeGradebook(this.detail.gradebook.id), 'Planilla cerrada.');
  }

  reopen(): void {
    if (!this.detail || this.isProcessing) return;
    const dialogRef = this.dialog.open(ReopenGradebookDialogComponent, { width: '450px', disableClose: true });
    dialogRef.afterClosed().subscribe((reason: string | null) => {
      if (!reason) return;
      this.runTransition(this.gradebookService.reopenGradebook(this.detail!.gradebook.id, reason), 'Planilla reabierta.');
    });
  }

  private runTransition(request: Observable<unknown>, successText: string): void {
    this.isProcessing = true;
    this.errorMsg = '';
    request.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.isProcessing = false;
        this.successMsg = successText;
        this.loadDetail(this.detail!.gradebook.id);
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.isProcessing = false;
        this.errorMsg = err.message || 'No se pudo completar la acción.';
        this.cdr.detectChanges();
      }
    });
  }
}
