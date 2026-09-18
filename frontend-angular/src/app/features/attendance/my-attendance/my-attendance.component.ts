import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CareerService, Career } from '../../../core/services/career.service';
import {
  AttendanceService,
  AttendanceSummaryItem,
  translateAttendanceError
} from '../../../core/services/attendance.service';

type SortMode = 'lowest' | 'alpha';
export type RiskLevel = 'low' | 'medium' | 'high';

export interface CourseGroup {
  courseId: number;
  courseName: string;
  years: AttendanceSummaryItem[];
  current: AttendanceSummaryItem;
  isRepeat: boolean;
  isAtRisk: boolean;
  expanded: boolean;
}

@Component({
  selector: 'app-my-attendance',
  templateUrl: './my-attendance.component.html',
  styleUrls: ['./my-attendance.component.scss'],
  standalone: false
})
export class MyAttendanceComponent implements OnInit {
  // Carrera decorativo, consistente con el resto de las vistas de Alumno.
  careers: Career[] = [];
  selectedCareerId: number | null = null;

  // Materia sí filtra de verdad (se arma con las materias reales del resumen).
  selectedCourseId: number | null = null;

  // Desde/Hasta son decorativos: el backend todavía no expone el detalle de asistencia
  // día por día para el alumno, solo el resumen por materia/año.
  dateFrom: string | null = null;
  dateTo: string | null = null;

  sortMode: SortMode = 'lowest';

  allGroups: CourseGroup[] = [];
  selectedGroup: CourseGroup | null = null;
  selectedYearItem: AttendanceSummaryItem | null = null;

  isLoading = false;
  errorMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly attendanceService: AttendanceService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe(careers => {
      this.careers = careers;
      this.selectedCareerId = careers[0]?.id ?? null;
      this.cdr.detectChanges();
    });
    this.loadSummary();
  }

  private showError(message: string): void {
    this.errorMsg = message;
    this.cdr.detectChanges();
    setTimeout(() => { this.errorMsg = ''; this.cdr.detectChanges(); }, 6000);
  }

  loadSummary(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.attendanceService.getMySummary().subscribe({
      next: (summary) => {
        this.buildGroups(summary.items);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.showError(translateAttendanceError(err.message, 'No se pudieron cargar tus asistencias.'));
      }
    });
  }

  private buildGroups(items: AttendanceSummaryItem[]): void {
    const byCourse = new Map<number, AttendanceSummaryItem[]>();
    for (const item of items) {
      const list = byCourse.get(item.courseId) ?? [];
      list.push(item);
      byCourse.set(item.courseId, list);
    }
    this.allGroups = [...byCourse.entries()].map(([courseId, years]) => {
      const sortedYears = [...years].sort((a, b) => b.academicYear - a.academicYear);
      const current = sortedYears[0];
      return {
        courseId,
        courseName: current.courseName,
        years: sortedYears,
        current,
        isRepeat: sortedYears.length > 1,
        isAtRisk: current.isAtRisk,
        expanded: false
      };
    });
    this.selectedGroup = this.allGroups[0] ?? null;
    this.selectedYearItem = this.selectedGroup?.current ?? null;
  }

  get filteredGroups(): CourseGroup[] {
    if (this.selectedCourseId == null) return this.allGroups;
    return this.allGroups.filter(g => g.courseId === this.selectedCourseId);
  }

  get atRiskGroups(): CourseGroup[] {
    return this.sortGroups(this.filteredGroups.filter(g => g.isAtRisk));
  }

  get restGroups(): CourseGroup[] {
    return this.sortGroups(this.filteredGroups.filter(g => !g.isAtRisk));
  }

  private sortGroups(groups: CourseGroup[]): CourseGroup[] {
    const copy = [...groups];
    if (this.sortMode === 'alpha') {
      copy.sort((a, b) => a.courseName.localeCompare(b.courseName));
    } else {
      copy.sort((a, b) => (a.current.attendancePercentage ?? 0) - (b.current.attendancePercentage ?? 0));
    }
    return copy;
  }

  setSortMode(mode: SortMode): void {
    this.sortMode = mode;
  }

  toggleExpand(group: CourseGroup, event: Event): void {
    event.stopPropagation();
    group.expanded = !group.expanded;
  }

  selectGroup(group: CourseGroup, item?: AttendanceSummaryItem): void {
    this.selectedGroup = group;
    this.selectedYearItem = item ?? group.current;
  }

  riskLevel(percentage: number | null): RiskLevel {
    if (percentage == null) return 'medium';
    if (percentage >= 85) return 'low';
    if (percentage >= 70) return 'medium';
    return 'high';
  }

  formatPercentage(value: number | null): string {
    if (value == null) return '—';
    return `${Math.round(value)}%`;
  }

  get generalAverageLabel(): string {
    const items = this.allGroups.flatMap(g => g.years);
    const totalEarned = items.reduce((sum, i) => sum + i.earnedUnits, 0);
    const totalPossible = items.reduce((sum, i) => sum + i.possibleUnits, 0);
    if (totalPossible === 0) return '—';
    const value = (totalEarned / totalPossible) * 100;
    return value.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '%';
  }

  get totalMaterias(): number {
    return this.allGroups.length;
  }

  get totalCursadas(): number {
    return this.allGroups.reduce((sum, g) => sum + g.years.length, 0);
  }

  get totalAtRisk(): number {
    return this.allGroups.filter(g => g.isAtRisk).length;
  }

  buscar(): void {
    // Desde/Hasta quedan como filtro decorativo: no hay endpoint de detalle diario para el alumno.
  }

  downloadSummary(): void {
    const header = ['Materia', 'Año', 'Presentes', 'Tardanzas', 'Ausentes', 'Justificadas', 'Asistencia'];
    const lines = [header.join(',')];

    for (const group of this.allGroups) {
      for (const item of group.years) {
        lines.push([
          group.courseName,
          item.academicYear,
          item.presentCount,
          item.lateCount,
          item.absentCount,
          item.justifiedCount,
          this.formatPercentage(item.attendancePercentage)
        ].join(','));
      }
    }

    const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'mis_asistencias.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}
