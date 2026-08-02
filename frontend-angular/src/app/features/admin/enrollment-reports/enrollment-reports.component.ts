import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { EnrollmentService } from '../../../core/services/enrollment. service';

Chart.register(...registerables);

export interface PeriodReportDto {
  genderCounts: { gender: string; count: number }[];
  courseCounts: { courseName: string; studentCount: number }[];
  dailyCounts: { date: string; studentCount: number }[];
}

@Component({
  selector: 'app-enrollment-reports',
  templateUrl: './enrollment-reports.component.html',
  styleUrls: ['./enrollment-reports.component.scss'],
  standalone: false
})
export class EnrollmentReportsComponent implements AfterViewInit, OnDestroy {
  @ViewChild('genderChart') genderCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('courseChart') courseCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('dailyChart') dailyCanvasRef!: ElementRef<HTMLCanvasElement>;

  periodId!: number;
  loading = true;
  errorMsg = '';
  report: PeriodReportDto | null = null;

  private charts: Chart[] = [];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly enrollmentService: EnrollmentService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngAfterViewInit(): void {
    this.periodId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadReport();
  }

  loadReport(): void {
    this.enrollmentService.getPeriodReport(this.periodId).subscribe({
      next: res => {
        this.report = res.data;
        this.loading = false;
        this.cdr.detectChanges();
        this.buildCharts();
      },
      error: () => {
        this.errorMsg = 'No se pudo cargar el reporte.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private buildCharts(): void {
    if (!this.report) return;
    this.charts.forEach(c => c.destroy());
    this.charts = [];

    // Pie — género
    const genderLabels = this.report.genderCounts.map(g => this.genderLabel(g.gender));
    const genderData = this.report.genderCounts.map(g => g.count);
    this.charts.push(new Chart(this.genderCanvasRef.nativeElement, {
      type: 'pie',
      data: {
        labels: genderLabels,
        datasets: [{ data: genderData, backgroundColor: ['#1565c0', '#e91e8c', '#9e9e9e', '#ff9800'] }]
      },
      options: { responsive: true, plugins: { legend: { position: 'bottom' } } }
    } as ChartConfiguration));

    // Bar — inscriptos por materia
    const courseLabels = this.report.courseCounts.map(c => c.courseName);
    const courseData = this.report.courseCounts.map(c => c.studentCount);
    this.charts.push(new Chart(this.courseCanvasRef.nativeElement, {
      type: 'bar',
      data: {
        labels: courseLabels,
        datasets: [{
          label: 'Alumnos inscriptos',
          data: courseData,
          backgroundColor: '#1976d2',
          borderRadius: 6
        }]
      },
      options: {
        responsive: true,
        indexAxis: 'y',
        plugins: { legend: { display: false } },
        scales: { x: { beginAtZero: true, ticks: { stepSize: 1 } } }
      }
    } as ChartConfiguration));

    // Bar — inscripciones por día
    const dailyLabels = this.report.dailyCounts.map(d => d.date);
    const dailyData = this.report.dailyCounts.map(d => d.studentCount);
    this.charts.push(new Chart(this.dailyCanvasRef.nativeElement, {
      type: 'bar',
      data: {
        labels: dailyLabels,
        datasets: [{
          label: 'Alumnos inscriptos',
          data: dailyData,
          backgroundColor: '#43a047',
          borderRadius: 6
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
      }
    } as ChartConfiguration));
  }

  private genderLabel(g: string): string {
    const map: Record<string, string> = {
      M: 'Masculino', F: 'Femenino', O: 'Otro',
      m: 'Masculino', f: 'Femenino', o: 'Otro',
      masculino: 'Masculino', femenino: 'Femenino', otro: 'Otro'
    };
    return map[g] ?? g;
  }

  goBack(): void {
    this.router.navigate(['/app/admin/enrollments']);
  }

  ngOnDestroy(): void {
    this.charts.forEach(c => c.destroy());
  }
}
