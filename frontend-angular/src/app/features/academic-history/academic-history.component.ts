import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { StudentService, StudentAcademicProgress, StudentCourseProgress } from '../../core/services/student.service';

// Etiquetas de los 5 estados reales (sin agrupar), mapeadas desde Enrollment.Status crudo.
const STATUS_LABELS: Record<string, string> = {
  Enrolled: 'En curso',
  Regularized: 'Regular',
  Failed: 'Libre',
  Approved: 'Aprobada',
  Promoted: 'Promocionada'
};

interface YearGroup {
  yearNumber: number;
  courses: StudentCourseProgress[];
}

@Component({
  selector: 'app-academic-history',
  templateUrl: './academic-history.component.html',
  styleUrls: ['./academic-history.component.scss'],
  standalone: false
})
export class AcademicHistoryComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  progress: StudentAcademicProgress | null = null;
  yearGroups: YearGroup[] = [];

  isLoading = false;
  errorMsg = '';

  constructor(
    private readonly studentService: StudentService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.studentService.getMyAcademicProgress().pipe(takeUntil(this.destroy$)).subscribe({
      next: (progress) => {
        this.progress = progress;
        this.yearGroups = this.groupByYear(progress.courses);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err?.error?.detail || err.message || 'No se pudo cargar tu historial académico.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private groupByYear(courses: StudentCourseProgress[]): YearGroup[] {
    const byYear = new Map<number, StudentCourseProgress[]>();
    for (const c of courses) {
      const list = byYear.get(c.yearNumber) ?? [];
      list.push(c);
      byYear.set(c.yearNumber, list);
    }
    return [...byYear.entries()]
      .sort((a, b) => a[0] - b[0])
      .map(([yearNumber, list]) => ({
        yearNumber,
        courses: list.sort((a, b) => a.semester - b.semester || a.name.localeCompare(b.name))
      }));
  }

  // Etiqueta legible; si la materia no tiene inscripción, se muestra como "Sin cursar".
  statusLabel(course: StudentCourseProgress): string {
    if (!course.enrollmentStatus) return 'Sin cursar';
    return STATUS_LABELS[course.enrollmentStatus] ?? course.enrollmentStatus;
  }

  // Clase CSS del chip por estado (reusa la paleta de condición del resto de la app).
  statusClass(course: StudentCourseProgress): string {
    const s = course.enrollmentStatus;
    if (!s) return 'chip--none';
    return `chip--${s.toLowerCase()}`;
  }

  hasGrade(course: StudentCourseProgress): boolean {
    return course.finalGrade != null;
  }
}
