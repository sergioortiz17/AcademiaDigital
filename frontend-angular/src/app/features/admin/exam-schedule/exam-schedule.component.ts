import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { from, of } from 'rxjs';
import { catchError, concatMap, map, toArray } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { Career, CareerService } from '../../../core/services/career.service';
import { Teacher, TeacherService } from '../../../core/services/teacher.service';
import { ExamProposalRow, ExamScheduleService } from '../../../core/services/exam-schedule.service';

interface EditableExam {
  row: ExamProposalRow;
  include: boolean;
  date: string;
  presidentId: number | null;
  vocalIds: number[];
  result?: string;
  ok?: boolean;
}

/**
 * Mesas de examen automáticas: el sistema propone fecha y tribunal por materia dentro de un rango
 * (sin dos materias del mismo año el mismo día y respetando correlativas). Administración ajusta y confirma.
 */
@Component({
  selector: 'app-exam-schedule',
  templateUrl: './exam-schedule.component.html',
  styleUrls: ['./exam-schedule.component.scss'],
  standalone: false
})
export class ExamScheduleComponent implements OnInit {
  careers: Career[] = [];
  teachers: Teacher[] = [];
  careerId: number | null = null;
  academicYear = new Date().getFullYear();
  callNumber = 1;
  fromDate = '';
  toDate = '';
  years = [1, 2, 3];
  selectedYears = new Set<number>([1, 2, 3]);
  examTime = '18:00';
  deadlineDays = 3;
  location = 'Sede central';
  blockedText = '';

  exams: EditableExam[] = [];
  isLoading = false;
  creating = false;
  errorMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly teacherService: TeacherService,
    private readonly service: ExamScheduleService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe(c => { this.careers = c; this.cdr.markForCheck(); });
    this.teacherService.getTeachers(false).subscribe(t => {
      this.teachers = t.sort((a, b) => this.teacherName(a).localeCompare(this.teacherName(b), 'es'));
      this.cdr.markForCheck();
    });
  }

  teacherName(t: Teacher): string {
    return `${t.lastName ?? ''}, ${t.firstName ?? ''}`.replace(/^, |, $/g, '');
  }

  toggleYear(year: number, checked: boolean): void {
    if (checked) this.selectedYears.add(year); else this.selectedYears.delete(year);
  }

  get canPropose(): boolean {
    return this.careerId != null && !!this.fromDate && !!this.toDate && this.selectedYears.size > 0;
  }

  propose(): void {
    if (!this.canPropose) return;
    this.isLoading = true;
    this.errorMsg = '';
    const blockedDates = this.blockedText.split(/[\s,;]+/).map(d => d.trim()).filter(d => /^\d{4}-\d{2}-\d{2}$/.test(d));
    this.service.propose({
      careerId: this.careerId!, academicYear: this.academicYear, from: this.fromDate, to: this.toDate,
      yearNumbers: [...this.selectedYears], blockedDates
    }).subscribe({
      next: rows => {
        this.exams = rows.map(row => ({
          row,
          include: row.date != null,
          date: row.date ?? '',
          presidentId: row.tribunal.find(t => t.role === 'President')?.teacherId ?? null,
          vocalIds: row.tribunal.filter(t => t.role === 'Vocal').map(t => t.teacherId)
        }));
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: err => { this.errorMsg = err?.message || 'No se pudo armar la propuesta.'; this.isLoading = false; this.cdr.markForCheck(); }
    });
  }

  /** El tribunal necesita presidente y al menos un vocal distinto (2 a 5 docentes). */
  problem(exam: EditableExam): string | null {
    if (!exam.date) return 'Falta la fecha.';
    if (exam.presidentId == null) return 'Falta el presidente del tribunal.';
    const vocals = exam.vocalIds.filter(id => id !== exam.presidentId);
    if (vocals.length === 0) return 'Falta al menos un vocal.';
    if (vocals.length > 4) return 'El tribunal admite hasta 5 docentes.';
    return null;
  }

  sameDayWarning(exam: EditableExam): boolean {
    return !!exam.date && this.exams.some(other => other !== exam && other.include
      && other.date === exam.date && other.row.yearNumber === exam.row.yearNumber);
  }

  get selectedCount(): number { return this.exams.filter(e => e.include).length; }
  get readyCount(): number { return this.exams.filter(e => e.include && !this.problem(e)).length; }

  async create(): Promise<void> {
    const ready = this.exams.filter(e => e.include && !this.problem(e) && !e.ok);
    if (ready.length === 0) return;
    const confirm = await Swal.fire({
      icon: 'question', titleText: `¿Crear ${ready.length} mesa(s) de examen?`,
      text: `Llamado ${this.callNumber} de ${this.academicYear}. La inscripción cierra ${this.deadlineDays} día(s) antes de cada mesa.`,
      showCancelButton: true, confirmButtonText: 'Crear mesas', cancelButtonText: 'Cancelar'
    });
    if (!confirm.isConfirmed) return;
    this.creating = true;
    from(ready).pipe(
      concatMap(exam => {
        const examDate = this.localDateTime(exam.date, this.examTime);
        const deadline = new Date(examDate.getTime() - this.deadlineDays * 86400000);
        const tribunal = [
          { teacherId: exam.presidentId!, role: 'President' as const },
          ...exam.vocalIds.filter(id => id !== exam.presidentId).map(id => ({ teacherId: id, role: 'Vocal' as const }))
        ];
        const key = `schedule-${this.academicYear}-${this.callNumber}-${exam.row.courseId}-${exam.date}`;
        return this.service.create({
          courseId: exam.row.courseId, academicYear: this.academicYear, callNumber: this.callNumber,
          examDateUtc: examDate.toISOString(), registrationDeadlineUtc: deadline.toISOString(),
          location: this.location.trim() || 'A confirmar', tribunal
        }, key).pipe(
          map(() => { exam.ok = true; exam.result = 'Creada'; }),
          catchError(err => { exam.ok = false; exam.result = err?.message || 'No se pudo crear.'; return of(null); })
        );
      }),
      toArray()
    ).subscribe(() => { this.creating = false; this.cdr.markForCheck(); });
  }

  private localDateTime(date: string, time: string): Date {
    const [y, m, d] = date.split('-').map(Number);
    const [hh, mm] = (time || '18:00').split(':').map(Number);
    return new Date(y, m - 1, d, hh, mm);
  }
}
