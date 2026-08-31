import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TeacherService, TeacherAssignment } from '../../core/services/teacher.service';
import {
  GradebookService,
  GradebookDetail,
  GradebookStudent,
  SaveGradeEntryInput
} from '../../core/services/gradebook.service';
import { EvaluationSetupDialogComponent } from './evaluation-setup-dialog/evaluation-setup-dialog.component';

interface EditableGrade {
  evaluationId: number;
  score: number | null;
  notes: string;
  updatedAt: string | null;
}

interface EditableRow extends GradebookStudent {
  editableGrades: Record<number, EditableGrade>;
}

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
  selector: 'app-grades',
  templateUrl: './grades.component.html',
  styleUrls: ['./grades.component.scss'],
  standalone: false
})
export class GradesComponent implements OnInit {
  assignments: TeacherAssignment[] = [];
  selectedTeachingPositionId: number | null = null;

  detail: GradebookDetail | null = null;
  rows: EditableRow[] = [];
  selectedStudent: EditableRow | null = null;
  searchTerm = '';

  isLoading = false;
  isCreating = false;
  isSaving = false;
  isSubmitting = false;
  errorMsg = '';
  successMsg = '';
  noGradebookYet = false;

  constructor(
    private readonly teacherService: TeacherService,
    private readonly gradebookService: GradebookService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.teacherService.getMyAssignments(false).subscribe({
      next: (assignments) => {
        this.assignments = assignments;
        if (assignments.length === 1) {
          this.selectedTeachingPositionId = assignments[0].teachingPositionId;
          this.loadGradebookForPosition();
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudieron cargar tus materias asignadas.';
        this.cdr.detectChanges();
      }
    });
  }

  get selectedAssignment(): TeacherAssignment | undefined {
    return this.assignments.find(a => a.teachingPositionId === this.selectedTeachingPositionId);
  }

  get canEdit(): boolean {
    return this.detail?.gradebook.status === 'Draft';
  }

  get filteredRows(): EditableRow[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.rows;
    return this.rows.filter(r => r.studentName.toLowerCase().includes(term));
  }

  resultLabel(status: string | null): string {
    if (!status) return '—';
    return RESULT_LABELS[status] ?? status;
  }

  statusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  onPositionChange(): void {
    this.loadGradebookForPosition();
  }

  loadGradebookForPosition(): void {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    this.isLoading = true;
    this.errorMsg = '';
    this.detail = null;
    this.rows = [];
    this.noGradebookYet = false;

    this.gradebookService.getGradebooks({
      courseId: assignment.courseId,
      commissionId: assignment.commissionId ?? undefined,
      academicYear: assignment.academicYear
    }).subscribe({
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

  loadDetail(gradebookId: number): void {
    this.isLoading = true;
    this.gradebookService.getGradebook(gradebookId).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.rows = detail.students.map(student => this.toEditableRow(student));
        this.selectedStudent = this.rows.find(r => r.enrollmentId === this.selectedStudent?.enrollmentId) ?? this.rows[0] ?? null;
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

  private toEditableRow(student: GradebookStudent): EditableRow {
    const editableGrades: Record<number, EditableGrade> = {};
    for (const grade of student.grades) {
      editableGrades[grade.evaluationId] = {
        evaluationId: grade.evaluationId,
        score: grade.score,
        notes: grade.notes ?? '',
        updatedAt: grade.updatedAt
      };
    }
    return { ...student, editableGrades };
  }

  openCreateDialog(): void {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    const dialogRef = this.dialog.open(EvaluationSetupDialogComponent, {
      width: '600px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe((evaluations) => {
      if (!evaluations) return;
      this.isCreating = true;
      this.gradebookService.createGradebook(assignment.teachingPositionId, evaluations).subscribe({
        next: (gradebook) => {
          this.isCreating = false;
          this.noGradebookYet = false;
          this.successMsg = 'Planilla creada correctamente.';
          this.loadDetail(gradebook.id);
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.isCreating = false;
          this.errorMsg = err.message || 'Error al crear la planilla.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  selectStudent(row: EditableRow): void {
    this.selectedStudent = row;
  }

  saveGrades(): void {
    if (!this.detail || !this.canEdit || this.isSaving) return;

    const grades: SaveGradeEntryInput[] = [];
    for (const row of this.rows) {
      for (const evaluation of this.detail.evaluations) {
        const grade = row.editableGrades[evaluation.id];
        if (grade?.score != null) {
          grades.push({
            evaluationId: evaluation.id,
            enrollmentId: row.enrollmentId,
            score: grade.score,
            notes: grade.notes.trim() || null
          });
        }
      }
    }

    if (grades.length === 0) {
      this.errorMsg = 'No hay notas cargadas para guardar.';
      return;
    }

    this.isSaving = true;
    this.errorMsg = '';
    this.gradebookService.saveGrades(this.detail.gradebook.id, grades).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.rows = detail.students.map(student => this.toEditableRow(student));
        this.selectedStudent = this.rows.find(r => r.enrollmentId === this.selectedStudent?.enrollmentId) ?? this.rows[0] ?? null;
        this.isSaving = false;
        this.successMsg = 'Notas guardadas correctamente.';
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMsg = err.message || 'Error al guardar las notas.';
        this.cdr.detectChanges();
      }
    });
  }

  get isComplete(): boolean {
    if (!this.detail || this.rows.length === 0) return false;
    return this.rows.every(row =>
      this.detail!.evaluations.every(ev => row.editableGrades[ev.id]?.score != null)
    );
  }

  submitGradebook(): void {
    if (!this.detail || this.isSubmitting) return;
    if (!this.isComplete) {
      this.errorMsg = 'Todos los alumnos deben tener una nota en cada instancia antes de enviar la planilla.';
      return;
    }
    if (!confirm('¿Enviar la planilla a Secretaría? Ya no vas a poder editarla salvo que la reabran.')) return;

    this.isSubmitting = true;
    this.errorMsg = '';
    this.gradebookService.submitGradebook(this.detail.gradebook.id).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successMsg = 'Planilla enviada a Secretaría.';
        this.loadDetail(this.detail!.gradebook.id);
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMsg = err.message || 'Error al enviar la planilla.';
        this.cdr.detectChanges();
      }
    });
  }

  downloadSummary(): void {
    if (!this.detail) return;
    const header = ['Alumno', 'Legajo', ...this.detail.evaluations.map(e => e.name), 'Promedio', 'Condición'];
    const lines = [header.join(',')];

    for (const row of this.filteredRows) {
      const cells = this.detail.evaluations.map(ev => row.editableGrades[ev.id]?.score ?? '');
      lines.push([
        row.studentName,
        row.legajoNumber,
        ...cells,
        row.average ?? '',
        this.resultLabel(row.resultStatus)
      ].join(','));
    }

    const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `notas_${this.detail.gradebook.courseName}_${this.detail.gradebook.commissionName}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
