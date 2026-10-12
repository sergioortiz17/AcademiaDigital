import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { finalize } from 'rxjs';
import { Store } from '@ngrx/store';
import { Career, CareerDivision, CareerService, StudyPlan, StudyPlanCourse} from '../../core/services/career.service';
import { TeacherService } from '../../core/services/teacher.service';
import { TeachingPosition, TeachingPositionService } from '../../core/services/teaching-position.service';
import { selectUserRole } from '../../store/account/account.selectors';
import { UserRole } from '../../store/account/account.actions';

type TeachersTab = 'assignments' | 'subjects';

interface TeacherTableAssignment {
  courseSectionId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  divisionId: number | null;
  divisionCode: string | null;
  divisionName: string | null;
  academicYear: number;
  semester: number;
  positionType: string;
  isCurrent: boolean;
}

@Component({
  selector: 'app-teachers',
  templateUrl: './teachers.component.html',
  styleUrls: ['./teachers.component.scss'],
  standalone: false
})
export class TeachersComponent implements OnInit {

  UserRole = UserRole;
  activeTab: TeachersTab = 'assignments';
  userRole: UserRole | null = null;

  careers: Career[] = [];
  selectedCareerId: number | null = null;
  selectedStudyPlan: StudyPlan | null = null;
  studyPlanCourses: StudyPlanCourse[] = [];
  careerDivisions: CareerDivision[] = [];
  assignments: TeacherTableAssignment[] = [];

  isCareersLoading = true;
  isLoading = false;
  isStudyPlanLoading = false;
  errorMsg = '';

  searchTerm = '';
  subjectSearchTerm = '';
  selectedCareerYear: number | null = null;

  constructor(
    private readonly teacherService: TeacherService,
    private readonly careerService: CareerService,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly store: Store,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCareers();
    this.store.select(selectUserRole).subscribe(role => {
      this.userRole = role as UserRole;
      if (this.userRole === UserRole.Profesor) {
        this.loadAssignments();
      } else if (this.userRole === UserRole.Admin) {
        this.loadAllTeachingPositions();
      }
    });
  }

  loadCareers(): void {
    this.careerService.getCareers().subscribe({
      next: careers => {
        this.careers = careers ?? [];
        this.isCareersLoading = false;
        this.cdr.detectChanges();
      },
      error: err => {
        this.isCareersLoading = false;
        this.errorMsg =
          err.error?.msg ||
          err.error?.message ||
          'No se pudieron cargar las carreras.';
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Obtiene las asignaciones del docente autenticado.
   */
  loadAssignments(): void {
    this.isLoading = true;
    this.errorMsg = '';

    this.teacherService
      .getMyAssignments(false)
      .pipe(
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: assignments => {
          this.assignments = (assignments ?? []).map(assignment => ({
            courseSectionId: assignment.courseSectionId,
            courseId: assignment.courseId,
            courseCode: assignment.courseCode,
            courseName: assignment.courseName,
            divisionId: assignment.divisionId,
            divisionCode: assignment.divisionCode,
            divisionName: assignment.divisionName,
            academicYear: assignment.academicYear,
            semester: assignment.semester,
            positionType: assignment.positionType,
            isCurrent: assignment.isCurrent
          }));
          this.cdr.detectChanges();
        },
        error: err => {
          this.errorMsg =
            err.error?.msg ||
            err.error?.message ||
            'No se pudieron cargar tus asignaciones.';

          this.assignments = [];
          this.cdr.detectChanges();
        }
      });
  }

  loadAllTeachingPositions(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.teachingPositionService
      .getTeachingPositions({ includeInactive: false })
      .pipe(finalize(() => {
        this.isLoading = false;
      }))
      .subscribe({
        next: positions => {
          this.assignments = positions.map(position => this.toTableAssignment(position));
          this.cdr.detectChanges();
        },
        error: err => {
          this.errorMsg =
            err.error?.msg ||
            err.error?.message ||
            'No se pudieron cargar las comisiones.';
          this.assignments = [];
          this.cdr.detectChanges();
        }
      });
  }

  private toTableAssignment(position: TeachingPosition): TeacherTableAssignment {
    return {
      courseSectionId: position.id,
      courseId: position.courseId,
      courseCode: position.courseCode,
      courseName: position.courseName,
      divisionId: position.divisionId,
      divisionCode: position.divisionCode,
      divisionName: position.divisionName,
      academicYear: position.academicYear,
      semester: position.semester,
      positionType: position.positionType,
      isCurrent: position.isActive
    };
  }

  changeTab(tab: TeachersTab): void {
    this.activeTab = tab;
  }

  /**
   * Años curriculares disponibles en el plan de estudio activo seleccionado.
   */
  get availableYears(): number[] {
    return [
      ...new Set(
        this.studyPlanCourses.map(course => course.yearNumber)
      )
    ].sort((a, b) => a - b);
  }

  get selectedCareer(): Career | undefined {
    return this.careers.find(career => career.id === this.selectedCareerId);
  }

  get careerDuration(): number | null {
    return this.selectedCareer?.durationYears ?? null;
  }

  careerYearLabel(year: number): string {
    const labels: Record<number, string> = {
      1: 'Primero',
      2: 'Segundo',
      3: 'Tercero',
      4: 'Cuarto',
      5: 'Quinto',
      6: 'Sexto'
    };

    return labels[year] ?? `${year}°`;
  }

  onCareerChange(careerId: number | null): void {
    this.selectedCareerId = careerId;
    this.selectedStudyPlan = null;
    this.studyPlanCourses = [];
    this.careerDivisions = [];
    this.selectedCareerYear = null;
    this.errorMsg = '';

    if (careerId == null) {
      this.isStudyPlanLoading = false;
      return;
    }

    this.isStudyPlanLoading = true;
    this.careerService.getStudyPlans(careerId).subscribe({
      next: plans => {
        if (this.selectedCareerId !== careerId) return;

        const activePlan = plans
          .filter(plan => plan.isActive && plan.status === 'Active')
          .sort((a, b) => b.versionNumber - a.versionNumber)[0];
        if (!activePlan) {
          this.isStudyPlanLoading = false;
          this.errorMsg = 'La carrera no tiene un plan de estudio activo.';
          this.cdr.detectChanges();
          return;
        }
        this.selectedStudyPlan = activePlan;

        this.careerService.getStudyPlanCourses(activePlan.id).subscribe({
          next: courses => {
            if (this.selectedCareerId !== careerId) return;
            this.studyPlanCourses = courses;
            this.isStudyPlanLoading = false;
            this.cdr.detectChanges();
          },
          error: err => {
            if (this.selectedCareerId !== careerId) return;
            this.isStudyPlanLoading = false;
            this.errorMsg =
              err.error?.msg ||
              err.error?.message ||
              'No se pudieron cargar las materias del plan de estudio.';
            this.cdr.detectChanges();
          }
        });

        if (this.userRole === UserRole.Admin) {
          this.careerService.getDivisions(careerId).subscribe({
            next: divisions => {
              if (this.selectedCareerId !== careerId) return;
              this.careerDivisions = divisions;
              this.cdr.detectChanges();
            },
            error: err => {
              if (this.selectedCareerId !== careerId) return;
              this.errorMsg =
                err.error?.msg ||
                err.error?.message ||
                'No se pudieron cargar las comisiones de la carrera.';
              this.cdr.detectChanges();
            }
          });
        }
      },
      error: err => {
        if (this.selectedCareerId !== careerId) return;
        this.isStudyPlanLoading = false;
        this.errorMsg =
          err.error?.msg ||
          err.error?.message ||
          'No se pudieron cargar los planes de estudio de la carrera.';
        this.cdr.detectChanges();
      }
    });
  }

  private get careerYearAssignments(): TeacherTableAssignment[] {
    if (this.selectedCareerId == null) return [];

    const courseIds = new Set(
      this.studyPlanCourses
        .filter(course => this.selectedCareerYear == null || course.yearNumber === this.selectedCareerYear)
        .map(course => course.courseId)
    );

    return this.assignments.filter(assignment =>
      courseIds.has(assignment.courseId) &&
      assignment.isCurrent
    );
  }

  careerYearForCourse(courseId: number): number | null {
    return this.studyPlanCourses.find(course => course.courseId === courseId)?.yearNumber ?? null;
  }

  careerYearLabelForCourse(courseId: number): string {
    const year = this.careerYearForCourse(courseId);
    return year == null ? '' : this.careerYearLabel(year);
  }

  divisionShift(divisionId: number | null): string | null {
    return this.careerDivisions.find(division => division.id === divisionId)?.shift ?? null;
  }

  /**
   * Asignaciones filtradas para la primera planilla.
   */
  get filteredAssignments(): TeacherTableAssignment[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.careerYearAssignments.filter(a => {
      const matchesSearch =
        !term ||
        a.courseName.toLowerCase().includes(term) ||
        a.courseCode.toLowerCase().includes(term) ||
        (a.divisionName ?? '').toLowerCase().includes(term) ||
        (a.divisionCode ?? '').toLowerCase().includes(term);

      return matchesSearch;
    });
  }

  /** * Materias del plan de estudio seleccionado, filtradas por año y búsqueda.*/
  get assignedSubjects(): StudyPlanCourse[] {
    if (this.userRole !== UserRole.Admin && this.userRole !== UserRole.Profesor) return [];

    const term = this.subjectSearchTerm.trim().toLowerCase();
    const assignedCourseIds = this.userRole === UserRole.Profesor
      ? new Set(this.assignments
        .filter(assignment => assignment.isCurrent)
        .map(assignment => assignment.courseId))
      : null;

    return this.studyPlanCourses
      .filter(course => this.selectedCareerYear == null || course.yearNumber === this.selectedCareerYear)
      .filter(course => assignedCourseIds == null || assignedCourseIds.has(course.courseId))
      .filter(course =>
        !term ||
        course.courseName.toLowerCase().includes(term) ||
        course.courseCode.toLowerCase().includes(term)
      )
      .sort((a, b) =>
        a.yearNumber - b.yearNumber ||
        a.sortOrder - b.sortOrder ||
        a.courseName.localeCompare(b.courseName)
      );
  }

}