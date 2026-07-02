import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import {
  EnrollmentService,
  EnrolledStudentDto,
  EnrollmentPeriodDto
} from '../../../core/services/enrollment. service';

@Component({
  selector: 'app-enrolled-students',
  templateUrl: './enrolled-students.component.html',
  styleUrls: ['./enrolled-students.component.scss'],
  standalone: false
})
export class EnrolledStudentsComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  periodId!: number;
  period: EnrollmentPeriodDto | null = null;
  dataSource = new MatTableDataSource<EnrolledStudentDto>([]);
  displayedColumns = ['fullName', 'dni', 'courseNames', 'actions'];

  loading = true;
  errorMsg = '';
  filterValue = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly enrollmentService: EnrollmentService
  ) {}

  ngOnInit(): void {
    this.periodId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadStudents();
  }

  loadStudents(): void {
    this.loading = true;
    this.enrollmentService.getEnrolledStudents(this.periodId).subscribe({
      next: res => {
        this.dataSource.data = res.data;
        this.dataSource.filterPredicate = (s, f) =>
          s.fullName.toLowerCase().includes(f) || s.dni.includes(f);
        setTimeout(() => {
          this.dataSource.paginator = this.paginator;
          this.dataSource.sort = this.sort;
        });
        this.loading = false;
      },
      error: () => {
        this.errorMsg = 'No se pudo cargar el listado de inscriptos.';
        this.loading = false;
      }
    });
  }

  applyFilter(value: string): void {
    this.filterValue = value;
    this.dataSource.filter = value.trim().toLowerCase();
  }

  removeStudent(student: EnrolledStudentDto): void {
    if (!confirm(`¿Dar de baja a ${student.fullName}?\nSe eliminarán todas sus inscripciones en este período.`)) return;
    this.enrollmentService.removeStudentFromPeriod(this.periodId, student.studentId).subscribe({
      next: () => {
        this.dataSource.data = this.dataSource.data.filter(s => s.studentId !== student.studentId);
      },
      error: err => alert(err.error?.msg || 'No se pudo dar de baja al estudiante.')
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/enrollments']);
  }
}
