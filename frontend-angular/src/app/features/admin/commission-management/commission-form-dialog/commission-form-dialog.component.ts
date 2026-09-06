import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Commission, UpsertCommissionRequest } from '../../../../core/services/commission.service';

export interface CommissionFormDialogData {
  commission: Commission | null;
}

/**
 * Turnos: el VALUE que se guarda es la convención del backend (Morning/Afternoon/Evening,
 * que es lo que valida y normaliza SaveCommissionAsync). El LABEL es en español para la UI.
 * OJO: Enrollment.Shift usa español (Mañana/Tarde/Noche) — esa discrepancia se resuelve en la
 * Parte 6, no acá; acá se respeta la convención actual de Commission para no romper nada.
 */
const SHIFTS = [
  { value: 'Morning', label: 'Mañana' },
  { value: 'Afternoon', label: 'Tarde' },
  { value: 'Evening', label: 'Noche' }
];

@Component({
  selector: 'app-commission-form-dialog',
  templateUrl: './commission-form-dialog.component.html',
  styleUrls: ['./commission-form-dialog.component.scss'],
  standalone: false
})
export class CommissionFormDialogComponent {
  isEdit: boolean;
  shifts = SHIFTS;

  code = '';
  name = '';
  academicYear = new Date().getFullYear();
  yearNumber = 1;
  shift = 'Morning';

  constructor(
    public dialogRef: MatDialogRef<CommissionFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CommissionFormDialogData
  ) {
    this.isEdit = !!data.commission;
    if (data.commission) {
      this.code = data.commission.code;
      this.name = data.commission.name;
      this.academicYear = data.commission.academicYear;
      this.yearNumber = data.commission.yearNumber;
      this.shift = data.commission.shift;
    }
  }

  static shiftLabel(shift: string): string {
    return SHIFTS.find(s => s.value === shift)?.label ?? shift;
  }

  get isValid(): boolean {
    return !!this.code.trim()
      && !!this.name.trim()
      && this.academicYear >= 2000 && this.academicYear <= 2100
      && this.yearNumber >= 1 && this.yearNumber <= 20
      && SHIFTS.some(s => s.value === this.shift);
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    const request: UpsertCommissionRequest = {
      code: this.code.trim(),
      name: this.name.trim(),
      academicYear: this.academicYear,
      yearNumber: this.yearNumber,
      shift: this.shift
    };
    this.dialogRef.close(request);
  }
}
