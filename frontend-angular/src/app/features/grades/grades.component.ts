import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import * as ExcelJS from 'exceljs';
import { asyncScheduler, forkJoin, Observable, observeOn } from 'rxjs';
import { selectUserRole } from '../../store/account/account.selectors';
import { UserRole } from '../../store/account/account.actions';
import { CareerService, Career, CareerDivision, StudyPlanCourse } from '../../core/services/career.service';
import { TeacherService, TeacherAssignment } from '../../core/services/teacher.service';
import { TeachingPosition, TeachingPositionService } from '../../core/services/teaching-position.service';
import { Student, StudentAcademicProgress, StudentExamTable, StudentService } from '../../core/services/student.service';
import { ProfileData, UserService } from '../../core/services/user.service';
import { GradebookExam, Gradebook, GradebookService, GradebookDetail, GradebookEvaluation, GradebookStudent, SaveGradeEntryInput, StudentPublishedGradebook } from '../../core/services/gradebook.service';
import { EvaluationSetupDialogComponent } from './evaluation-setup-dialog/evaluation-setup-dialog.component';
import { GradebookActionDialogComponent } from './gradebook-action-dialog.component';

interface EditableGrade {
  evaluationId: number;
  score: number | null;
  notes: string;
  updatedAt: string | null;
}

interface EditableRow extends GradebookStudent {
  editableGrades: Record<number, EditableGrade>;
}

interface EvaluationGroup {
  label: string;
  evaluation: GradebookEvaluation;
}

interface ProfessorCourseOption {
  key: string;
  courseId: number;
  courseCode: string;
  courseName: string;
  yearNumber: number;
  semester: number;
  isAnnual: boolean;
  section: GradeTeachingSection | null;
}

interface GradeTeachingSection {
  courseSectionId: number;
  courseId: number;
  courseCode: string;
  courseName: string;
  divisionId: number | null;
  divisionCode: string | null;
  divisionName: string | null;
  academicYear: number;
  semester: number;
}

interface GradeDivisionOption {
  divisionId: number;
  divisionCode: string;
  divisionName: string;
  academicYear: number;
  yearNumber: number;
}

interface StudentGradeRow {
  courseId: number;
  courseName: string;
  yearNumber: number;
  academicYear: number | null;
  semester: number;
  gradebook: StudentPublishedGradebook | null;
  latestExam: StudentExamDisplay | null;
  finalGrade: number | null;
  enrollmentStatus: string | null;
}

interface StudentExamDisplay {
  examDateUtc: string;
  attemptNumber: number;
  status: string;
  grade: number | null;
  outcome: string | null;
}

const RESULT_LABELS: Record<string, string> = {
  Promoted: 'Promocionado',
  Regularized: 'Regular',
  Failed: 'Libre'
};

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Borrador',
  Submitted: 'Enviada',
  Approved: 'Aprobada',
  Published: 'Publicada',
  Closed: 'Cerrada'
};

const CAREER_YEAR_LABELS: Record<number, string> = {
  1: 'Primero',
  2: 'Segundo',
  3: 'Tercero',
  4: 'Cuarto',
  5: 'Quinto',
  6: 'Sexto'
};

@Component({
  selector: 'app-grades',
  templateUrl: './grades.component.html',
  styleUrls: ['./grades.component.scss'],
  standalone: false
})
export class GradesComponent implements OnInit {
  UserRole = UserRole;
  userRole: UserRole | null = null;

  careers: Career[] = [];
  selectedCareerId: number | null = null;
  studyPlanCourses: StudyPlanCourse[] = [];
  // --- Profesor ---
  assignments: GradeTeachingSection[] = [];
  careerDivisions: CareerDivision[] = [];
  selectedTeachingPositionId: number | null = null;
  selectedCareerYear: number | null = null;
  selectedDivisionId: number | null = null;
  selectedCourseKey: string | null = null;
  noTeachingSection = false;

  detail: GradebookDetail | null = null;
  rows: EditableRow[] = [];
  selectedStudent: EditableRow | null = null;
  searchTerm = '';

  isCreating = false;
  isSaving = false;
  isSubmitting = false;
  isTransitioning = false;
  editingEnabled = false;
  noGradebookYet = false;

  // --- Alumno ---
  myGradebooks: StudentPublishedGradebook[] = [];
  academicYears: number[] = [];
  selectedAcademicYear: number | null = null;
  studentProfile: ProfileData | null = null;
  studentRecord: Student | null = null;
  studentProgress: StudentAcademicProgress | null = null;
  studentExamTables: StudentExamTable[] = [];
  isStudentProgressLoading = false;
  private studentProgressRequestId = 0;

  isLoading = false;
  errorMsg = '';
  successMsg = '';

  constructor(
    private readonly store: Store,
    private readonly teacherService: TeacherService,
    private readonly teachingPositionService: TeachingPositionService,
    private readonly gradebookService: GradebookService,
    private readonly careerService: CareerService,
    private readonly studentService: StudentService,
    private readonly userService: UserService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe(careers => {
      this.careers = careers;
      this.cdr.detectChanges();
    }, error => {
      this.errorMsg = this.readErrorMessage(error, 'No se pudieron cargar las carreras.');
      this.cdr.detectChanges();
    });

    this.store.select(selectUserRole).subscribe(role => {
      this.userRole = role as UserRole;
      if (this.userRole === UserRole.Profesor) {
        this.loadMyAssignments();
      } else if (this.userRole === UserRole.Admin) {
        this.loadAllTeachingPositions();
      } else if (this.userRole === UserRole.Alumno) {
        this.loadMyGrades();
        this.loadMyExamTables();
        this.loadStudentProfile();
        this.loadStudentAcademicProgress();
      }
    });
  }

  loadMyAssignments(): void {
    this.teacherService.getMyAssignments(false).subscribe({
      next: (assignments) => {
        this.assignments = assignments.map(assignment => ({
          courseSectionId: assignment.courseSectionId,
          courseId: assignment.courseId,
          courseCode: assignment.courseCode,
          courseName: assignment.courseName,
          divisionId: assignment.divisionId,
          divisionCode: assignment.divisionCode,
          divisionName: assignment.divisionName,
          academicYear: assignment.academicYear,
          semester: assignment.semester
        }));
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = this.readErrorMessage(err, 'No se pudieron cargar tus materias asignadas.');
        this.cdr.detectChanges();
      }
    });
  }

  loadAllTeachingPositions(): void {
    this.teachingPositionService.getTeachingPositions().subscribe({
      next: positions => {
        this.assignments = positions.map(position => this.toGradeTeachingSection(position));
        this.cdr.detectChanges();
      },
      error: err => {
        this.errorMsg = this.readErrorMessage(err, 'No se pudieron cargar las secciones de cursada.');
        this.cdr.detectChanges();
      }
    });
  }

  private toGradeTeachingSection(position: TeachingPosition): GradeTeachingSection {
    return {
      courseSectionId: position.id,
      courseId: position.courseId,
      courseCode: position.courseCode,
      courseName: position.courseName,
      divisionId: position.divisionId,
      divisionCode: position.divisionCode,
      divisionName: position.divisionName,
      academicYear: position.academicYear,
      semester: position.semester
    };
  }

  get selectedAssignment(): GradeTeachingSection | undefined {
    return this.assignments.find(a => a.courseSectionId === this.selectedTeachingPositionId);
  }

  get canEdit(): boolean {
    return this.detail?.gradebook.status === 'Draft' && this.editingEnabled;
  }

  get canEditGradebook(): boolean {
    return this.detail?.gradebook.status === 'Draft';
  }

  get filteredRows(): EditableRow[] {
    const terms = this.searchTerm.trim().toLowerCase().split(/\s+/).filter(Boolean);
    if (!terms.length) return this.rows;
    return this.rows.filter(r => terms.every(term => r.studentName.toLowerCase().includes(term)));
  }

  get evaluationGroups(): EvaluationGroup[] {
    if (!this.detail) return [];

    const evaluations = [...this.detail.evaluations].sort((a, b) => {
      return a.displayOrder - b.displayOrder;
    });
    return evaluations.map(evaluation => ({ label: evaluation.name, evaluation }));
  }

  trackEvaluationGroup(index: number, group: EvaluationGroup): number {
    return group.evaluation.id || -(index + 1);
  }

  examOutcomeLabel(outcome: string | null): string {
    switch (outcome) {
      case 'Passed': return 'Aprobado';
      case 'Failed': return 'Desaprobado';
      case 'Absent': return 'Ausente';
      default: return '—';
    }
  }

  get professorYears(): number[] {
    return [...new Set(this.studyPlanCourses.map(course => course.yearNumber))].sort((a, b) => a - b);
  }

  careerYearLabel(year: number): string {
    return CAREER_YEAR_LABELS[year] ?? `${year}°`;
  }

  get professorCourses(): ProfessorCourseOption[] {
    if (this.selectedCareerYear == null || this.selectedDivisionId == null) return [];

    const options: ProfessorCourseOption[] = [];
    const courses = this.studyPlanCourses
      .filter(course => course.yearNumber === this.selectedCareerYear)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.courseName.localeCompare(b.courseName));

    for (const course of courses) {
      const sections = this.assignments.filter(assignment =>
        assignment.courseId === course.courseId && assignment.divisionId === this.selectedDivisionId
      );

      if (this.userRole === UserRole.Admin && sections.length === 0) {
        options.push({
          key: `course-${course.courseId}`,
          courseId: course.courseId,
          courseCode: course.courseCode,
          courseName: course.courseName,
          yearNumber: course.yearNumber,
          semester: course.semester,
          isAnnual: course.isAnnual,
          section: null
        });
        continue;
      }

      for (const section of sections) {
        options.push({
          key: `section-${section.courseSectionId}`,
          courseId: course.courseId,
          courseCode: course.courseCode,
          courseName: course.courseName,
          yearNumber: course.yearNumber,
          semester: course.semester,
          isAnnual: course.isAnnual,
          section
        });
      }
    }

    return options;
  }

  get professorDivisions(): GradeDivisionOption[] {
    if (this.selectedCareerYear == null) return [];

    if (this.userRole === UserRole.Admin) {
      return this.careerDivisions
        .filter(division => division.yearNumber === this.selectedCareerYear)
        .map(division => ({
          divisionId: division.id,
          divisionCode: division.code,
          divisionName: division.name,
          academicYear: division.academicYear,
          yearNumber: division.yearNumber
        }));
    }

    const courseIds = new Set(this.studyPlanCourses
      .filter(course => course.yearNumber === this.selectedCareerYear)
      .map(course => course.courseId));
    const seen = new Set<number>();
    return this.assignments
      .filter(assignment => courseIds.has(assignment.courseId))
      .filter(assignment => {
        if (assignment.divisionId == null || seen.has(assignment.divisionId)) return false;
        seen.add(assignment.divisionId);
        return true;
      })
      .map(assignment => ({
        divisionId: assignment.divisionId!,
        divisionCode: assignment.divisionCode ?? '',
        divisionName: assignment.divisionName ?? '',
        academicYear: assignment.academicYear,
        yearNumber: this.selectedCareerYear!
      }));
  }

  get selectedProfessorCourse(): ProfessorCourseOption | undefined {
    return this.professorCourses.find(course => course.key === this.selectedCourseKey);
  }

  trackProfessorDivision(index: number, division: GradeDivisionOption): number {
    return division.divisionId ?? -(index + 1);
  }

  trackProfessorCourse(index: number, course: ProfessorCourseOption): string {
    return course.key;
  }

  resultLabel(status: string | null): string {
    if (!status) return '—';
    return RESULT_LABELS[status] ?? status;
  }

  statusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  private readErrorMessage(error: unknown, fallback: string): string {
    if (typeof error === 'string' && error.trim()) return error;
    if (!error || typeof error !== 'object') return fallback;

    const response = error as Record<string, unknown>;
    const body = response['error'];
    const details = body && typeof body === 'object' ? body as Record<string, unknown> : {};
    const message = details['detail'] ?? details['msg'] ?? details['message'] ??
      response['detail'] ?? response['title'] ?? response['message'];
    if (typeof message === 'string' && message.trim()) return message;
    if (typeof body === 'string' && body.trim()) return body;

    try {
      const serialized = JSON.stringify(body ?? error);
      return serialized && serialized !== '{}' ? serialized : fallback;
    } catch {
      return fallback;
    }
  }

  onPositionChange(): void {
    this.loadGradebookForPosition();
  }

  onCareerChange(): void {
    const careerId = this.selectedCareerId;
    this.studyPlanCourses = [];
    this.careerDivisions = [];
    this.selectedCareerYear = null;
    this.selectedDivisionId = null;
    this.clearGradebookSelection();
    this.errorMsg = '';

    if (careerId == null) {
      this.isLoading = false;
      this.cdr.detectChanges();
      return;
    }

    if (this.userRole === UserRole.Admin) {
      this.careerService.getDivisions(careerId).subscribe({
        next: divisions => {
          if (this.selectedCareerId !== careerId) return;
          this.careerDivisions = divisions;
          this.cdr.detectChanges();
        },
        error: err => {
          if (this.selectedCareerId !== careerId) return;
          this.errorMsg = this.readErrorMessage(err, 'No se pudieron cargar las divisiones de la carrera.');
          this.cdr.detectChanges();
        }
      });
    }

    this.isLoading = true;
    this.careerService.getStudyPlans(careerId).subscribe({
      next: plans => {
        if (this.selectedCareerId !== careerId) return;
        const activePlan = plans
          .filter(plan => plan.isActive && plan.status === 'Active')
          .sort((a, b) => b.versionNumber - a.versionNumber)[0];
        if (!activePlan) {
          this.isLoading = false;
          this.errorMsg = 'La carrera no tiene un plan de estudio activo.';
          this.cdr.detectChanges();
          return;
        }

        this.careerService.getStudyPlanCourses(activePlan.id).subscribe({
          next: courses => {
            if (this.selectedCareerId !== careerId) return;
            this.studyPlanCourses = courses;
            this.isLoading = false;
            this.cdr.detectChanges();
          },
          error: err => {
            if (this.selectedCareerId !== careerId) return;
            this.isLoading = false;
            this.errorMsg = this.readErrorMessage(err, 'No se pudieron cargar las materias del plan de estudio.');
            this.cdr.detectChanges();
          }
        });
      },
      error: err => {
        if (this.selectedCareerId !== careerId) return;
        this.isLoading = false;
        this.errorMsg = this.readErrorMessage(err, 'No se pudieron cargar los planes de estudio de la carrera.');
        this.cdr.detectChanges();
      }
    });
  }

  onAssignmentFilterChange(): void {
    const yearIsAvailable = this.selectedCareerYear == null ||
      this.professorYears.includes(this.selectedCareerYear);
    if (!yearIsAvailable) this.selectedCareerYear = null;

    const divisionIsAvailable = this.selectedDivisionId == null ||
      this.professorDivisions.some(a => a.divisionId === this.selectedDivisionId);
    if (!divisionIsAvailable) this.selectedDivisionId = null;

    const courseIsAvailable = this.selectedCourseKey == null ||
      this.professorCourses.some(course => course.key === this.selectedCourseKey);
    if (!courseIsAvailable) this.selectedCourseKey = null;

    if (this.selectedCourseKey == null) {
      this.clearGradebookSelection();
      return;
    }

    this.onCourseChange();
  }

  onDivisionChange(): void {
    this.clearGradebookSelection();
  }

  onCourseChange(): void {
    try {
      const course = this.selectedProfessorCourse;
      if (!course) {
        this.clearGradebookSelection();
        return;
      }

      this.selectedTeachingPositionId = course.section?.courseSectionId ?? null;
      this.detail = null;
      this.rows = [];
      this.selectedStudent = null;
      this.searchTerm = '';
      this.noGradebookYet = false;
      this.noTeachingSection = course.section == null;
      this.editingEnabled = false;
      this.errorMsg = '';
      if (course.section) this.loadGradebookForPosition();
      else this.isLoading = false;
    } catch (error) {
      this.isLoading = false;
      this.errorMsg = this.readErrorMessage(error, 'No se pudo seleccionar la materia.');
      console.error('Error selecting gradebook course', error);
      this.cdr.detectChanges();
    }
  }

  private clearGradebookSelection(): void {
    this.selectedTeachingPositionId = null;
    this.selectedCourseKey = null;
    this.detail = null;
    this.rows = [];
    this.selectedStudent = null;
    this.searchTerm = '';
    this.noGradebookYet = false;
    this.noTeachingSection = false;
    this.editingEnabled = false;
    this.isLoading = false;
    this.errorMsg = '';
  }

  loadGradebookForPosition(): void {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    this.isLoading = true;
    this.errorMsg = '';
    this.detail = null;
    this.rows = [];
    this.editingEnabled = false;
    this.noGradebookYet = false;

    this.gradebookService.getGradebooks({
      courseId: assignment.courseId,
      divisionId: assignment.divisionId ?? undefined,
      academicYear: assignment.academicYear
    }).subscribe({
      next: (gradebooks) => {
        try {
          if (!Array.isArray(gradebooks)) throw new Error('La respuesta de planillas no tiene el formato esperado.');
          const gradebook = gradebooks.find(item => item.courseSectionId === assignment.courseSectionId);
          if (!gradebook) {
            this.noGradebookYet = true;
            this.isLoading = false;
            this.cdr.detectChanges();
            return;
          }
          this.loadDetail(gradebook.id);
        } catch (error) {
          this.errorMsg = this.readErrorMessage(error, 'No se pudo procesar la respuesta de planillas.');
          this.isLoading = false;
          console.error('Error processing gradebook list response', error);
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.errorMsg = this.readErrorMessage(err, 'Error al buscar la planilla.');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadDetail(gradebookId: number): void {
    this.isLoading = true;
    this.gradebookService.getGradebook(gradebookId).subscribe({
      next: (detail) => {
        try {
          if (!detail || !Array.isArray(detail.students) || !Array.isArray(detail.evaluations)) {
            throw new Error('El detalle de la planilla no tiene el formato esperado.');
          }
          this.detail = detail;
          this.rows = detail.students.map(student => this.toEditableRow(student));
          this.selectedStudent = this.rows.find(r => r.enrollmentId === this.selectedStudent?.enrollmentId) ?? this.rows[0] ?? null;
          this.isLoading = false;
          this.cdr.detectChanges();
        } catch (error) {
          this.detail = null;
          this.rows = [];
          this.errorMsg = this.readErrorMessage(error, 'No se pudo procesar el detalle de la planilla.');
          this.isLoading = false;
          console.error('Error processing gradebook detail response', error);
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.errorMsg = this.readErrorMessage(err, 'No se pudo cargar la planilla.');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private toEditableRow(student: GradebookStudent): EditableRow {
    const editableGrades: Record<number, EditableGrade> = {};
    for (const grade of student.grades) {
      editableGrades[grade.evaluationId] = {
        evaluationId: grade.evaluationId,
        score: grade.score,
        notes: grade.notes ?? '',
        updatedAt: grade.updatedAt
      };
    }
    return { ...student, editableGrades };
  }

  openCreateDialog(): void {
    const assignment = this.selectedAssignment;
    if (!assignment) return;

    const dialogRef = this.dialog.open(EvaluationSetupDialogComponent, {
      width: '600px',
      disableClose: true
    });

    dialogRef.afterClosed().pipe(observeOn(asyncScheduler)).subscribe((evaluations) => {
      if (!evaluations) return;
      this.isCreating = true;
      this.gradebookService.createGradebook(assignment.courseSectionId, evaluations).subscribe({
        next: (gradebook) => {
          this.isCreating = false;
          this.noGradebookYet = false;
          this.successMsg = 'Planilla creada correctamente.';
          this.loadDetail(gradebook.id);
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.isCreating = false;
          if (err.status === 409) {
            this.noGradebookYet = false;
            this.isLoading = true;
            this.gradebookService.getGradebooks({
              courseId: assignment.courseId,
              divisionId: assignment.divisionId ?? undefined,
              academicYear: assignment.academicYear
            }).subscribe({
              next: gradebooks => {
                const existingGradebook = gradebooks.find(item =>
                  item.courseSectionId === assignment.courseSectionId
                );
                if (existingGradebook) {
                  this.noGradebookYet = false;
                  this.loadDetail(existingGradebook.id);
                  return;
                }
                this.isLoading = false;
                this.errorMsg = this.readErrorMessage(err, 'No se pudo crear la planilla.');
                this.cdr.detectChanges();
              },
              error: () => {
                this.isLoading = false;
                this.errorMsg = this.readErrorMessage(err, 'No se pudo crear la planilla.');
                this.cdr.detectChanges();
              }
            });
            return;
          }
          this.errorMsg = this.readErrorMessage(err, 'Error al crear la planilla.');
          this.cdr.detectChanges();
        }
      });
    });
  }

  selectStudent(row: EditableRow): void {
    this.selectedStudent = row;
  }

  enableEditing(): void {
    if (this.canEditGradebook) this.editingEnabled = true;
  }

  updateGradeScore(row: EditableRow, evaluationId: number, score: number | null): void {
    const current = row.editableGrades[evaluationId] ?? {
      evaluationId,
      score: null,
      notes: '',
      updatedAt: null
    };
    row.editableGrades[evaluationId] = { ...current, score };
  }

  saveGrades(): void {
    if (!this.detail || !this.canEdit || this.isSaving || this.isSubmitting) return;

    const missingEvaluations = this.evaluationGroups.filter(group =>
      group.evaluation.id < 0 && this.rows.some(row => row.editableGrades[group.evaluation.id]?.score != null)
    );

    if (missingEvaluations.length > 0) {
      this.isSaving = true;
      this.errorMsg = '';
      forkJoin(missingEvaluations.map(group => this.gradebookService.addEvaluation(this.detail!.gradebook.id, {
        name: group.label,
        weightPercentage: group.evaluation.weightPercentage,
        maximumScore: group.evaluation.maximumScore,
        isRecovery: group.evaluation.isRecovery
      }))).subscribe({
        next: createdEvaluations => {
          for (const [index, created] of createdEvaluations.entries()) {
            const temporaryId = missingEvaluations[index].evaluation.id;
            for (const row of this.rows) {
              const pendingGrade = row.editableGrades[temporaryId];
              if (pendingGrade) {
                row.editableGrades[created.id] = { ...pendingGrade, evaluationId: created.id };
                delete row.editableGrades[temporaryId];
              }
            }
          }
          this.detail = {
            ...this.detail!,
            evaluations: [...this.detail!.evaluations, ...createdEvaluations]
          };
          this.persistGrades();
        },
        error: err => {
          this.isSaving = false;
          this.errorMsg = err.error?.msg || err.error?.message || err.message || 'Error al crear las nuevas instancias.';
          this.cdr.detectChanges();
        }
      });
      return;
    }

    this.persistGrades();
  }

  private persistGrades(): void {
    if (!this.detail) return;

    const grades: SaveGradeEntryInput[] = [];
    for (const row of this.rows) {
      for (const evaluation of this.detail.evaluations) {
        const grade = row.editableGrades[evaluation.id];
        if (grade?.score != null) {
          grades.push({
            evaluationId: evaluation.id,
            enrollmentId: row.enrollmentId,
            score: grade.score,
            notes: grade.notes.trim() || null
          });
        }
      }
    }

    if (grades.length === 0) {
      this.isSaving = false;
      this.errorMsg = 'No hay notas cargadas para guardar.';
      return;
    }

    this.isSaving = true;
    this.errorMsg = '';
    this.gradebookService.saveGrades(this.detail.gradebook.id, grades).subscribe({
      next: (detail) => {
        this.detail = detail;
        this.rows = detail.students.map(student => this.toEditableRow(student));
        this.selectedStudent = this.rows.find(r => r.enrollmentId === this.selectedStudent?.enrollmentId) ?? this.rows[0] ?? null;
        this.isSaving = false;
        this.editingEnabled = false;
        this.successMsg = 'Notas guardadas correctamente.';
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMsg = err.message || 'Error al guardar las notas.';
        this.cdr.detectChanges();
      }
    });
  }

  submitGradebook(): void {
    if (!this.detail || this.isSubmitting) return;
    if (this.rows.length === 0) {
      this.errorMsg = 'Una planilla sin estudiantes inscriptos no puede enviarse.';
      return;
    }
    if (!this.detail.evaluations.some(evaluation => !evaluation.isRecovery)) {
      this.errorMsg = 'La planilla no tiene instancias regulares configuradas.';
      return;
    }

    this.isSubmitting = true;
    const isAdmin = this.userRole === UserRole.Admin;
    const dialogRef = this.dialog.open(GradebookActionDialogComponent, {
      width: '480px',
      disableClose: true,
      data: {
        confirmation: isAdmin
          ? '¿Enviar la planilla? Ya no vas a poder editarla salvo que la reabran.'
          : '¿Enviar la planilla a Secretaría? Ya no vas a poder editarla salvo que la reabran.',
        confirmLabel: 'Enviar planilla'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed !== true) {
        this.isSubmitting = false;
        this.cdr.detectChanges();
        return;
      }

      if (!this.detail) {
        this.isSubmitting = false;
        return;
      }
      const gradebookId = this.detail.gradebook.id;
      this.errorMsg = '';
      const missingGrades = this.fillMissingGrades();
      if (missingGrades.length === 0) {
        this.sendGradebook(gradebookId, isAdmin);
        return;
      }

      this.gradebookService.saveGrades(gradebookId, missingGrades).subscribe({
        next: detail => {
          this.detail = detail;
          this.rows = detail.students.map(student => this.toEditableRow(student));
          this.selectedStudent = this.rows.find(row => row.enrollmentId === this.selectedStudent?.enrollmentId) ?? this.rows[0] ?? null;
          this.sendGradebook(gradebookId, isAdmin);
        },
        error: err => {
          this.isSubmitting = false;
          this.errorMsg = this.readErrorMessage(err, 'No se pudieron completar las notas vacías con cero.');
          this.cdr.detectChanges();
        }
      });
    });
  }

  private fillMissingGrades(): SaveGradeEntryInput[] {
    if (!this.detail) return [];

    const missingGrades: SaveGradeEntryInput[] = [];
    for (const row of this.rows) {
      for (const evaluation of this.detail.evaluations) {
        const existingGrade = row.editableGrades[evaluation.id];
        if (existingGrade?.score != null) continue;

        row.editableGrades[evaluation.id] = {
          evaluationId: evaluation.id,
          score: 0,
          notes: existingGrade?.notes ?? '',
          updatedAt: existingGrade?.updatedAt ?? null
        };
        missingGrades.push({
          evaluationId: evaluation.id,
          enrollmentId: row.enrollmentId,
          score: 0,
          notes: existingGrade?.notes.trim() || null
        });
      }
    }
    return missingGrades;
  }

  private sendGradebook(gradebookId: number, isAdmin: boolean): void {
    this.gradebookService.submitGradebook(gradebookId).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successMsg = isAdmin ? 'Planilla enviada.' : 'Planilla enviada a Secretaría.';
        this.loadDetail(gradebookId);
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: err => {
        this.isSubmitting = false;
        this.errorMsg = this.readErrorMessage(err, 'Error al enviar la planilla.');
        this.cdr.detectChanges();
      }
    });
  }

  approveGradebook(): void {
    this.runAdminGradebookTransition(
      gradebookId => this.gradebookService.approveGradebook(gradebookId),
      '¿Aprobar esta planilla enviada?',
      'Planilla aprobada.'
    );
  }

  publishGradebook(): void {
    this.runAdminGradebookTransition(
      gradebookId => this.gradebookService.publishGradebook(gradebookId),
      '¿Publicar las notas para los alumnos?',
      'Planilla publicada.'
    );
  }

  closeGradebook(): void {
    this.runAdminGradebookTransition(
      gradebookId => this.gradebookService.closeGradebook(gradebookId),
      '¿Cerrar esta instancia y aplicar los resultados finales?',
      'Instancia cerrada y resultados aplicados.'
    );
  }

  reopenGradebook(): void {
    const gradebookId = this.detail?.gradebook.id;
    if (this.userRole !== UserRole.Admin || gradebookId == null || this.isTransitioning) return;

    this.isTransitioning = true;
    const dialogRef = this.dialog.open(GradebookActionDialogComponent, {
      width: '480px',
      disableClose: true,
      data: {
        confirmation: '¿Reabrir la planilla? Volverá al estado Borrador.',
        confirmLabel: 'Reabrir',
        reasonPrompt: 'Ingresá el motivo para volver a editar la planilla:'
      }
    });

    dialogRef.afterClosed().subscribe(reason => {
      if (typeof reason !== 'string') {
        this.isTransitioning = false;
        this.cdr.detectChanges();
        return;
      }
      this.executeAdminGradebookTransition(
        gradebookId,
        id => this.gradebookService.reopenGradebook(id, reason),
        'Planilla reabierta. Ya podés editarla.',
        true
      );
    });
  }

  private runAdminGradebookTransition(
    transition: (gradebookId: number) => Observable<Gradebook>,
    confirmation: string,
    successMessage: string,
    enableEditing = false
  ): void {
    const gradebookId = this.detail?.gradebook.id;
    if (this.userRole !== UserRole.Admin || gradebookId == null || this.isTransitioning) return;

    this.isTransitioning = true;
    const dialogRef = this.dialog.open(GradebookActionDialogComponent, {
      width: '480px',
      disableClose: true,
      data: { confirmation, confirmLabel: 'Confirmar' }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed !== true) {
        this.isTransitioning = false;
        this.cdr.detectChanges();
        return;
      }

      this.executeAdminGradebookTransition(gradebookId, transition, successMessage, enableEditing);
    });
  }

  private executeAdminGradebookTransition(
    gradebookId: number,
    transition: (gradebookId: number) => Observable<Gradebook>,
    successMessage: string,
    enableEditing: boolean
  ): void {
    this.errorMsg = '';
    transition(gradebookId).subscribe({
      next: () => {
        this.isTransitioning = false;
        this.editingEnabled = enableEditing;
        this.successMsg = successMessage;
        this.loadDetail(gradebookId);
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: err => {
        this.isTransitioning = false;
        this.errorMsg = this.readErrorMessage(err, 'No se pudo cambiar el estado de la planilla.');
        this.cdr.detectChanges();
      }
    });
  }

  async downloadSummary(): Promise<void> {
    if (!this.detail) return;
    const workbook = new ExcelJS.Workbook();
    workbook.creator = 'Academia Digital';
    const worksheet = workbook.addWorksheet('Calificaciones');
    const evaluations = this.evaluationGroups;
    let currentColumn = 3;

    const setFill = (rowNumber: number, columnNumber: number, color: string): void => {
      worksheet.getCell(rowNumber, columnNumber).fill = {
        type: 'pattern',
        pattern: 'solid',
        fgColor: { argb: color }
      };
    };

    const addGroup = (label: string, subheaders: string[], color: string, subheaderColor = color): number => {
      const startColumn = currentColumn;
      const endColumn = startColumn + subheaders.length - 1;
      worksheet.mergeCells(2, startColumn, 2, endColumn);
      worksheet.getCell(2, startColumn).value = label;
      for (let column = startColumn; column <= endColumn; column++) {
        setFill(2, column, color);
      }
      subheaders.forEach((header, index) => {
        const cell = worksheet.getCell(3, startColumn + index);
        cell.value = header;
        setFill(3, startColumn + index, subheaderColor);
      });
      currentColumn = endColumn + 1;
      return startColumn;
    };

    worksheet.mergeCells(2, 1, 3, 1);
    worksheet.mergeCells(2, 2, 3, 2);
    worksheet.getCell(2, 1).value = 'Alumno';
    worksheet.getCell(2, 2).value = 'Legajo';
    setFill(2, 1, 'FFDCEAF4');
    setFill(3, 1, 'FFDCEAF4');
    setFill(2, 2, 'FFDCEAF4');
    setFill(3, 2, 'FFDCEAF4');

    const evaluationColumns = evaluations.map(group => {
      const startColumn = addGroup(
        group.label,
        ['Fecha', 'Nota'],
        group.evaluation.isRecovery ? 'FFFFC857' : 'FFFFF200',
        group.evaluation.isRecovery ? 'FFFFE7A3' : 'FFFFF7A8'
      );
      worksheet.getColumn(startColumn).width = 14;
      worksheet.getColumn(startColumn + 1).width = 12;
      return startColumn;
    });

    const examStartColumn = addGroup(
      'Examen Regular-Libre', ['Fecha', 'Nota', 'Condición'], 'FF92D050', 'FFD9EAD3'
    );
    worksheet.getColumn(examStartColumn).width = 24;
    worksheet.getColumn(examStartColumn + 1).width = 12;
    worksheet.getColumn(examStartColumn + 2).width = 20;

    const finalStartColumn = addGroup(
      'FINAL', ['Libro', 'Folio', 'Fecha', 'Nota N°', 'Condición'], 'FF70AD47', 'FFE2F0D9'
    );
    [12, 12, 14, 12, 20].forEach((width, index) => {
      worksheet.getColumn(finalStartColumn + index).width = width;
    });

    worksheet.getColumn(1).width = 28;
    worksheet.getColumn(2).width = 18;

    const lastColumn = currentColumn - 1;
    worksheet.mergeCells(1, 1, 1, lastColumn);
    const titleCell = worksheet.getCell(1, 1);
    titleCell.value = 'Instancias de Evaluación';
    titleCell.font = { name: 'Arial', size: 16, bold: true, color: { argb: 'FFFFFFFF' } };
    titleCell.alignment = { horizontal: 'center', vertical: 'middle' };
    for (let column = 1; column <= lastColumn; column++) setFill(1, column, 'FF287DB2');
    worksheet.getRow(1).height = 32;

    for (const rowNumber of [2, 3]) {
      const headerRow = worksheet.getRow(rowNumber);
      headerRow.height = 26;
      headerRow.eachCell(cell => {
        cell.font = { name: 'Arial', size: 11, bold: true, color: { argb: 'FF20384E' } };
        cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
        cell.border = {
          top: { style: 'thin', color: { argb: 'FF647789' } },
          left: { style: 'thin', color: { argb: 'FF647789' } },
          bottom: { style: 'thin', color: { argb: 'FF647789' } },
          right: { style: 'thin', color: { argb: 'FF647789' } }
        };
      });
    }
    worksheet.views = [{ state: 'frozen', xSplit: 2, ySplit: 3 }];

    const formatDate = (value: string): string => new Intl.DateTimeFormat('es-AR', {
      day: '2-digit', month: '2-digit', year: '2-digit'
    }).format(new Date(value));

    for (const student of this.filteredRows) {
      const values: (string | number | Date | null)[] = [student.studentName, student.legajoNumber];
      evaluations.forEach((group, index) => {
        const grade = student.editableGrades[group.evaluation.id];
        values.push(grade?.updatedAt ? new Date(grade.updatedAt) : null, grade?.score ?? null);
      });

      const exam = student.latestExam;
      values.push(
        exam ? `${formatDate(exam.examDateUtc)}\nIntento ${exam.attemptNumber}` : null,
        exam?.status === 'Published' ? exam.grade : null,
        exam?.status === 'Published' ? this.examOutcomeLabel(exam.outcome) : null,
        null, null, null, null, null
      );
      const excelRow = worksheet.addRow(values);
      excelRow.height = 30;
      evaluationColumns.forEach(column => {
        excelRow.getCell(column).numFmt = 'dd/mm/yy';
      });
      excelRow.eachCell({ includeEmpty: true }, cell => {
        cell.font = { name: 'Arial', size: 11, color: { argb: 'FF20384E' } };
        cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
        cell.border = {
          top: { style: 'thin', color: { argb: 'FF9AA7B1' } },
          left: { style: 'thin', color: { argb: 'FF9AA7B1' } },
          bottom: { style: 'thin', color: { argb: 'FF9AA7B1' } },
          right: { style: 'thin', color: { argb: 'FF9AA7B1' } }
        };
      });
    }

    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer as BlobPart], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const fileName = `notas_${this.detail.gradebook.courseName}_${this.detail.gradebook.divisionName}`
      .replace(/[<>:"/\\|?*\u0000-\u001F]/g, '_');
    a.download = `${fileName}.xlsx`;
    a.click();
    URL.revokeObjectURL(url);
  }

  // ===================== Alumno =====================

  loadMyGrades(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.gradebookService.getMyGrades().subscribe({
      next: (gradebooks) => {
        this.myGradebooks = gradebooks;
        this.refreshStudentAcademicYears();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.message || 'No se pudieron cargar tus calificaciones.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadStudentProfile(): void {
    this.userService.getProfile().subscribe({
      next: profile => {
        this.studentProfile = profile;
        this.cdr.detectChanges();
      },
      error: err => {
        this.errorMsg = this.readErrorMessage(err, 'No se pudieron cargar tus datos personales.');
        this.cdr.detectChanges();
      }
    });
  }

  loadStudentAcademicProgress(careerId = this.selectedCareerId ?? undefined): void {
    const requestId = ++this.studentProgressRequestId;
    this.isStudentProgressLoading = true;
    this.studentService.getMyAcademicProgress(careerId).subscribe({
      next: progress => {
        if (requestId !== this.studentProgressRequestId) return;
        this.studentProgress = progress;
        this.isStudentProgressLoading = false;
        if (this.selectedCareerId == null) this.selectedCareerId = progress.careerId;
        this.refreshStudentAcademicYears();
        this.studentService.getStudent(progress.studentId).subscribe({
          next: student => {
            if (requestId !== this.studentProgressRequestId) return;
            this.studentRecord = student;
            this.cdr.detectChanges();
          },
          error: err => {
            if (requestId !== this.studentProgressRequestId) return;
            this.errorMsg = this.readErrorMessage(err, 'No se pudo cargar tu legajo.');
            this.cdr.detectChanges();
          }
        });
        this.cdr.detectChanges();
      },
      error: err => {
        if (requestId !== this.studentProgressRequestId) return;
        this.studentProgress = null;
        this.isStudentProgressLoading = false;
        this.errorMsg = this.readErrorMessage(err, 'No se pudo cargar tu progreso académico.');
        this.cdr.detectChanges();
      }
    });
  }

  loadMyExamTables(): void {
    this.studentService.getMyExamTables().subscribe({
      next: examTables => {
        this.studentExamTables = examTables;
        this.cdr.detectChanges();
      },
      error: err => {
        this.errorMsg = this.readErrorMessage(err, 'No se pudieron cargar tus mesas de examen.');
        this.cdr.detectChanges();
      }
    });
  }

  onStudentCareerChange(): void {
    this.studentProgress = null;
    this.academicYears = [];
    this.selectedAcademicYear = null;
    this.isStudentProgressLoading = true;
    this.loadStudentAcademicProgress(this.selectedCareerId ?? undefined);
  }

  get studentDisplayName(): string {
    const name = [this.studentProfile?.username, this.studentProfile?.lastName]
      .filter(Boolean)
      .join(' ');
    return name || this.studentRecord?.userName || '';
  }

  private refreshStudentAcademicYears(): void {
    if (!this.studentProgress) return;
    this.academicYears = [...new Set(this.studentProgress.courses.map(course => course.yearNumber))]
      .sort((a, b) => a - b);
    if (this.selectedAcademicYear == null || !this.academicYears.includes(this.selectedAcademicYear)) {
      this.selectedAcademicYear = this.academicYears[0] ?? null;
    }
  }

  get filteredMyGradebooks(): StudentPublishedGradebook[] {
    if (!this.studentProgress) return [];
    let gradebooks = this.myGradebooks;
    if (this.selectedCareerId !== this.studentProgress.careerId) return [];
    if (this.selectedAcademicYear != null) {
      const yearByCourse = new Map(this.studentProgress.courses.map(course => [course.courseId, course.yearNumber]));
      gradebooks = gradebooks.filter(gradebook => yearByCourse.get(gradebook.courseId) === this.selectedAcademicYear);
    }
    return gradebooks;
  }

  get studentGradeRows(): StudentGradeRow[] {
    if (!this.studentProgress || this.selectedCareerId !== this.studentProgress.careerId) return [];
    const gradebooksByCourse = new Map<number, StudentPublishedGradebook[]>();
    for (const gradebook of this.filteredMyGradebooks) {
      const books = gradebooksByCourse.get(gradebook.courseId) ?? [];
      books.push(gradebook);
      gradebooksByCourse.set(gradebook.courseId, books);
    }

    return this.studentProgress.courses
      .filter(course => course.enrollmentStatus != null && course.enrollmentStatus !== 'Withdrawn')
      .filter(course => this.selectedAcademicYear == null || course.yearNumber === this.selectedAcademicYear)
      .map(course => {
        const gradebooks = gradebooksByCourse.get(course.courseId) ?? [];
        const gradebook = gradebooks.find(item =>
          item.academicYear === course.academicYear && item.semester === course.semester
        ) ?? null;
        return {
          courseId: course.courseId,
          courseName: course.name,
          yearNumber: course.yearNumber,
          academicYear: course.academicYear,
          semester: course.semester,
          gradebook,
          latestExam: this.latestExamForCourse(course.courseId, course.academicYear),
          finalGrade: course.finalGrade,
          enrollmentStatus: course.enrollmentStatus
        };
      });
  }

  private latestExamForCourse(courseId: number, academicYear: number | null): StudentExamDisplay | null {
    const latestExam = this.studentExamTables
      .filter(item => item.examTable.courseId === courseId
        && item.registrationId != null
        && (academicYear == null || item.examTable.academicYear === academicYear))
      .sort((left, right) =>
        (right.attemptNumber ?? 0) - (left.attemptNumber ?? 0)
        || Date.parse(right.examTable.examDateUtc) - Date.parse(left.examTable.examDateUtc)
      )[0];

    if (!latestExam || latestExam.attemptNumber == null) return null;
    return {
      examDateUtc: latestExam.examTable.examDateUtc,
      attemptNumber: latestExam.attemptNumber,
      status: latestExam.examTable.status,
      grade: latestExam.result?.grade ?? null,
      outcome: latestExam.result?.outcome ?? null
    };
  }

  get studentEvaluationGroups(): EvaluationGroup[] {
    const seen = new Set<string>();
    const groups: EvaluationGroup[] = [];
    for (const gradebook of this.filteredMyGradebooks) {
      for (const evaluation of [...gradebook.evaluations].sort((left, right) => left.displayOrder - right.displayOrder)) {
        const key = evaluation.name.trim().toLocaleLowerCase();
        if (seen.has(key)) continue;
        seen.add(key);
        groups.push({ label: evaluation.name, evaluation });
      }
    }
    return groups;
  }

  get myColumns(): string[] {
    return this.studentEvaluationGroups.map(group => group.label);
  }

  myGradeForEvaluation(row: StudentGradeRow, group: EvaluationGroup): { score: number | null; updatedAt: string | null } | null {
    const gradebook = row.gradebook;
    if (!gradebook) return null;
    const evaluation = gradebook.evaluations.find(item => item.name === group.label);
    if (!evaluation) return null;
    const grade = gradebook.grades.find(item => item.evaluationId === evaluation.id);
    return { score: grade?.score ?? null, updatedAt: grade?.updatedAt ?? null };
  }

  studentConditionLabel(status: string | null): string {
    switch (status) {
      case 'Approved': return 'Aprobado';
      case 'Promoted': return 'Promocionado';
      case 'Regularized': return 'Regular';
      case 'Failed': return 'Libre';
      case 'Enrolled': return 'En curso';
      default: return '—';
    }
  }

  myGradeFor(row: StudentGradeRow, columnName: string): { score: number | null; updatedAt: string | null } | null {
    const gradebook = row.gradebook;
    if (!gradebook) return null;
    const evaluation = gradebook.evaluations.find(e => e.name === columnName);
    if (!evaluation) return null;
    const grade = gradebook.grades.find(g => g.evaluationId === evaluation.id);
    return { score: grade?.score ?? null, updatedAt: grade?.updatedAt ?? null };
  }

  async downloadMySummary(): Promise<void> {
    const rows = this.studentGradeRows;
    if (rows.length === 0) return;

    const workbook = new ExcelJS.Workbook();
    workbook.creator = 'Academia Digital';
    const worksheet = workbook.addWorksheet('Mis calificaciones');
    const columns = this.myColumns;
    const lastColumn = 2 + columns.length * 2 + 3;
    worksheet.mergeCells(1, 1, 1, lastColumn);
    const title = worksheet.getCell(1, 1);
    title.value = 'Instancias de Evaluación';
    title.font = { name: 'Arial', size: 16, bold: true, color: { argb: 'FFFFFFFF' } };
    title.alignment = { horizontal: 'center', vertical: 'middle' };
    worksheet.getRow(1).height = 32;

    const fill = (row: number, column: number, color: string): void => {
      worksheet.getCell(row, column).fill = {
        type: 'pattern',
        pattern: 'solid',
        fgColor: { argb: color }
      };
    };
    for (let column = 1; column <= lastColumn; column++) fill(1, column, 'FF287DB2');

    worksheet.mergeCells(2, 1, 3, 1);
    worksheet.mergeCells(2, 2, 3, 2);
    worksheet.getCell(2, 1).value = 'Alumno';
    worksheet.getCell(2, 2).value = 'Materia';
    fill(2, 1, 'FFDCEAF4');
    fill(3, 1, 'FFDCEAF4');
    fill(2, 2, 'FFDCEAF4');
    fill(3, 2, 'FFDCEAF4');
    worksheet.getColumn(1).width = 28;
    worksheet.getColumn(2).width = 28;

    columns.forEach((columnName, index) => {
      const startColumn = 3 + index * 2;
      worksheet.mergeCells(2, startColumn, 2, startColumn + 1);
      worksheet.getCell(2, startColumn).value = columnName;
      worksheet.getCell(3, startColumn).value = 'Fecha';
      worksheet.getCell(3, startColumn + 1).value = 'Nota';
      fill(2, startColumn, 'FFFFF200');
      fill(2, startColumn + 1, 'FFFFF200');
      fill(3, startColumn, 'FFFFF7A8');
      fill(3, startColumn + 1, 'FFFFF7A8');
      worksheet.getColumn(startColumn).width = 14;
      worksheet.getColumn(startColumn + 1).width = 12;
    });

    const averageColumn = lastColumn - 2;
    const conditionColumn = lastColumn - 1;
    const statusColumn = lastColumn;
    worksheet.mergeCells(2, averageColumn, 3, averageColumn);
    worksheet.mergeCells(2, conditionColumn, 3, conditionColumn);
    worksheet.getCell(2, averageColumn).value = 'Promedio';
    worksheet.getCell(2, conditionColumn).value = 'Condición';
    fill(2, averageColumn, 'FFDDEBF7');
    fill(3, averageColumn, 'FFDDEBF7');
    fill(2, conditionColumn, 'FFDDEBF7');
    fill(3, conditionColumn, 'FFDDEBF7');
    worksheet.getColumn(averageColumn).width = 14;
    worksheet.getColumn(conditionColumn).width = 20;
    worksheet.mergeCells(2, statusColumn, 3, statusColumn);
    worksheet.getCell(2, statusColumn).value = 'Estado';
    fill(2, statusColumn, 'FFDDEBF7');
    fill(3, statusColumn, 'FFDDEBF7');
    worksheet.getColumn(statusColumn).width = 20;

    for (const rowNumber of [2, 3]) {
      const headerRow = worksheet.getRow(rowNumber);
      headerRow.height = 26;
      headerRow.eachCell(cell => {
        cell.font = { name: 'Arial', size: 11, bold: true, color: { argb: 'FF20384E' } };
        cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
        cell.border = {
          top: { style: 'thin', color: { argb: 'FF647789' } },
          left: { style: 'thin', color: { argb: 'FF647789' } },
          bottom: { style: 'thin', color: { argb: 'FF647789' } },
          right: { style: 'thin', color: { argb: 'FF647789' } }
        };
      });
    }

    for (const gradeRow of rows) {
      const values: (string | number | Date | null)[] = [this.studentDisplayName, gradeRow.courseName];
      for (const columnName of columns) {
        const grade = this.myGradeFor(gradeRow, columnName);
        values.push(grade?.updatedAt ? new Date(grade.updatedAt) : null, grade?.score ?? null);
      }
      values.push(
        gradeRow.gradebook?.average ?? null,
        gradeRow.gradebook?.resultStatus ? this.resultLabel(gradeRow.gradebook.resultStatus) : null,
        gradeRow.gradebook ? this.statusLabel(gradeRow.gradebook.status) : 'En cursada'
      );
      const row = worksheet.addRow(values);
      row.height = 30;
      columns.forEach((_, index) => {
        row.getCell(3 + index * 2).numFmt = 'dd/mm/yy';
      });
      row.eachCell({ includeEmpty: true }, cell => {
        cell.font = { name: 'Arial', size: 11, color: { argb: 'FF20384E' } };
        cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
        cell.border = {
          top: { style: 'thin', color: { argb: 'FF9AA7B1' } },
          left: { style: 'thin', color: { argb: 'FF9AA7B1' } },
          bottom: { style: 'thin', color: { argb: 'FF9AA7B1' } },
          right: { style: 'thin', color: { argb: 'FF9AA7B1' } }
        };
      });
    }

    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer as BlobPart], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `mis_calificaciones_${this.selectedAcademicYear ?? ''}.xlsx`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
