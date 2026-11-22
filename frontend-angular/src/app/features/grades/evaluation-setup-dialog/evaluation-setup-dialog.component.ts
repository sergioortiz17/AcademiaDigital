import { Component, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { CreateGradebookEvaluationInput } from '../../../core/services/gradebook.service';

interface EditableEvaluation {
  name: string;
  maximumScore: number;
  isRecovery: boolean;
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

  constructor(
    public dialogRef: MatDialogRef<EvaluationSetupDialogComponent>,
    private readonly cdr: ChangeDetectorRef
  ) {}

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
    this.evaluations.push({ name: '', maximumScore: 10, isRecovery: false });
    this.cdr.detectChanges();
  }

  removeEvaluation(index: number): void {
    this.evaluations.splice(index, 1);
    this.cdr.detectChanges();
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
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
