import { Component, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { CreateGradebookEvaluationInput } from '../../../core/services/gradebook.service';

interface EditableEvaluation {
  name: string;
  weightPercentage: number;
  maximumScore: number;
}

const DEFAULT_EVALUATIONS: EditableEvaluation[] = [
  { name: '1era Instancia', weightPercentage: 20, maximumScore: 10 },
  { name: '2da Instancia', weightPercentage: 20, maximumScore: 10 },
  { name: '3era Instancia', weightPercentage: 20, maximumScore: 10 },
  { name: 'Recuperación 1', weightPercentage: 20, maximumScore: 10 },
  { name: 'Recuperación 2', weightPercentage: 20, maximumScore: 10 }
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

  get totalWeight(): number {
    return this.evaluations.reduce((sum, e) => sum + (Number(e.weightPercentage) || 0), 0);
  }

  get isValid(): boolean {
    if (this.evaluations.length === 0 || this.evaluations.length > 20) return false;
    if (this.evaluations.some(e => !e.name.trim() || e.weightPercentage <= 0 || e.maximumScore <= 0)) return false;
    const names = this.evaluations.map(e => e.name.trim().toLowerCase());
    if (new Set(names).size !== names.length) return false;
    return this.totalWeight === 100;
  }

  addEvaluation(): void {
    if (this.evaluations.length >= 20) return;
    this.evaluations.push({ name: '', weightPercentage: 0, maximumScore: 10 });
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
      maximumScore: Number(e.maximumScore)
    }));
    this.dialogRef.close(result);
  }
}
