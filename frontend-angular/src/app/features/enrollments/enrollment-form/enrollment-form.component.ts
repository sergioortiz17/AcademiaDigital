import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CareerService } from '../../../core/services/career.service';
import { StudentService, EligibleCourse } from '../../../core/services/student.service';
import { EnrollmentService, EnrollmentPeriodDto } from '../../../core/services/enrollment. service';
import { EnrollmentSuccessDialogComponent } from './enrollment-success-dialog.component';

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
}

@Component({
  selector: 'app-enrollment-form',
  templateUrl: './enrollment-form.component.html',
  styleUrls: ['./enrollment-form.component.scss'],
  standalone: false
})
export class EnrollmentFormComponent implements OnInit {
  careers: Career[] = [];

  /** Eligible courses grouped by year, already filtered by the backend's real eligibility logic. */
  eligibleByYear: Record<number, EligibleCourse[]> = {};

  /** Years that have at least one tildable (Eligible or EligibleWithWarning) course. */
  availableYears: { id: number; name: string }[] = [];

  selectedCareer: number | null = null;
  activePeriod: EnrollmentPeriodDto | null = null;
  checkingPeriod = false;

  selectedYear: number | null = null;
  selectedYears: number[] = [];
  selectedSubjectsByYear: Record<number, number[]> = {};

  shifts = ['Mañana', 'Tarde', 'Noche'];
  selectedShift = '';

  isSubmitting = false;
  loadingCourses = false;
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

  private static readonly YEAR_LABELS: Record<number, string> = { 1: 'Primero', 2: 'Segundo', 3: 'Tercero' };

  constructor(
    private readonly careerService: CareerService,
    private readonly studentService: StudentService,
    private readonly enrollmentService: EnrollmentService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe({
      next: data => { this.careers = data; this.cdr.detectChanges(); },
      error: err => console.error(err)
    });
  }

  onCareerChange(): void {
    this.activePeriod = null;
    this.eligibleByYear = {};
    this.availableYears = [];
    this.selectedYears = [];
    this.selectedSubjectsByYear = {};
    this.errorMsg = '';
    if (!this.selectedCareer) return;

    this.checkingPeriod = true;
    this.enrollmentService.getActivePeriod(this.selectedCareer).subscribe({
      next: res => {
        this.activePeriod = res.data;
        this.checkingPeriod = false;
        if (this.activePeriod) this.loadEligibleCourses();
        this.cdr.detectChanges();
      },
      error: () => { this.checkingPeriod = false; this.cdr.detectChanges(); }
    });
  }

  loadEligibleCourses(): void {
    if (!this.selectedCareer) return;
    this.loadingCourses = true;
    this.studentService.getMyEligibleCourses(this.selectedCareer).subscribe({
      next: courses => {
        this.organizeByYear(courses);
        this.loadingCourses = false;
        this.cdr.detectChanges();
      },
      error: err => {
        this.errorMsg = err?.error?.detail || err?.message || 'No se pudieron cargar las materias elegibles.';
        this.loadingCourses = false;
        this.cdr.detectChanges();
      }
    });
  }

  private organizeByYear(courses: EligibleCourse[]): void {
    const periodSemester = this.activePeriod?.semester ?? 0;
    // Drop AlreadyApproved/AlreadyEnrolled entirely — those aren't actionable.
    const actionable = courses.filter(c =>
      c.eligibilityStatus !== 'AlreadyApproved' && c.eligibilityStatus !== 'AlreadyEnrolled');
    // Group by year.
    const grouped: Record<number, EligibleCourse[]> = {};
    for (const c of actionable) {
      (grouped[c.yearNumber] ??= []).push(c);
    }
    this.eligibleByYear = grouped;
    // Available years = only those with ≥1 tildable (Eligible/EligibleWithWarning).
    this.availableYears = Object.keys(grouped)
      .map(Number)
      .filter(y => grouped[y].some(c => c.eligibilityStatus === 'Eligible' || c.eligibilityStatus === 'EligibleWithWarning'))
      .sort()
      .map(y => ({ id: y, name: EnrollmentFormComponent.YEAR_LABELS[y] ?? `${y}°` }));
    // Reset selections for newly computed years.
    this.selectedSubjectsByYear = {};
    for (const y of this.availableYears) this.selectedSubjectsByYear[y.id] = [];
  }

  coursesForYear(year: number): EligibleCourse[] {
    return this.eligibleByYear[year] ?? [];
  }

  isTildable(c: EligibleCourse): boolean {
    return c.eligibilityStatus === 'Eligible' || c.eligibilityStatus === 'EligibleWithWarning';
  }

  blockReason(c: EligibleCourse): string {
    if (c.eligibilityStatus !== 'BlockedByStrictPrerequisite') return '';
    const strict = c.missingPrerequisites.filter(p => p.prerequisiteType === 'Strict');
    if (strict.length === 0) return 'Correlativa pendiente';
    return 'Falta: ' + strict.map(p => p.name).join(', ');
  }

  semesterLabel(c: EligibleCourse): string {
    return c.semester === 0 ? 'ANUAL' : c.semester === 1 ? '1° Cuatrimestre' : '2° Cuatrimestre';
  }

  addYear(): void {
    if (this.selectedYear == null || this.selectedYears.includes(this.selectedYear)) return;
    this.selectedYears.push(this.selectedYear);
    this.selectedYears.sort((a, b) => a - b);
    this.selectedYear = null;
  }

  removeYear(year: number): void {
    this.selectedYears = this.selectedYears.filter(x => x !== year);
    delete this.selectedSubjectsByYear[year];
    this.selectedSubjectsByYear[year] = [];
  }

  toggleSubject(year: number, studyPlanCourseId: number, event: any): void {
    const list = this.selectedSubjectsByYear[year] ?? [];
    if (event.checked) {
      if (!list.includes(studyPlanCourseId)) list.push(studyPlanCourseId);
    } else {
      this.selectedSubjectsByYear[year] = list.filter(id => id !== studyPlanCourseId);
      return;
    }
    this.selectedSubjectsByYear[year] = list;
  }

  isAllSelectedForYear(year: number): boolean {
    const tildable = this.coursesForYear(year).filter(c => this.isTildable(c));
    if (tildable.length === 0) return false;
    return tildable.every(c => (this.selectedSubjectsByYear[year] ?? []).includes(c.studyPlanCourseId));
  }

  toggleSelectAllForYear(year: number): void {
    const tildable = this.coursesForYear(year).filter(c => this.isTildable(c));
    if (this.isAllSelectedForYear(year)) {
      this.selectedSubjectsByYear[year] = [];
    } else {
      this.selectedSubjectsByYear[year] = tildable.map(c => c.studyPlanCourseId);
    }
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
    if (this.selectedShift === 'Mañana') return this.activePeriod.quotasMorning - this.activePeriod.enrolledMorning;
    if (this.selectedShift === 'Tarde') return this.activePeriod.quotasAfternoon - this.activePeriod.enrolledAfternoon;
    return this.activePeriod.quotasEvening - this.activePeriod.enrolledEvening;
  }

  submitEnrollment(): void {
    if (!this.canSubmit() || !this.activePeriod) return;
    this.isSubmitting = true;
    this.errorMsg = '';
    this.cdr.detectChanges();

    const studyPlanCourseIds = Object.values(this.selectedSubjectsByYear).flat();
    this.enrollmentService.enroll({
      enrollmentPeriodId: this.activePeriod.id,
      shift: this.selectedShift,
      studyPlanCourseIds
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.cdr.detectChanges();
        this.resetForm();
        this.dialog.open(EnrollmentSuccessDialogComponent, { width: '420px', disableClose: false });
      },
      error: err => {
        this.errorMsg = err.message || err.error?.msg || 'No fue posible realizar la inscripción.';
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.selectedCareer = null;
    this.activePeriod = null;
    this.selectedShift = '';
    this.selectedYear = null;
    this.selectedYears = [];
    this.selectedSubjectsByYear = {};
    this.eligibleByYear = {};
    this.availableYears = [];
  }
}
