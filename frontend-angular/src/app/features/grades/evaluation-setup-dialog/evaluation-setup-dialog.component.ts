import { Component, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { CreateGradebookEvaluationInput } from '../../../core/services/gradebook.service';

interface EditableEvaluation {
  name: string;
  weightPercentage: number;
  maximumScore: number;
  isRecovery: boolean;
}

const DEFAULT_EVALUATIONS: EditableEvaluation[] = [
  { name: '1era Instancia', weightPercentage: 34, maximumScore: 10, isRecovery: false },
  { name: '2da Instancia', weightPercentage: 33, maximumScore: 10, isRecovery: false },
  { name: '3era Instancia', weightPercentage: 33, maximumScore: 10, isRecovery: false },
  { name: 'Recuperación 1', weightPercentage: 0, maximumScore: 10, isRecovery: true },
  { name: 'Recuperación 2', weightPercentage: 0, maximumScore: 10, isRecovery: true }
];

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

  // Solo las instancias regulares suman peso; las recuperaciones no llevan peso propio.
  get totalWeight(): number {
    return this.evaluations
      .filter(e => !e.isRecovery)
      .reduce((sum, e) => sum + (Number(e.weightPercentage) || 0), 0);
  }

  get isValid(): boolean {
    if (this.evaluations.length === 0 || this.evaluations.length > 20) return false;
    const regular = this.evaluations.filter(e => !e.isRecovery);
    if (regular.length === 0) return false;
    if (this.evaluations.some(e => !e.name.trim() || e.maximumScore <= 0)) return false;
    // Instancias regulares: peso > 0. Recuperaciones: peso 0.
    if (regular.some(e => e.weightPercentage <= 0)) return false;
    if (this.evaluations.some(e => e.isRecovery && Number(e.weightPercentage) !== 0)) return false;
    const names = this.evaluations.map(e => e.name.trim().toLowerCase());
    if (new Set(names).size !== names.length) return false;
    return this.totalWeight === 100;
  }

  onRecoveryToggle(e: EditableEvaluation): void {
    // Al marcar recuperación forzamos peso 0 (no lleva peso propio).
    if (e.isRecovery) e.weightPercentage = 0;
    this.cdr.detectChanges();
  }

  addEvaluation(): void {
    if (this.evaluations.length >= 20) return;
    this.evaluations.push({ name: '', weightPercentage: 0, maximumScore: 10, isRecovery: false });
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
    const result: CreateGradebookEvaluationInput[] = this.evaluations.map(e => ({
      name: e.name.trim(),
      weightPercentage: Number(e.weightPercentage),
      maximumScore: Number(e.maximumScore),
      isRecovery: e.isRecovery
    }));
    this.dialogRef.close(result);
  }
}
