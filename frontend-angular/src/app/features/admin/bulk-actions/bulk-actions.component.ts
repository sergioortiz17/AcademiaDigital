import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import Swal from 'sweetalert2';
import { Career, CareerDivision, CareerService } from '../../../core/services/career.service';
import { StudentListItem, StudentService } from '../../../core/services/student.service';
import { BulkActionsService, BulkEnrollResult, BulkGraduateResult } from '../../../core/services/bulk-actions.service';

/** Acciones masivas: egresar alumnos con fecha e inscribir una división en las materias de su año. */
@Component({
  selector: 'app-bulk-actions',
  templateUrl: './bulk-actions.component.html',
  styleUrls: ['./bulk-actions.component.scss'],
  standalone: false
})
export class BulkActionsComponent implements OnInit {
  careers: Career[] = [];

  // --- Egreso ---
  graduateCareerId: number | null = null;
  students: StudentListItem[] = [];
  selected = new Set<number>();
  search = '';
  onlyFinalYear = true;
  graduatedOn = new Date().toISOString().slice(0, 10);
  reason = '';
  loadingStudents = false;
  graduating = false;
  graduateResult: BulkGraduateResult | null = null;

  // --- Inscripción de una división ---
  enrollCareerId: number | null = null;
  divisions: CareerDivision[] = [];
  divisionId: number | null = null;
  semester = 1;
  enrolling = false;
  enrollResult: BulkEnrollResult | null = null;

  errorMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly studentService: StudentService,
    private readonly bulk: BulkActionsService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe(careers => {
      this.careers = careers;
      this.cdr.markForCheck();
    });
  }

  // ---------- Egreso ----------
  loadStudents(): void {
    if (this.graduateCareerId == null) return;
    this.loadingStudents = true;
    this.selected.clear();
    this.graduateResult = null;
    this.studentService.getAllStudentsOfCareer(this.graduateCareerId).subscribe({
      next: students => {
        this.students = students.filter(s => s.status !== 'Graduated' && s.status !== 'Withdrawn');
        this.loadingStudents = false;
        this.cdr.markForCheck();
      },
      error: err => {
        this.errorMsg = err?.message || 'No se pudieron cargar los alumnos.';
        this.loadingStudents = false;
        this.cdr.markForCheck();
      }
    });
  }

  get maxYear(): number {
    return Math.max(0, ...this.students.map(s => s.yearNumber ?? 0));
  }

  get visibleStudents(): StudentListItem[] {
    const term = this.search.trim().toLowerCase();
    return this.students
      .filter(s => !this.onlyFinalYear || this.maxYear === 0 || s.yearNumber === this.maxYear)
      .filter(s => !term || s.fullName.toLowerCase().includes(term) || (s.legajoNumber ?? '').includes(term) || (s.dni ?? '').includes(term));
  }

  toggle(id: number, checked: boolean): void {
    if (checked) this.selected.add(id); else this.selected.delete(id);
  }

  get allVisibleSelected(): boolean {
    return this.visibleStudents.length > 0 && this.visibleStudents.every(s => this.selected.has(s.id));
  }

  toggleAll(checked: boolean): void {
    for (const s of this.visibleStudents) this.toggle(s.id, checked);
  }

  async graduate(): Promise<void> {
    if (this.selected.size === 0 || this.reason.trim().length < 3) return;
    const confirm = await Swal.fire({
      icon: 'question',
      titleText: `¿Marcar ${this.selected.size} alumno(s) como egresados?`,
      text: `Fecha de egreso: ${this.graduatedOn.split('-').reverse().join('/')}. Queda registrado en el historial de cada alumno.`,
      showCancelButton: true, confirmButtonText: 'Sí, egresar', cancelButtonText: 'Cancelar'
    });
    if (!confirm.isConfirmed) return;
    this.graduating = true;
    this.bulk.graduate([...this.selected], this.graduatedOn, this.reason.trim()).subscribe({
      next: result => {
        this.graduateResult = result;
        this.graduating = false;
        this.loadStudents();
      },
      error: err => {
        this.errorMsg = err?.message || 'No se pudo registrar el egreso.';
        this.graduating = false;
        this.cdr.markForCheck();
      }
    });
  }

  studentName(id: number): string {
    return this.students.find(s => s.id === id)?.fullName ?? `#${id}`;
  }

  // ---------- Inscripción ----------
  loadDivisions(): void {
    this.divisionId = null;
    this.divisions = [];
    if (this.enrollCareerId == null) return;
    this.careerService.getDivisions(this.enrollCareerId).subscribe(divisions => {
      this.divisions = divisions.filter(d => d.isActive)
        .sort((a, b) => b.academicYear - a.academicYear || a.yearNumber - b.yearNumber || a.name.localeCompare(b.name));
      this.cdr.markForCheck();
    });
  }

  async enroll(): Promise<void> {
    const division = this.divisions.find(d => d.id === this.divisionId);
    if (!division) return;
    const confirm = await Swal.fire({
      icon: 'question',
      titleText: `¿Inscribir a la división ${division.code}?`,
      text: `Todos los alumnos asignados a la división quedan inscriptos en las materias de ${division.yearNumber}.º año `
        + `del ${this.semester === 1 ? '1.er cuatrimestre (y las anuales)' : '2.º cuatrimestre'} de ${division.academicYear}. Las inscripciones que ya existen no se duplican.`,
      showCancelButton: true, confirmButtonText: 'Inscribir', cancelButtonText: 'Cancelar'
    });
    if (!confirm.isConfirmed) return;
    this.enrolling = true;
    this.enrollResult = null;
    this.bulk.enrollDivision(division.id, this.semester).subscribe({
      next: result => {
        this.enrollResult = result;
        this.enrolling = false;
        this.cdr.markForCheck();
      },
      error: err => {
        this.errorMsg = err?.message || 'No se pudo inscribir a la división.';
        this.enrolling = false;
        this.cdr.markForCheck();
      }
    });
  }
}
