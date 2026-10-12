import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, catchError, concatMap, firstValueFrom, forkJoin, from, map, of, takeUntil, timeout, toArray } from 'rxjs';
import Swal from 'sweetalert2';
import { ExamTableService, StudentExamTable } from '../../../core/services/exam-table.service';
import { StudentAcademicProgress, StudentService } from '../../../core/services/student.service';

interface ExamOption extends StudentExamTable {
  enrollmentId: number | null;
  year: number;
}
interface ExamYear { year: number; subjects: ExamOption[]; }

@Component({
  selector: 'app-exam-enrollment',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './exam-enrollment.component.html',
  styleUrl: './exam-enrollment.component.scss'
})
export class ExamEnrollmentComponent implements OnInit, OnDestroy {
  readonly scheduleImage: string | null = null;
  private readonly destroy$ = new Subject<void>();
  private destroyed = false;
  progress: StudentAcademicProgress | null = null;
  years: ExamYear[] = [];
  tables: ExamOption[] = [];
  selections = new Set<number>();
  showMyEnrollments = false;
  loading = false;
  submitting = false;
  loadError = '';
  validationMessage = '';

  constructor(
    private readonly cdr: ChangeDetectorRef,
    private readonly exams: ExamTableService,
    private readonly students: StudentService
  ) {}

  ngOnInit(): void { void this.load(); }
  ngOnDestroy(): void { this.destroyed = true; this.destroy$.next(); this.destroy$.complete(); }

  get hasSoftwarePlan(): boolean {
    return !!this.progress && /desarrollo de software/i.test(this.progress.careerName)
      && /2023/.test(this.progress.studyPlanName);
  }
  get registrations(): ExamOption[] { return this.tables.filter(item => item.registrationId != null); }
  get selectedSubjects(): ExamOption[] { return this.tables.filter(item => this.selections.has(item.examTable.id)); }

  async load(): Promise<void> {
    if (this.loading || this.destroyed) return;
    this.loading = true;
    this.loadError = '';
    this.cdr.detectChanges();
    try {
      const data = await firstValueFrom(this.students.getMyAcademicProgress().pipe(
        concatMap(progress => forkJoin({
          progress: of(progress), tables: this.exams.getMine(),
          history: this.exams.getAcademicEnrollments(progress.studentId)
        })), timeout(20000), takeUntil(this.destroy$)
      ), { defaultValue: undefined });
      if (!data || this.destroyed) return;
      this.progress = data.progress;
      this.tables = data.tables.map(item => {
        const course = data.progress.courses.find(course => course.courseId === item.examTable.courseId);
        const candidates = data.history.filter(entry => entry.courseId === item.examTable.courseId && entry.status === 'Regularized')
          .sort((a, b) => b.academicYear - a.academicYear || b.semester - a.semester || b.enrollmentId - a.enrollmentId);
        return { ...item, year: course?.yearNumber ?? 0,
          enrollmentId: course?.enrollmentStatus === 'Regularized' ? candidates[0]?.enrollmentId ?? null : null };
      });
      this.years = [...new Set(this.tables.map(item => item.year))].sort((a, b) => a - b)
        .map(year => ({ year, subjects: this.tables.filter(item => item.year === year)
          .sort((a, b) => a.examTable.courseName.localeCompare(b.examTable.courseName)
            || a.examTable.examDateUtc.localeCompare(b.examTable.examDateUtc)) }));
      this.selections = new Set(this.selectedSubjects.filter(item => this.canSelect(item)).map(item => item.examTable.id));
    } catch (error) {
      this.loadError = this.errorMessage(error, 'No se pudieron cargar las mesas de examen. Intentá nuevamente.');
    } finally {
      if (!this.destroyed) { this.loading = false; this.cdr.detectChanges(); }
    }
  }

  canSelect(item: ExamOption): boolean {
    return item.registrationId == null && item.canRegister && item.enrollmentId != null
      && item.examTable.status === 'Open' && Date.parse(item.examTable.registrationDeadlineUtc) >= Date.now();
  }

  statusLabel(item: ExamOption): string {
    if (item.registrationId != null) return 'Inscripción registrada';
    if (item.examTable.status !== 'Open' || Date.parse(item.examTable.registrationDeadlineUtc) < Date.now()) return 'Inscripción cerrada';
    if (item.enrollmentId == null) return 'Requiere una cursada regularizada vigente';
    return item.canRegister ? '' : 'No disponible para inscripción';
  }

  select(item: ExamOption): void {
    if (this.submitting || this.loading || this.loadError || !this.canSelect(item)) return;
    const id = item.examTable.id;
    if (this.selections.has(id)) this.selections.delete(id);
    else {
      // Una materia por envio: el alumno elige el llamado concreto que desea rendir.
      for (const other of this.tables.filter(other => other.examTable.courseId === item.examTable.courseId)) {
        this.selections.delete(other.examTable.id);
      }
      this.selections.add(id);
    }
    this.validationMessage = '';
  }

  async submit(): Promise<void> {
    if (this.submitting || this.loading || this.loadError) return;
    const selection = this.selectedSubjects;
    if (!selection.length || selection.some(item => !this.canSelect(item))) {
      this.validationMessage = 'Seleccioná al menos una mesa disponible en condición Regular.';
      return;
    }
    this.submitting = true;
    this.validationMessage = '';
    try {
      const confirmation = await Swal.fire({
        icon: 'question', titleText: 'Confirmar inscripción',
        text: selection.map(item => `${item.examTable.courseName} (llamado ${item.examTable.callNumber})`).join('; '),
        confirmButtonText: 'Inscribirme', showCancelButton: true, cancelButtonText: 'Volver',
        confirmButtonColor: '#00579c', heightAuto: false
      });
      if (!confirmation.isConfirmed || this.destroyed) return;
      const results = await firstValueFrom(from(selection).pipe(
        concatMap(item => this.exams.register(item.examTable.id, item.enrollmentId!).pipe(
          timeout(20000),
          map(registration => ({ item, registration, error: '' })),
          catchError(error => of({ item, registration: null,
            error: this.errorMessage(error, 'No se pudo confirmar. Revisá Mis inscripciones antes de reintentar.') }))
        )), toArray(), takeUntil(this.destroy$)
      ), { defaultValue: [] });
      if (this.destroyed) return;
      for (const result of results) {
        if (!result.registration) continue;
        result.item.registrationId = result.registration.id;
        result.item.attemptNumber = result.registration.attemptNumber;
        result.item.canRegister = false;
        this.selections.delete(result.item.examTable.id);
      }
      const failed = results.filter(result => !result.registration);
      const saved = results.length - failed.length;
      this.showMyEnrollments = failed.length === 0;
      await this.load();
      if (this.destroyed) return;
      await Swal.fire({
        icon: failed.length ? (saved ? 'warning' : 'error') : 'success',
        titleText: failed.length ? 'Revisá el resultado de la inscripción' : 'Inscripción registrada',
        text: `${saved} de ${results.length} inscripción(es) confirmada(s). ` +
          failed.map(result => `${result.item.examTable.courseName}: ${result.error}`).join(' '),
        confirmButtonText: 'Entendido', confirmButtonColor: '#00579c', heightAuto: false
      });
    } finally {
      if (!this.destroyed) { this.submitting = false; this.cdr.detectChanges(); }
    }
  }

  private errorMessage(error: unknown, fallback: string): string {
    const value = error as { error?: { detail?: string; msg?: string }; message?: string; name?: string };
    if (value?.name === 'TimeoutError') return fallback;
    return value?.error?.detail || value?.error?.msg || value?.message || fallback;
  }
}
