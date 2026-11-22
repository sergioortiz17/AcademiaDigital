import { Component, ChangeDetectorRef, Inject, Optional } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import {
  CreateGradebookEvaluationInput,
  GradebookEvaluation,
  GradebookEvaluationLayoutInput
} from '../../../core/services/gradebook.service';

interface EditableEvaluation {
  /** Id de una instancia existente (al editar el formato); null si es nueva. */
  id?: number | null;
  name: string;
  maximumScore: number;
  isRecovery: boolean;
  evaluationDate?: Date | null;
  /** Ya tiene notas: no se puede quitar. */
  hasGrades?: boolean;
}

/** Si viene, el diálogo edita el formato de una planilla existente en vez de crear una. */
export interface EvaluationSetupDialogData {
  evaluations: GradebookEvaluation[];
  evaluationIdsWithGrades: number[];
}

const DEFAULT_EVALUATIONS: EditableEvaluation[] = [
  { name: '1era Instancia', maximumScore: 10, isRecovery: false },
  { name: '2da Instancia', maximumScore: 10, isRecovery: false },
  { name: '3era Instancia', maximumScore: 10, isRecovery: false },
  { name: 'Recuperación 1', maximumScore: 10, isRecovery: true },
  { name: 'Recuperación 2', maximumScore: 10, isRecovery: true },
  { name: 'IEFI', maximumScore: 10, isRecovery: false }
];

/**
 * Reparte el 100% en partes iguales entre las instancias regulares, con dos decimales.
 * Los centésimos que sobran van a las primeras instancias para que la suma dé 100 exacto.
 */
export function distributeWeights(regularCount: number): number[] {
  if (regularCount <= 0) return [];
  const totalCents = 10000;
  const base = Math.floor(totalCents / regularCount);
  const remainder = totalCents - base * regularCount;
  return Array.from({ length: regularCount }, (_, i) => (base + (i < remainder ? 1 : 0)) / 100);
}

@Component({
  selector: 'app-evaluation-setup-dialog',
  templateUrl: './evaluation-setup-dialog.component.html',
  styleUrls: ['./evaluation-setup-dialog.component.scss'],
  standalone: false
})
export class EvaluationSetupDialogComponent {
  evaluations: EditableEvaluation[] = DEFAULT_EVALUATIONS.map(e => ({ ...e }));
  readonly isEdit: boolean;

  constructor(
    public dialogRef: MatDialogRef<EvaluationSetupDialogComponent>,
    private readonly cdr: ChangeDetectorRef,
    @Optional() @Inject(MAT_DIALOG_DATA) data?: EvaluationSetupDialogData | null
  ) {
    this.isEdit = !!data?.evaluations?.length;
    if (data && this.isEdit) {
      const withGrades = new Set(data.evaluationIdsWithGrades);
      this.evaluations = [...data.evaluations]
        .sort((a, b) => a.displayOrder - b.displayOrder)
        .map(e => ({
          id: e.id,
          name: e.name,
          maximumScore: e.maximumScore,
          isRecovery: e.isRecovery,
          evaluationDate: e.evaluationDate ? new Date(e.evaluationDate + 'T12:00:00') : null,
          hasGrades: withGrades.has(e.id)
        }));
    }
  }

  /** Peso que le toca a cada instancia (las recuperaciones no llevan peso propio). */
  get weights(): number[] {
    const regularWeights = distributeWeights(this.evaluations.filter(e => !e.isRecovery).length);
    let next = 0;
    return this.evaluations.map(e => (e.isRecovery ? 0 : regularWeights[next++]));
  }

  get regularCount(): number {
    return this.evaluations.filter(e => !e.isRecovery).length;
  }

  get summary(): string {
    const first = this.weights.find((_, i) => !this.evaluations[i].isRecovery) ?? 0;
    const each = this.regularCount === 1 ? 'vale el 100%' : `vale ${first.toLocaleString('es-AR')}%`;
    return `${this.regularCount} instancia(s) regular(es): cada una ${each} del promedio.`;
  }

  get validationMessage(): string | null {
    if (this.evaluations.length === 0) return 'Agregue al menos una instancia.';
    if (this.evaluations.length > 20) return 'Se permiten hasta 20 instancias.';
    if (this.regularCount === 0) return 'Debe haber al menos una instancia que no sea recuperación.';
    if (this.evaluations.some(e => !e.name.trim())) return 'Todas las instancias necesitan un nombre.';
    if (this.evaluations.some(e => !(Number(e.maximumScore) > 0) || Number(e.maximumScore) > 100))
      return 'La nota máxima debe ser mayor que 0 y hasta 100.';
    const names = this.evaluations.map(e => e.name.trim().toLowerCase());
    if (new Set(names).size !== names.length) return 'No puede haber dos instancias con el mismo nombre.';
    return null;
  }

  get isValid(): boolean {
    return this.validationMessage === null;
  }

  onRecoveryToggle(): void {
    this.cdr.detectChanges();
  }

  addEvaluation(): void {
    if (this.evaluations.length >= 20) return;
    this.evaluations.push({ id: null, name: '', maximumScore: 10, isRecovery: false, evaluationDate: null });
    this.cdr.detectChanges();
  }

  removeEvaluation(index: number): void {
    if (this.evaluations[index]?.hasGrades) return;
    this.evaluations.splice(index, 1);
    this.cdr.detectChanges();
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    if (this.isEdit) {
      const layout: GradebookEvaluationLayoutInput[] = this.evaluations.map(e => ({
        id: e.id ?? null,
        name: e.name.trim(),
        maximumScore: Number(e.maximumScore),
        isRecovery: e.isRecovery,
        evaluationDate: e.evaluationDate ? toDateOnly(e.evaluationDate) : null
      }));
      this.dialogRef.close(layout);
      return;
    }
    const weights = this.weights;
    const result: CreateGradebookEvaluationInput[] = this.evaluations.map((e, i) => ({
      name: e.name.trim(),
      weightPercentage: weights[i],
      maximumScore: Number(e.maximumScore),
      isRecovery: e.isRecovery
    }));
    this.dialogRef.close(result);
  }
}

function toDateOnly(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
