import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { Career, CareerDivision, CareerService, StudyPlanCourse } from '../../../core/services/career.service';
import { TeachingPosition, TeachingPositionService } from '../../../core/services/teaching-position.service';
import { TeacherService } from '../../../core/services/teacher.service';

/** Resultado de un envío: cuántas materias se asignaron y si alguna falló. */
export interface AssignCoursesOutcome {
  assigned: number;
  hasFailures: boolean;
}

type CourseStatus = 'vacant' | 'create' | 'mine' | 'taken';

interface CourseRow {
  course: StudyPlanCourse;
  status: CourseStatus;
  teacherName: string | null;
}

const POSITION_TYPES = [
  { value: 'Titular', label: 'Titular' },
  { value: 'Adjunct', label: 'Adjunto' },
  { value: 'JTP', label: 'Jefe de Trabajos Prácticos' },
  { value: 'Assistant', label: 'Ayudante' }
];

/**
 * Formulario para asignar materias a un docente en un paso: carrera → división → se marcan las materias.
 * Lo usan el diálogo del legajo y la pantalla Docentes → Asignar materia.
 * El servidor usa la comisión vacante de cada materia o la crea si todavía no existe, así que no
 * hace falta crear las comisiones antes en "Comisiones".
 */
@Component({
  selector: 'app-assign-courses-form',
  templateUrl: './assign-courses-form.component.html',
  styleUrls: ['./assign-courses-form.component.scss'],
  standalone: false
})
export class AssignCoursesFormComponent implements OnInit, OnChanges {
  /** Docente al que se asignan las materias. */
  @Input({ required: true }) teacherId!: number;
  /** Se emite después de cada envío. */
  @Output() readonly submitted = new EventEmitter<AssignCoursesOutcome>();

  readonly positionTypes = POSITION_TYPES;

  careers: Career[] = [];
  divisions: CareerDivision[] = [];
  private planCourses: StudyPlanCourse[] = [];
  private sections: TeachingPosition[] = [];
  rows: CourseRow[] = [];

  careerId: number | null = null;
  divisionId: number | null = null;
  positionType = 'Titular';
  startedOn: Date = new Date();
  reason = '';
  showAllYears = false;

  readonly selected = new Set<number>();
  isLoading = false;
  isSubmitting = false;
  errorMsg = '';
  successMsg = '';
  failures: { name: string; reason: string }[] = [];

  constructor(
    private readonly careerService: CareerService,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly teacherService: TeacherService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe({
      next: careers => {
        this.careers = careers;
        if (careers.length === 1) { this.careerId = careers[0].id; this.onCareerChange(); }
        this.cdr.detectChanges();
      },
      error: err => this.fail(err)
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Al cambiar de docente se recalcula qué materias ya tiene.
    if (changes['teacherId'] && !changes['teacherId'].firstChange) {
      this.selected.clear();
      this.clearMessages();
      this.loadSections();
    }
  }

  get division(): CareerDivision | undefined {
    return this.divisions.find(d => d.id === this.divisionId);
  }

  get selectableRows(): CourseRow[] {
    return this.rows.filter(r => r.status === 'vacant' || r.status === 'create');
  }

  get allSelected(): boolean {
    const rows = this.selectableRows;
    return rows.length > 0 && rows.every(r => this.selected.has(r.course.courseId));
  }

  get someSelected(): boolean {
    return this.selected.size > 0 && !this.allSelected;
  }

  onCareerChange(): void {
    this.divisionId = null;
    this.divisions = [];
    this.planCourses = [];
    this.rows = [];
    this.selected.clear();
    this.clearMessages();
    if (!this.careerId) return;

    this.isLoading = true;
    const careerId = this.careerId;
    forkJoin({
      divisions: this.careerService.getDivisions(careerId),
      courses: this.careerService.getStudyPlans(careerId).pipe(switchMap(plans => {
        const plan = plans.find(p => p.status === 'Active') ?? null;
        return plan ? this.careerService.getStudyPlanCourses(plan.id) : of([] as StudyPlanCourse[]);
      }))
    }).subscribe({
      next: ({ divisions, courses }) => {
        this.divisions = divisions.filter(d => d.isActive);
        this.planCourses = [...courses].sort((a, b) =>
          a.yearNumber - b.yearNumber || a.semester - b.semester || a.sortOrder - b.sortOrder);
        if (!courses.length) this.errorMsg = 'La carrera no tiene un plan de estudios vigente.';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: err => this.fail(err)
    });
  }

  onDivisionChange(): void {
    this.selected.clear();
    this.clearMessages();
    const division = this.division;
    if (!division) { this.rows = []; return; }
    // La fecha de inicio tiene que caer en el año académico de la división.
    if (this.startedOn.getFullYear() !== division.academicYear) {
      this.startedOn = new Date(division.academicYear, 2, 1);
    }
    this.loadSections();
  }

  onPositionTypeChange(): void {
    this.selected.clear();
    this.buildRows();
  }

  private loadSections(): void {
    const division = this.division;
    if (!division) return;
    this.isLoading = true;
    this.teachingPositionService.getTeachingPositions({ academicYear: division.academicYear }).subscribe({
      next: sections => {
        this.sections = sections.filter(s => s.divisionId === division.id && s.isActive);
        this.isLoading = false;
        this.buildRows();
      },
      error: err => this.fail(err)
    });
  }

  buildRows(): void {
    const division = this.division;
    if (!division) { this.rows = []; return; }
    this.rows = this.planCourses
      .filter(c => this.showAllYears || c.yearNumber === division.yearNumber)
      .map(course => {
        const same = this.sections.filter(s => s.courseId === course.courseId && s.positionType === this.positionType);
        if (same.some(s => s.isVacant && !s.teacherId)) return { course, status: 'vacant' as const, teacherName: null };
        const taken = same.find(s => s.teacherId);
        if (taken) {
          const mine = taken.teacherId === this.teacherId;
          return { course, status: mine ? 'mine' as const : 'taken' as const, teacherName: taken.teacherName };
        }
        return { course, status: 'create' as const, teacherName: null };
      });
    for (const id of [...this.selected]) {
      if (!this.selectableRows.some(r => r.course.courseId === id)) this.selected.delete(id);
    }
    this.cdr.detectChanges();
  }

  toggle(row: CourseRow, checked: boolean): void {
    if (row.status !== 'vacant' && row.status !== 'create') return;
    if (checked) this.selected.add(row.course.courseId); else this.selected.delete(row.course.courseId);
  }

  toggleAll(checked: boolean): void {
    for (const row of this.selectableRows) this.toggle(row, checked);
  }

  semesterLabel(course: StudyPlanCourse): string {
    return course.isAnnual ? 'Anual' : `${course.semester}° cuatr.`;
  }

  get canSubmit(): boolean {
    return !!this.divisionId && this.selected.size > 0 && !!this.startedOn && !this.isSubmitting;
  }

  submit(): void {
    const division = this.division;
    if (!division || !this.canSubmit) return;
    this.clearMessages();
    this.isSubmitting = true;
    const names = new Map(this.planCourses.map(c => [c.courseId, c.courseName]));

    this.teacherService.assignCourses(this.teacherId, {
      divisionId: division.id,
      courseIds: [...this.selected],
      positionType: this.positionType,
      startedOn: this.toIsoDate(this.startedOn),
      reason: this.reason.trim() || null
    }).subscribe({
      next: res => {
        this.isSubmitting = false;
        this.successMsg = res.assigned.length ? `${res.assigned.length} materia(s) asignada(s).` : '';
        this.failures = res.failed.map(f => ({ name: names.get(f.courseId) ?? `Materia ${f.courseId}`, reason: f.reason }));
        this.selected.clear();
        this.submitted.emit({ assigned: res.assigned.length, hasFailures: res.failed.length > 0 });
        this.loadSections();
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
    this.errorMsg = err?.error?.msg || err?.error?.detail || err?.message || 'Ocurrió un error.';
    this.cdr.detectChanges();
  }

  private toIsoDate(date: Date): string {
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
