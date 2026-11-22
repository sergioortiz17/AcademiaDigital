import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CareerService } from '../../../core/services/career.service';
import { SubjectService } from '../../../core/services/subject.service';
import { EnrollmentService, EnrollmentPeriodDto, MyEnrollmentPeriodDto } from '../../../core/services/enrollment. service';
import { EnrollmentSuccessDialogComponent } from './enrollment-success-dialog.component';
import { HttpErrorResponse } from '@angular/common/http';
import Swal from 'sweetalert2';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { EligibleCourse, StudentService } from '../../../core/services/student.service';

export interface Career {
  success: boolean;
  id: number;
  name: string;
  code: string;
  description: string;
  totalCredits: number;
  durationYears: number;
  isActive: boolean;
  createdAt: string;
}

export interface StudyPlan {
  id: number;
  careerId: number;
  code: string;
  name: string;
  versionNumber: number;
  status: string;
  effectiveFrom: string;
  effectiveTo: string;
  isActive: boolean;
}

export interface Subject {
  id: number;
  studyPlanId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  yearNumber: number;
  semester: number;
  isAnnual: boolean;
  sortOrder: number;
  isMandatory: boolean;
  credits: number;
  workloadHours: number;
  courseType: null;
  eligibilityStatus?: EligibleCourse['eligibilityStatus'];
}

@Component({
  selector: 'app-enrollment-form',
  templateUrl: './enrollment-form.component.html',
  styleUrls: ['./enrollment-form.component.scss'],
  standalone: false
})
export class EnrollmentFormComponent implements OnInit {
  careers: Career[] = [];
  subjects: Subject[] = [];
  firstYearSubjects: Subject[] = [];
  secondYearSubjects: Subject[] = [];
  thirdYearSubjects: Subject[] = [];

  selectedCareer: number | null = null;
  activePeriod: EnrollmentPeriodDto | null = null;
  /** Inscripción que el alumno ya envió en el período abierto (si la hay, no se muestra el formulario). */
  existingEnrollment: MyEnrollmentPeriodDto | null = null;
  isCancelling = false;
  checkingPeriod = false;

  selectedYear: number | null = null;
  availableYears = [
    { id: 1, name: 'Primero' },
    { id: 2, name: 'Segundo' },
    { id: 3, name: 'Tercero' }
  ];
  selectedYears: number[] = [];

  selectedSubjectsByYear: Record<number, number[]> = { 1: [], 2: [], 3: [] };

  shifts = ['Mañana', 'Tarde', 'Noche'];
  selectedShift = '';

  isSubmitting = false;
  errorMsg = '';

  requiredDocuments = [
    { id: 1, label: 'Formulario impreso', checked: false },
    { id: 2, label: 'DNI', checked: false },
    { id: 3, label: 'CUIL', checked: false },
    { id: 4, label: 'Partida de nacimiento', checked: false },
    { id: 5, label: 'Analítico definitivo', checked: false },
    { id: 6, label: 'Constancia de analítico en trámite', checked: false },
    { id: 7, label: 'CUS (Hasta el 30/04/2027)', checked: false },
    { id: 8, label: 'Cuota cooperadora', checked: false }
  ];

  constructor(
    private readonly careerService: CareerService,
    private readonly subjectService: SubjectService,
    private readonly enrollmentService: EnrollmentService,
    private readonly studentService: StudentService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // La carrera del alumno se elige sola al entrar: la de su plan de estudios, o la única que haya.
    forkJoin({
      careers: this.careerService.getCareers(),
      myCareerId: this.studentService.getMyAcademicProgress().pipe(
        map(progress => progress.careerId),
        catchError(() => of(null))
      )
    }).subscribe({
      next: ({ careers, myCareerId }) => {
        this.careers = careers;
        const preselected = careers.find(c => c.id === myCareerId) ?? (careers.length === 1 ? careers[0] : null);
        if (preselected) {
          this.selectedCareer = preselected.id;
          this.onCareerChange();
        }
        this.cdr.detectChanges();
      },
      error: err => console.error(err)
    });
  }

  /** Materias tildadas en todos los años, para el botón flotante. */
  get selectedCount(): number {
    return Object.values(this.selectedSubjectsByYear).reduce((total, list) => total + list.length, 0);
  }

  onCareerChange(): void {
    this.errorMsg = '';
    this.activePeriod = null;
    this.existingEnrollment = null;
    this.subjects = [];
    this.firstYearSubjects = [];
    this.secondYearSubjects = [];
    this.thirdYearSubjects = [];
    this.selectedYears = [];
    this.selectedSubjectsByYear = { 1: [], 2: [], 3: [] };

    if (!this.selectedCareer) return;

    this.checkingPeriod = true;
    this.enrollmentService.getActivePeriod(this.selectedCareer).subscribe({
      next: res => {
        this.activePeriod = res.data;
        if (!this.activePeriod) {
          this.checkingPeriod = false;
          this.cdr.detectChanges();
          return;
        }
        const period = this.activePeriod;
        // Si ya se inscribió en este período, se muestra esa inscripción en lugar del formulario.
        this.enrollmentService.getMyEnrollments().pipe(
          map(r => (r.data ?? []).find(e => e.periodId === period.id) ?? null),
          catchError(() => of(null))
        ).subscribe(existing => {
          this.existingEnrollment = existing;
          this.checkingPeriod = false;
          if (!existing) this.loadStudyPlanCourses(period.studyPlanId);
          this.cdr.detectChanges();
        });
      },
      error: err => {
        this.errorMsg = this.enrollmentErrorMessage(err, 'No se pudo verificar el período de inscripción.');
        this.checkingPeriod = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadStudyPlanCourses(studyPlanId: number): void {
    this.subjectService.getSubjectsByCareer(studyPlanId).subscribe({
      next: courses => {
        this.studentService.getMyEligibleCourses(this.selectedCareer ?? undefined).subscribe({
          next: eligibleCourses => {
            const eligibilityByCourse = new Map(
              eligibleCourses.map(course => [course.studyPlanCourseId, course])
            );
            this.subjects = courses.map(course => ({
              ...course,
              eligibilityStatus: eligibilityByCourse.get(course.id)?.eligibilityStatus
            }));
            this.organizeSubjects();
            this.cdr.detectChanges();
          },
          error: err => {
            this.errorMsg = this.enrollmentErrorMessage(err, 'No se pudo verificar la elegibilidad de las materias.');
            this.cdr.detectChanges();
          }
        });
      },
      error: err => {
        this.errorMsg = this.enrollmentErrorMessage(err, 'No se pudieron cargar las materias del plan de estudios.');
        this.cdr.detectChanges();
      }
    });
  }

  organizeSubjects(): void {
    const periodSemester = this.activePeriod?.semester ?? 0;
    const visible = (s: Subject) => s.isAnnual || s.semester === periodSemester;
    this.firstYearSubjects = this.subjects.filter(x => x.yearNumber === 1 && visible(x));
    this.secondYearSubjects = this.subjects.filter(x => x.yearNumber === 2 && visible(x));
    this.thirdYearSubjects = this.subjects.filter(x => x.yearNumber === 3 && visible(x));
  }

  semesterLabel(subject: Subject): string {
    if (subject.isAnnual) return 'ANUAL';
    return subject.semester === 1 ? '1° Cuatrimestre' : '2° Cuatrimestre';
  }

  addYear(): void {
    if (this.selectedYear == null || this.selectedYears.includes(this.selectedYear)) return;
    this.selectedYears.push(this.selectedYear);
    this.selectedYears.sort((a, b) => a - b);
    this.selectedYear = null;
  }

  removeYear(year: number): void {
    this.selectedYears = this.selectedYears.filter(x => x !== year);
    this.selectedSubjectsByYear[year] = [];
  }

  toggleSubject(year: number, studyPlanCourseId: number, event: any): void {
    const subject = this.subjectsForYear(year).find(course => course.id === studyPlanCourseId);
    if (!subject || !this.isTildable(subject)) return;

    const list = this.selectedSubjectsByYear[year];
    if (event.checked) {
      if (!list.includes(studyPlanCourseId)) list.push(studyPlanCourseId);
    } else {
      this.selectedSubjectsByYear[year] = list.filter(id => id !== studyPlanCourseId);
    }
  }

  subjectsForYear(year: number): Subject[] {
    if (year === 1) return this.firstYearSubjects;
    if (year === 2) return this.secondYearSubjects;
    return this.thirdYearSubjects;
  }

  isAllSelectedForYear(year: number): boolean {
    const subjects = this.subjectsForYear(year).filter(subject => this.isTildable(subject));
    if (subjects.length === 0) return false;
    return subjects.every(s => this.selectedSubjectsByYear[year].includes(s.id));
  }

  toggleSelectAllForYear(year: number): void {
    const subjects = this.subjectsForYear(year).filter(subject => this.isTildable(subject));
    if (this.isAllSelectedForYear(year)) {
      this.selectedSubjectsByYear[year] = [];
    } else {
      this.selectedSubjectsByYear[year] = subjects.map(s => s.id);
    }
  }

  /** Por qué no se puede tildar la materia: ya inscripta, ya aprobada o correlativa pendiente. */
  blockReason(course: Subject): string {
    switch (course.eligibilityStatus) {
      case 'AlreadyEnrolled': return 'Ya está inscripto en esta materia';
      case 'AlreadyApproved': return 'Materia aprobada';
      case 'BlockedByStrictPrerequisite': return 'Correlativa no regularizada o aprobada';
      default: return 'No disponible para inscripción';
    }
  }

  isTildable(course: Subject): boolean {
    return course.eligibilityStatus === 'Eligible' || course.eligibilityStatus === 'EligibleWithWarning';
  }

  canSubmit(): boolean {
    return (
      this.activePeriod !== null &&
      this.selectedShift !== '' &&
      Object.values(this.selectedSubjectsByYear).some(list => list.length > 0)
    );
  }

  availableQuotas(): number {
    if (!this.activePeriod) return 0;
    if (this.selectedShift === 'Mañana')
      return this.activePeriod.quotasMorning - this.activePeriod.enrolledMorning;
    if (this.selectedShift === 'Tarde')
      return this.activePeriod.quotasAfternoon - this.activePeriod.enrolledAfternoon;
    return this.activePeriod.quotasEvening - this.activePeriod.enrolledEvening;
  }

  /** Da de baja la inscripción del período abierto para poder inscribirse de nuevo. */
  cancelExistingEnrollment(): void {
    const existing = this.existingEnrollment;
    if (!existing || this.isCancelling) return;
    Swal.fire({
      title: 'Dar de baja la inscripción',
      text: `Se quitarán las ${existing.courseNames.length} materia(s) de esta inscripción. Después puede volver a inscribirse mientras el período siga abierto.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Sí, dar de baja',
      cancelButtonText: 'Cancelar'
    }).then(result => {
      if (!result.isConfirmed) return;
      this.isCancelling = true;
      this.cdr.detectChanges();
      this.enrollmentService.cancelMyEnrollment(existing.periodId).subscribe({
        next: () => {
          this.isCancelling = false;
          this.onCareerChange(); // vuelve a mostrar el formulario
        },
        error: err => {
          this.isCancelling = false;
          this.errorMsg = this.enrollmentErrorMessage(err, 'No se pudo dar de baja la inscripción.');
          this.cdr.detectChanges();
        }
      });
    });
  }

  submitEnrollment(): void {
    if (!this.canSubmit() || !this.activePeriod) return;

    this.isSubmitting = true;
    this.errorMsg = '';
    this.cdr.detectChanges();

    const studyPlanCourseIds = [
      ...this.selectedSubjectsByYear[1],
      ...this.selectedSubjectsByYear[2],
      ...this.selectedSubjectsByYear[3]
    ];

    this.enrollmentService.enroll({
      enrollmentPeriodId: this.activePeriod.id,
      shift: this.selectedShift,
      studyPlanCourseIds
    }).subscribe({
      next: () => {
        const careerId = this.selectedCareer;
        this.resetForm();
        // Conserva la carrera y recarga las materias: las recién pedidas ya figuran como inscriptas.
        this.selectedCareer = careerId;
        this.onCareerChange();
        this.isSubmitting = false;
        this.cdr.detectChanges();
        this.dialog.open(EnrollmentSuccessDialogComponent, {
          width: '420px',
          disableClose: false
        });
      },
      error: err => {
        this.errorMsg = this.enrollmentErrorMessage(err, 'No fue posible realizar la inscripción.');
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  private enrollmentErrorMessage(error: HttpErrorResponse, fallback: string): string {
    const backendMessage = error?.error?.msg || error?.error?.title;
    if (typeof backendMessage !== 'string' || !backendMessage.trim()) {
      return fallback;
    }

    const normalizedMessage = backendMessage.toLowerCase();
    if (normalizedMessage.includes('already enrolled')) {
      return 'Ya estás inscripto en este período de inscripción.';
    }
    if (normalizedMessage.includes('approved or in progress')) {
      return 'No podés inscribirte nuevamente en una materia aprobada o que ya estás cursando.';
    }
    if (normalizedMessage.includes('prerequisites are not satisfied')) {
      return 'No podés inscribirte: no cumplís con la correlativa obligatoria de una o más materias seleccionadas.';
    }
    if (normalizedMessage.includes('no vacancies')) {
      return 'No hay cupos disponibles para el turno seleccionado.';
    }
    if (normalizedMessage.includes('at least one course')) {
      return 'Seleccioná al menos una materia para realizar la inscripción.';
    }
    if (normalizedMessage.includes('period is closed')) {
      return 'El período de inscripción está cerrado.';
    }
    if (normalizedMessage.includes('no current study plan')) {
      return 'No podés inscribirte todavía: un administrador debe asignarte un plan de estudio vigente para esta carrera.';
    }
    if (normalizedMessage.includes('does not match the student')) {
      return 'El período corresponde a un plan de estudio diferente al que tenés asignado.';
    }

    return backendMessage;
  }

  resetForm(): void {
    this.errorMsg = '';
    this.selectedCareer = null;
    this.activePeriod = null;
    this.selectedShift = '';
    this.selectedYear = null;
    this.selectedYears = [];
    this.selectedSubjectsByYear = { 1: [], 2: [], 3: [] };
    this.subjects = [];
    this.firstYearSubjects = [];
    this.secondYearSubjects = [];
    this.thirdYearSubjects = [];
  }
}
