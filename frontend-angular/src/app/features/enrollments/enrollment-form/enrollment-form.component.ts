import { Component, OnInit } from '@angular/core';
import { CareerService } from '../../../core/services/career.service';
import { SubjectService } from '../../../core/services/subject.service';
import { EnrollmentService, EnrollmentPeriodDto } from '../../../core/services/enrollment. service';

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
  subjects: Subject[] = [];
  firstYearSubjects: Subject[] = [];
  secondYearSubjects: Subject[] = [];
  thirdYearSubjects: Subject[] = [];

  selectedCareer: number | null = null;
  activePeriod: EnrollmentPeriodDto | null = null;
  checkingPeriod = false;

  selectedYear: number | null = null;
  availableYears = [
    { id: 1, name: 'Primero' },
    { id: 2, name: 'Segundo' },
    { id: 3, name: 'Tercero' }
  ];
  selectedYears: number[] = [];

  // stores StudyPlanCourse.id (not courseId)
  selectedSubjectsByYear: Record<number, number[]> = { 1: [], 2: [], 3: [] };

  shifts = ['Mañana', 'Tarde', 'Noche'];
  selectedShift = '';

  isSubmitting = false;
  successMsg = '';
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
    private readonly enrollmentService: EnrollmentService
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe({
      next: data => this.careers = data,
      error: err => console.error(err)
    });
  }

  onCareerChange(): void {
    this.activePeriod = null;
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
        this.checkingPeriod = false;
        if (this.activePeriod) {
          this.loadStudyPlanCourses(this.activePeriod.studyPlanId);
        }
      },
      error: err => {
        console.error(err);
        this.checkingPeriod = false;
      }
    });
  }

  loadStudyPlanCourses(studyPlanId: number): void {
    this.subjectService.getSubjectsByCareer(studyPlanId).subscribe({
      next: courses => {
        this.subjects = courses;
        this.organizeSubjects();
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
    const list = this.selectedSubjectsByYear[year];
    if (event.checked) {
      if (!list.includes(studyPlanCourseId)) list.push(studyPlanCourseId);
    } else {
      this.selectedSubjectsByYear[year] = list.filter(id => id !== studyPlanCourseId);
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
    if (this.selectedShift === 'Mañana')
      return this.activePeriod.quotasMorning - this.activePeriod.enrolledMorning;
    if (this.selectedShift === 'Tarde')
      return this.activePeriod.quotasAfternoon - this.activePeriod.enrolledAfternoon;
    return this.activePeriod.quotasEvening - this.activePeriod.enrolledEvening;
  }

  submitEnrollment(): void {
    if (!this.canSubmit() || !this.activePeriod) return;

    this.isSubmitting = true;
    this.successMsg = '';
    this.errorMsg = '';

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
        this.successMsg = 'La inscripción fue realizada correctamente.';
        this.resetForm();
        this.isSubmitting = false;
      },
      error: err => {
        this.errorMsg = err.error?.msg || 'No fue posible realizar la inscripción.';
        this.isSubmitting = false;
      }
    });
  }

  resetForm(): void {
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
