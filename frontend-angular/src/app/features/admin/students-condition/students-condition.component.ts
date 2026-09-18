import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { forkJoin, of, Subject } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { GradebookService, Gradebook, GradebookStudent } from '../../../core/services/gradebook.service';
import { selectIsAdmin } from '../../../store/account/account.selectors';
import {
  AdminApproveDialogComponent,
  AdminApproveDialogResult
} from '../gradebook-management/admin-approve-dialog/admin-approve-dialog.component';
import { ApproveReasonDialogComponent } from './approve-reason-dialog/approve-reason-dialog.component';

const RESULT_LABELS: Record<string, string> = {
  Promoted: 'Promocionado',
  Regularized: 'Regular',
  Failed: 'Libre'
};

@Component({
  selector: 'app-students-condition',
  templateUrl: './students-condition.component.html',
  styleUrls: ['./students-condition.component.scss'],
  standalone: false
})
export class StudentsConditionComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  // Materias visibles según el rol: el backend ya limita GET /gradebooks a las del profesor
  // cuando no es admin, así que la lista de materias es correcta por permisos sin lógica extra.
  gradebooks: Gradebook[] = [];
  selectedGradebookId: number | null = null;

  students: GradebookStudent[] = [];
  searchTerm = '';

  // enrollmentIds tildados para la aprobación en lote (solo alumnos Promocionados).
  selectedIds = new Set<number>();

  isAdmin = false;
  isLoading = false;
  isProcessing = false;
  errorMsg = '';
  successMsg = '';
  loadedOnce = false;

  constructor(
    private readonly gradebookService: GradebookService,
    private readonly dialog: MatDialog,
    private readonly store: Store,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.store.select(selectIsAdmin).pipe(takeUntil(this.destroy$)).subscribe(isAdmin => {
      this.isAdmin = isAdmin;
      this.cdr.detectChanges();
    });

    this.isLoading = true;
    this.gradebookService.getGradebooks({}).pipe(takeUntil(this.destroy$)).subscribe({
      next: (gradebooks) => {
        this.gradebooks = gradebooks;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudieron cargar las materias.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get filteredStudents(): GradebookStudent[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.students;
    return this.students.filter(s =>
      s.studentName.toLowerCase().includes(term) ||
      (s.legajoNumber ?? '').toLowerCase().includes(term));
  }

  gradebookLabel(g: Gradebook): string {
    const division = g.divisionName ? ` — ${g.divisionName}` : '';
    const code = g.divisionCode ? ` [${g.divisionCode}]` : '';
    return `${g.courseName}${division}${code} (${g.academicYear})`;
  }

  resultLabel(status: string | null): string {
    if (!status) return 'Sin cerrar';
    return RESULT_LABELS[status] ?? status;
  }

  isPromoted(row: GradebookStudent): boolean {
    return row.resultStatus === 'Promoted';
  }

  // Regular/Libre requieren mesa final; "sin condición" (resultStatus null) todavía no cerró notas.
  needsFinalExam(row: GradebookStudent): boolean {
    return row.resultStatus === 'Regularized' || row.resultStatus === 'Failed';
  }

  onGradebookChange(): void {
    if (this.selectedGradebookId == null) return;
    this.isLoading = true;
    this.errorMsg = '';
    this.students = [];
    this.selectedIds.clear();
    this.gradebookService.getGradebook(this.selectedGradebookId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (detail) => {
        this.students = detail.students;
        this.loadedOnce = true;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudo cargar el listado de alumnos.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // Aprobación manual: solo Admin y solo para Promocionado. Reusa el diálogo y el endpoint
  // existentes (AdminApproveDialogComponent + adminApproveEnrollment). El backend además
  // exige RequireAdmin(), así que un Profesor nunca puede aprobar aunque fuerce la llamada.
  approve(row: GradebookStudent): void {
    if (!this.isAdmin || !this.isPromoted(row) || this.isProcessing) return;
    const gb = this.gradebooks.find(g => g.id === this.selectedGradebookId);
    const dialogRef = this.dialog.open(AdminApproveDialogComponent, {
      width: '480px',
      disableClose: true,
      data: {
        studentName: row.studentName,
        courseName: gb?.courseName ?? 'la materia',
        initialGrade: row.average
      }
    });
    dialogRef.afterClosed().subscribe((result: AdminApproveDialogResult | null) => {
      if (!result) return;
      this.isProcessing = true;
      this.errorMsg = '';
      this.gradebookService.adminApproveEnrollment(row.enrollmentId, result.finalGrade, result.reason)
        .pipe(takeUntil(this.destroy$)).subscribe({
          next: () => {
            this.isProcessing = false;
            this.successMsg = `Materia marcada como aprobada para ${row.studentName}.`;
            this.onGradebookChange();
            setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
          },
          error: (err) => {
            this.isProcessing = false;
            this.errorMsg = err.message || 'No se pudo aprobar la materia.';
            this.cdr.detectChanges();
          }
        });
    });
  }

  // ── Selección múltiple (checkboxes) ─────────────────────────────────────────
  // Solo tildable para Promocionados y solo para Admin. Regular/Libre/sin-nota no participan.

  canSelect(row: GradebookStudent): boolean {
    return this.isAdmin && this.isPromoted(row);
  }

  isSelected(row: GradebookStudent): boolean {
    return this.selectedIds.has(row.enrollmentId);
  }

  toggleSelection(row: GradebookStudent, checked: boolean): void {
    if (!this.canSelect(row)) return;
    if (checked) this.selectedIds.add(row.enrollmentId);
    else this.selectedIds.delete(row.enrollmentId);
  }

  /** Promocionados visibles bajo el filtro/búsqueda actual (candidatos a selección). */
  get selectablePromoted(): GradebookStudent[] {
    return this.filteredStudents.filter(row => this.canSelect(row));
  }

  get selectedCount(): number {
    // Contar solo los que siguen visibles y seleccionables (defensa ante filtros que cambian).
    return this.selectablePromoted.filter(row => this.isSelected(row)).length;
  }

  get allVisibleSelected(): boolean {
    const selectable = this.selectablePromoted;
    return selectable.length > 0 && selectable.every(row => this.isSelected(row));
  }

  get someVisibleSelected(): boolean {
    return this.selectedCount > 0 && !this.allVisibleSelected;
  }

  toggleSelectAll(checked: boolean): void {
    for (const row of this.selectablePromoted) {
      if (checked) this.selectedIds.add(row.enrollmentId);
      else this.selectedIds.delete(row.enrollmentId);
    }
  }

  // ── Aprobación en lote ──────────────────────────────────────────────────────

  approveSelected(): void {
    if (!this.isAdmin || this.isProcessing) return;
    // Blindaje: aprobar solo Promocionados seleccionados y visibles; excluir cualquier otro caso.
    const targets = this.selectablePromoted.filter(row => this.isSelected(row));
    if (targets.length === 0) return;

    const gb = this.gradebooks.find(g => g.id === this.selectedGradebookId);
    const dialogRef = this.dialog.open(ApproveReasonDialogComponent, {
      width: '480px',
      disableClose: true,
      data: { count: targets.length, courseName: gb?.courseName ?? 'la materia' }
    });

    dialogRef.afterClosed().subscribe((reason: string | null) => {
      if (!reason) return;
      this.isProcessing = true;
      this.errorMsg = '';

      // Una request por alumno, cada uno con SU propio promedio como nota final. forkJoin espera
      // a todas y no aborta el lote si una falla (catchError -> resultado ok:false por alumno).
      forkJoin(
        targets.map(row =>
          this.gradebookService.adminApproveEnrollment(row.enrollmentId, row.average ?? 0, reason).pipe(
            map(() => ({ name: row.studentName, ok: true as const })),
            catchError(err => of({ name: row.studentName, ok: false as const, msg: err?.error?.msg || err?.message }))
          ))
      ).pipe(takeUntil(this.destroy$)).subscribe({
        next: results => {
          this.isProcessing = false;
          const approved = results.filter(r => r.ok).length;
          const failed = results.filter(r => !r.ok);
          this.successMsg = `${approved}/${targets.length} materia(s) aprobada(s).`;
          if (failed.length > 0) {
            this.errorMsg = `No se pudieron aprobar: ${failed.map(f => f.name).join(', ')}.`;
          }
          this.selectedIds.clear();
          this.onGradebookChange();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 5000);
        },
        error: () => {
          this.isProcessing = false;
          this.errorMsg = 'No se pudo completar la aprobación en lote.';
          this.cdr.detectChanges();
        }
      });
    });
  }
}
