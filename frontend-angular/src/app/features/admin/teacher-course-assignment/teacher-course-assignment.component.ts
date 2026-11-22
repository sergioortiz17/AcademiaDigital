import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { forkJoin, of } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { Career, CareerDivision, CareerService, StudyPlanCourse } from '../../../core/services/career.service';
import { TeachingPosition, TeachingPositionService } from '../../../core/services/teaching-position.service';
import { Teacher, TeacherService } from '../../../core/services/teacher.service';

const POSITION_TYPES = [
  { value: 'Titular', label: 'Titular' },
  { value: 'Adjunct', label: 'Adjunto' },
  { value: 'JTP', label: 'JTP' },
  { value: 'Assistant', label: 'Ayudante' }
];

/** Docente soltado sobre una materia, a la espera de que se le elija la división. */
interface PendingTeacher {
  teacher: Teacher;
  divisionId: number | null;
  positionType: string;
  error?: string;
}

interface YearGroup {
  yearNumber: number;
  courses: StudyPlanCourse[];
}

/**
 * Docentes → Asignar materia, con arrastrar y soltar: los docentes son píldoras que se sueltan
 * sobre el cuadro de una materia. Al abrir la materia se elige la división (y el cargo) de cada
 * docente soltado y se guarda. El servidor usa la comisión vacante de esa materia y división o
 * la crea si todavía no existe.
 */
@Component({
  selector: 'app-teacher-course-assignment',
  templateUrl: './teacher-course-assignment.component.html',
  styleUrls: ['./teacher-course-assignment.component.scss'],
  standalone: false
})
export class TeacherCourseAssignmentComponent implements OnInit {
  readonly positionTypes = POSITION_TYPES;

  careers: Career[] = [];
  careerId: number | null = null;
  academicYear = new Date().getFullYear();
  academicYears: number[] = [this.academicYear];

  teachers: Teacher[] = [];
  teacherSearch = '';
  private allDivisions: CareerDivision[] = [];
  years: YearGroup[] = [];
  private sections: TeachingPosition[] = [];

  /** Docentes pendientes por materia (courseId). */
  pending: Record<number, PendingTeacher[]> = {};
  /** Materias con la lista de comisiones desplegada. */
  expanded = new Set<number>();
  /** Materia abierta para elegir divisiones. */
  openCourse: StudyPlanCourse | null = null;

  isLoading = false;
  isSaving = false;
  errorMsg = '';
  successMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly teacherService: TeacherService,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    forkJoin({
      careers: this.careerService.getCareers(),
      teachers: this.teacherService.getTeachers()
    }).subscribe({
      next: ({ careers, teachers }) => {
        this.careers = careers.filter(c => c.isActive !== false);
        this.teachers = teachers.filter(t => t.isActive)
          .sort((a, b) => `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`, 'es'));
        if (this.careers.length >= 1) { this.careerId = this.careers[0].id; this.onCareerChange(); }
        this.cdr.detectChanges();
      },
      error: err => this.fail(err, 'No se pudieron cargar las carreras y los docentes.')
    });
  }

  get filteredTeachers(): Teacher[] {
    const term = this.teacherSearch.trim().toLowerCase();
    if (!term) return this.teachers;
    return this.teachers.filter(t =>
      `${t.lastName} ${t.firstName} ${t.employeeNumber} ${t.email}`.toLowerCase().includes(term));
  }

  teacherName(t: Teacher): string {
    return `${t.lastName}, ${t.firstName}`;
  }

  /** Cantidad de comisiones activas del docente en el ciclo y la carrera elegidos. */
  teacherLoad(t: Teacher): number {
    return this.sections.filter(s => s.teacherId === t.id).length;
  }

  onCareerChange(): void {
    this.pending = {};
    this.expanded.clear();
    this.openCourse = null;
    this.years = [];
    this.clearMessages();
    if (!this.careerId) return;
    const careerId = this.careerId;
    this.isLoading = true;
    forkJoin({
      divisions: this.careerService.getDivisions(careerId),
      courses: this.careerService.getStudyPlans(careerId).pipe(switchMap(plans => {
        const plan = plans.find(p => p.status === 'Active') ?? null;
        return plan ? this.careerService.getStudyPlanCourses(plan.id) : of([] as StudyPlanCourse[]);
      }))
    }).subscribe({
      next: ({ divisions, courses }) => {
        this.allDivisions = divisions.filter(d => d.isActive);
        const years = [...new Set(this.allDivisions.map(d => d.academicYear))].sort((a, b) => b - a);
        this.academicYears = years.length ? years : [new Date().getFullYear()];
        if (!this.academicYears.includes(this.academicYear)) this.academicYear = this.academicYears[0];
        const byYear = new Map<number, StudyPlanCourse[]>();
        for (const course of [...courses].sort((a, b) =>
          a.yearNumber - b.yearNumber || a.semester - b.semester || a.sortOrder - b.sortOrder)) {
          byYear.set(course.yearNumber, [...(byYear.get(course.yearNumber) ?? []), course]);
        }
        this.years = [...byYear.entries()].map(([yearNumber, list]) => ({ yearNumber, courses: list }));
        if (!courses.length) this.errorMsg = 'La carrera no tiene un plan de estudios vigente.';
        this.loadSections();
      },
      error: err => this.fail(err, 'No se pudo cargar el plan de la carrera.')
    });
  }

  onAcademicYearChange(): void {
    this.pending = {};
    this.openCourse = null;
    this.loadSections();
  }

  private loadSections(): void {
    this.isLoading = true;
    const divisionIds = new Set(this.allDivisions.map(d => d.id));
    this.teachingPositionService.getTeachingPositions({ academicYear: this.academicYear })
      .pipe(catchError(() => of([] as TeachingPosition[])))
      .subscribe(sections => {
        this.sections = sections.filter(s => s.isActive && s.divisionId != null && divisionIds.has(s.divisionId));
        this.isLoading = false;
        this.cdr.detectChanges();
      });
  }

  /** Divisiones del ciclo elegido que cursan el año de la materia. */
  divisionsFor(course: StudyPlanCourse): CareerDivision[] {
    return this.allDivisions.filter(d => d.academicYear === this.academicYear && d.yearNumber === course.yearNumber);
  }

  sectionsFor(course: StudyPlanCourse): TeachingPosition[] {
    return this.sections.filter(s => s.courseId === course.courseId)
      .sort((a, b) => (a.divisionName ?? '').localeCompare(b.divisionName ?? '', 'es'));
  }

  pendingFor(course: StudyPlanCourse): PendingTeacher[] {
    return this.pending[course.courseId] ?? [];
  }

  semesterLabel(course: StudyPlanCourse): string {
    return course.isAnnual ? 'Anual' : `${course.semester}° cuatr.`;
  }

  positionLabel(type: string): string {
    return POSITION_TYPES.find(p => p.value === type)?.label ?? type;
  }

  toggleExpanded(course: StudyPlanCourse, event: Event): void {
    event.stopPropagation();
    if (this.expanded.has(course.courseId)) this.expanded.delete(course.courseId);
    else this.expanded.add(course.courseId);
  }

  open(course: StudyPlanCourse): void {
    this.openCourse = this.openCourse?.courseId === course.courseId ? null : course;
    this.clearMessages();
  }

  /** Las píldoras se copian a la materia; la lista de docentes no recibe nada. */
  readonly noDropBack = () => false;

  onDrop(event: CdkDragDrop<StudyPlanCourse>, course: StudyPlanCourse): void {
    const teacher = event.item.data as Teacher;
    const list = this.pending[course.courseId] ?? [];
    if (list.some(p => p.teacher.id === teacher.id)) return;
    // Si la materia tiene una sola división ese año, ya queda elegida.
    const divisions = this.divisionsFor(course);
    this.pending[course.courseId] = [...list, {
      teacher, divisionId: divisions.length === 1 ? divisions[0].id : null, positionType: 'Titular'
    }];
    this.openCourse = course;
    this.clearMessages();
    this.cdr.markForCheck();
  }

  removePending(course: StudyPlanCourse, item: PendingTeacher): void {
    this.pending[course.courseId] = this.pendingFor(course).filter(p => p !== item);
  }

  /** Movimientos sin guardar (en todas las materias). */
  get pendingCount(): number {
    return Object.values(this.pending).reduce((total, list) => total + list.length, 0);
  }

  /** Pendientes a los que todavía les falta elegir la división. */
  get missingDivisionCount(): number {
    return Object.values(this.pending).reduce((total, list) => total + list.filter(p => p.divisionId == null).length, 0);
  }

  get canSave(): boolean {
    return !this.isSaving && this.pendingCount > 0 && this.missingDivisionCount === 0;
  }

  discardAll(): void {
    this.pending = {};
    this.clearMessages();
  }

  /**
   * "Guardar cambios": recién acá se escribe en la base. Guarda todos los docentes movidos, en
   * todas las materias. Los que fallan quedan pendientes con el motivo.
   */
  save(): void {
    if (!this.canSave) return;
    const courses = new Map(this.years.flatMap(y => y.courses).map(c => [c.courseId, c]));
    const ready = Object.entries(this.pending).flatMap(([courseId, list]) =>
      list.map(item => ({ course: courses.get(Number(courseId))!, item })));
    this.isSaving = true;
    this.clearMessages();
    const calls = ready.map(({ course, item }) => {
      const division = this.allDivisions.find(d => d.id === item.divisionId)!;
      return this.teacherService.assignCourses(item.teacher.id, {
        divisionId: division.id,
        courseIds: [course.courseId],
        positionType: item.positionType,
        startedOn: this.startDateFor(division),
        reason: null
      }).pipe(catchError(err => of({ assigned: [], failed: [{ courseId: course.courseId, reason: this.readError(err) }] })));
    });
    forkJoin(calls).subscribe(results => {
      let saved = 0;
      results.forEach((result, index) => {
        const { course, item } = ready[index];
        if (result.assigned.length > 0) {
          saved++;
          this.pending[course.courseId] = this.pendingFor(course).filter(p => p !== item);
          this.expanded.add(course.courseId);
        } else {
          item.error = result.failed[0]?.reason ?? 'No se pudo asignar.';
        }
      });
      for (const key of Object.keys(this.pending)) if (!this.pending[+key].length) delete this.pending[+key];
      this.isSaving = false;
      this.successMsg = saved ? `${saved} asignación(es) guardada(s).` : '';
      if (saved < ready.length) this.errorMsg = 'Algunas asignaciones no se pudieron guardar: siguen pendientes con el motivo.';
      this.loadSections();
    });
  }

  /** Hoy, o el 1 de marzo si el ciclo de la división es otro año. */
  private startDateFor(division: CareerDivision): string {
    const today = new Date();
    const date = today.getFullYear() === division.academicYear ? today : new Date(division.academicYear, 2, 1);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  private clearMessages(): void {
    this.errorMsg = '';
    this.successMsg = '';
  }

  private readError(err: any): string {
    return err?.error?.detail || err?.error?.msg || err?.message || 'No se pudo asignar.';
  }

  private fail(err: any, fallback: string): void {
    this.isLoading = false;
    this.errorMsg = this.readError(err) || fallback;
    this.cdr.detectChanges();
  }
}
