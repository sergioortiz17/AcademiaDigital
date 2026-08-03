import { Component, OnInit } from '@angular/core';
import {
  AttendanceService,
  AttendanceStudentRow
} from '../../../core/services/attendance.service';

@Component({
  selector: 'app-attendance-management',
  templateUrl: './attendance-management.component.html',
  styleUrls: ['./attendance-management.component.scss'],
  standalone: false
})
export class AttendanceManagementComponent implements OnInit {
  careers = ['Desarrollo de Software'];
  selectedCareer = this.careers[0];

  sedes = ['Sede Centro', 'Sede Norte'];
  selectedSede: string | null = null;

  years = [
    { value: 1, label: 'Primero' },
    { value: 2, label: 'Segundo' },
    { value: 3, label: 'Tercero' }
  ];
  selectedYear: number | null = 1;

  private readonly subjectsByCareer: Record<string, string[]> = {
    'Desarrollo de Software': [
      'Base de Datos',
      'Programación I',
      'Programación II',
      'Elementos de Matemática y Lógica',
      'Sistemas y Organizaciones',
      'Redes'
    ]
  };
  selectedSubject = 'Base de Datos';

  currentMonth = new Date(2026, 5, 1);
  searchTerm = '';

  rows: AttendanceStudentRow[] = [];
  selectedStudent: AttendanceStudentRow | null = null;
  uploadedCertificateName: string | null = null;

  constructor(private readonly attendanceService: AttendanceService) {}

  ngOnInit(): void {
    this.loadAttendance();
  }

  get subjects(): string[] {
    return this.subjectsByCareer[this.selectedCareer] ?? [];
  }

  get dayColumns(): number[] {
    const days = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + 1, 0).getDate();
    return Array.from({ length: days }, (_, i) => i + 1);
  }

  get filteredRows(): AttendanceStudentRow[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.rows;
    return this.rows.filter(r => r.fullName.toLowerCase().includes(term));
  }

  yearLabel(value: number | null): string {
    return this.years.find(y => y.value === value)?.label ?? '';
  }

  loadAttendance(): void {
    this.attendanceService
      .getAttendance({
        careerName: this.selectedCareer,
        sede: this.selectedSede,
        yearNumber: this.selectedYear,
        subjectName: this.selectedSubject,
        month: this.currentMonth
      })
      .subscribe(rows => {
        this.rows = rows;
        const preferredId = this.selectedStudent?.id ?? 4;
        this.selectedStudent = rows.find(r => r.id === preferredId) ?? rows[0] ?? null;
      });
  }

  onFilterChange(): void {
    this.loadAttendance();
  }

  clearYear(): void {
    this.selectedYear = null;
    this.onFilterChange();
  }

  prevMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() - 1, 1);
    this.loadAttendance();
  }

  nextMonth(): void {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + 1, 1);
    this.loadAttendance();
  }

  selectStudent(row: AttendanceStudentRow): void {
    this.selectedStudent = row;
    this.uploadedCertificateName = null;
  }

  statusFor(row: AttendanceStudentRow, day: number): string {
    return row.records[day] ?? '';
  }

  formatDay(day: number): string {
    const month = (this.currentMonth.getMonth() + 1).toString().padStart(2, '0');
    return `${day.toString().padStart(2, '0')}/${month}`;
  }

  onCertificateSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.uploadedCertificateName = file.name;
    input.value = '';
  }

  downloadSummary(): void {
    const header = ['Alumno', ...this.dayColumns.map(d => this.formatDay(d)), 'Promedio Asistencia'];
    const lines = [header.join(',')];

    for (const row of this.filteredRows) {
      const cells = this.dayColumns.map(d => this.statusFor(row, d));
      lines.push([row.fullName, ...cells, `${row.averageAttendance}%`].join(','));
    }

    const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `asistencias_${this.selectedSubject}_${this.currentMonth.getMonth() + 1}-${this.currentMonth.getFullYear()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
