import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Commission, UpsertCommissionRequest } from '../../../../core/services/commission.service';

export interface CommissionFormDialogData {
  commission: Commission | null;
  /** Precargas al crear (atajo desde el aviso de cobertura de la Parte 11 / 13). */
  presetAcademicYear?: number | null;
  presetYearNumber?: number | null;
  presetShift?: string | null;
}

/**
 * Turnos: el VALUE guardado ahora es en español (Mañana/Tarde/Noche), unificado con
 * Enrollment.Shift y con las constantes de dominio EnrollmentCapacityPolicy. Esto es lo que
 * permite el matching automático de comisión al inscribirse (Parte 6): antes Commission.Shift
 * se guardaba en inglés y nunca coincidía con el turno de la inscripción.
 */
const SHIFTS = [
  { value: 'Mañana', label: 'Mañana' },
  { value: 'Tarde', label: 'Tarde' },
  { value: 'Noche', label: 'Noche' }
];

// AcademicYear es AÑO CALENDARIO real (2024, 2025, ...), NO un número de ciclo (1,2,3).
// Es la convención única del sistema: todos los DTOs del backend validan [Range(2000, 2100)] y el
// auto-match de la Parte 6 compara Period.AcademicYear == Commission.AcademicYear por año calendario.
// Este rango coincide con esa validación del backend (no es un número arbitrario).
const MIN_ACADEMIC_YEAR = 2000;
const MAX_ACADEMIC_YEAR = 2100;

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
  shift = 'Mañana';

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
    } else if (data.presetAcademicYear) {
      this.academicYear = data.presetAcademicYear;
    }
    if (!data.commission) {
      if (data.presetYearNumber) this.yearNumber = data.presetYearNumber;
      if (data.presetShift && SHIFTS.some(s => s.value === data.presetShift)) this.shift = data.presetShift!;
    }
  }

  static shiftLabel(shift: string): string {
    return SHIFTS.find(s => s.value === shift)?.label ?? shift;
  }

  get isValid(): boolean {
    return !!this.code.trim()
      && !!this.name.trim()
      && this.academicYear >= MIN_ACADEMIC_YEAR && this.academicYear <= MAX_ACADEMIC_YEAR
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
