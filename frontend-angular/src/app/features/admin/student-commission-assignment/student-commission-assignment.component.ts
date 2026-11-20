import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CareerService, Career } from '../../../core/services/career.service';
import { CommissionService, Commission } from '../../../core/services/commission.service';
import { StudentService, StudentListItem } from '../../../core/services/student.service';

/**
 * Asignar alumnos a una división, de a varios: se elige carrera y división, se marcan los alumnos
 * (con búsqueda y "Seleccionar todos") y se asignan en un solo paso. El plan de estudios, el año
 * académico y el año del plan los toma el servidor del plan vigente de cada alumno y de la división.
 */
@Component({
  selector: 'app-student-commission-assignment',
  templateUrl: './student-commission-assignment.component.html',
  styleUrls: ['./student-commission-assignment.component.scss'],
  standalone: false
})
export class StudentCommissionAssignmentComponent implements OnInit {
  careers: Career[] = [];
  divisions: Commission[] = [];
  students: StudentListItem[] = [];

  selectedCareerId: number | null = null;
  selectedDivisionId: number | null = null;
  search = '';
  onlyWithoutDivision = false;
  reason = '';

  readonly selected = new Set<number>();

  isLoading = false;
  isSubmitting = false;
  errorMsg = '';
  successMsg = '';
  failures: { name: string; reason: string }[] = [];

  constructor(
    private readonly careerService: CareerService,
    private readonly commissionService: CommissionService,
    private readonly studentService: StudentService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe({
      next: careers => { this.careers = careers; this.cdr.detectChanges(); },
      error: err => this.fail(err)
    });
  }

  get selectedDivision(): Commission | undefined {
    return this.divisions.find(d => d.id === this.selectedDivisionId);
  }

  /** Alumnos visibles según la búsqueda y el filtro "solo sin división". */
  get visibleStudents(): StudentListItem[] {
    const term = this.search.trim().toLowerCase();
    return this.students.filter(s =>
      (!this.onlyWithoutDivision || !s.divisionId) &&
      (!term || s.fullName.toLowerCase().includes(term) || s.legajoNumber?.toLowerCase().includes(term)
        || (s.dni ?? '').includes(term)));
  }

  get allVisibleSelected(): boolean {
    const visible = this.visibleStudents;
    return visible.length > 0 && visible.every(s => this.selected.has(s.id));
  }

  get someVisibleSelected(): boolean {
    return this.visibleStudents.some(s => this.selected.has(s.id)) && !this.allVisibleSelected;
  }

  onCareerChange(): void {
    this.students = [];
    this.divisions = [];
    this.selected.clear();
    this.selectedDivisionId = null;
    this.clearMessages();
    if (!this.selectedCareerId) return;

    this.isLoading = true;
    this.studentService.getAllStudentsOfCareer(this.selectedCareerId).subscribe({
      next: students => {
        this.students = [...students].sort((a, b) => a.fullName.localeCompare(b.fullName));
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: err => this.fail(err)
    });
    this.commissionService.getCommissions(this.selectedCareerId).subscribe({
      next: divisions => { this.divisions = divisions.filter(d => d.isActive); this.cdr.detectChanges(); },
      error: err => this.fail(err)
    });
  }

  toggle(student: StudentListItem, checked: boolean): void {
    if (checked) this.selected.add(student.id); else this.selected.delete(student.id);
  }

  /** "Seleccionar todos": marca o desmarca los alumnos visibles (respeta la búsqueda). */
  toggleAllVisible(checked: boolean): void {
    for (const s of this.visibleStudents) {
      if (checked) this.selected.add(s.id); else this.selected.delete(s.id);
    }
  }

  canSubmit(): boolean {
    return !!this.selectedDivisionId && this.selected.size > 0 && !this.isSubmitting;
  }

  submit(): void {
    const division = this.selectedDivision;
    if (!division || !this.selectedCareerId || this.selected.size === 0) return;
    this.clearMessages();
    this.isSubmitting = true;
    const names = new Map(this.students.map(s => [s.id, s.fullName]));

    this.studentService.assignStudentsToDivision(this.selectedCareerId, division.id, [...this.selected],
      this.reason.trim() || null).subscribe({
      next: res => {
        this.isSubmitting = false;
        for (const id of res.assigned) {
          this.selected.delete(id);
          const s = this.students.find(x => x.id === id);
          if (s) { s.divisionId = division.id; s.divisionName = division.name; }
        }
        this.successMsg = res.assigned.length
          ? `${res.assigned.length} alumno(s) asignado(s) a la división ${division.code} (año ${division.academicYear}, ${division.yearNumber}° año del plan).`
          : '';
        this.failures = res.failed.map(f => ({ name: names.get(f.studentId) ?? `Alumno ${f.studentId}`, reason: f.reason }));
        this.cdr.detectChanges();
      },
      error: err => { this.isSubmitting = false; this.fail(err); }
    });
  }

  private clearMessages(): void {
    this.errorMsg = '';
    this.successMsg = '';
    this.failures = [];
  }

  private fail(err: any): void {
    this.isLoading = false;
    this.errorMsg = err?.error?.msg || err?.message || 'Ocurrió un error.';
    this.cdr.detectChanges();
  }
}
