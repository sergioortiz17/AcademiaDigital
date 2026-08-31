import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { TeacherService, Teacher, TeacherAssignment, SaveTeacherRequest } from '../../../core/services/teacher.service';
import { TeacherFormDialogComponent } from '../teacher-management/teacher-form-dialog/teacher-form-dialog.component';
import { AssignPositionDialogComponent, AssignPositionDialogResult } from './assign-position-dialog/assign-position-dialog.component';
import { EndAssignmentDialogComponent, EndAssignmentDialogResult } from './end-assignment-dialog/end-assignment-dialog.component';

@Component({
  selector: 'app-teacher-detail',
  templateUrl: './teacher-detail.component.html',
  styleUrls: ['./teacher-detail.component.scss'],
  standalone: false
})
export class TeacherDetailComponent implements OnInit {
  teacherId!: number;
  teacher: Teacher | null = null;
  assignments: TeacherAssignment[] = [];
  includeEnded = false;

  isLoading = false;
  errorMsg = '';
  successMsg = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly teacherService: TeacherService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.teacherId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAll();
  }

  loadAll(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.teacherService.getTeacher(this.teacherId).subscribe({
      next: (teacher) => {
        this.teacher = teacher;
        this.loadAssignments();
      },
      error: (err) => {
        this.errorMsg = err.error?.msg || 'No se pudo cargar el legajo.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadAssignments(): void {
    this.teacherService.getAssignments(this.teacherId, this.includeEnded).subscribe({
      next: (assignments) => {
        this.assignments = assignments;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleIncludeEnded(): void {
    this.includeEnded = !this.includeEnded;
    this.loadAssignments();
  }

  goBack(): void {
    this.router.navigate(['/app/admin/teachers']);
  }

  editTeacher(): void {
    if (!this.teacher) return;
    const dialogRef = this.dialog.open(TeacherFormDialogComponent, {
      width: '600px',
      disableClose: true,
      data: { teacher: this.teacher }
    });

    dialogRef.afterClosed().subscribe((request: SaveTeacherRequest | null) => {
      if (!request) return;
      this.teacherService.updateTeacher(this.teacherId, request).subscribe({
        next: (updated) => {
          this.teacher = updated;
          this.successMsg = 'Legajo actualizado correctamente.';
          this.cdr.detectChanges();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.errorMsg = err.error?.msg || 'Error al actualizar el legajo.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  assignPosition(): void {
    const dialogRef = this.dialog.open(AssignPositionDialogComponent, {
      width: '480px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe((result: AssignPositionDialogResult | null) => {
      if (!result) return;
      this.teacherService.assignTeacher(this.teacherId, result.teachingPositionId, result.startedOn, result.reason).subscribe({
        next: () => {
          this.successMsg = 'Cargo asignado correctamente.';
          this.loadAssignments();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.errorMsg = err.error?.msg || 'Error al asignar el cargo.';
          this.cdr.detectChanges();
        }
      });
    });
  }

  endAssignment(assignment: TeacherAssignment): void {
    const dialogRef = this.dialog.open(EndAssignmentDialogComponent, {
      width: '480px',
      disableClose: true,
      data: { assignment }
    });

    dialogRef.afterClosed().subscribe((result: EndAssignmentDialogResult | null) => {
      if (!result) return;
      this.teacherService.endAssignment(this.teacherId, assignment.id, result.endedOn, result.reason).subscribe({
        next: () => {
          this.successMsg = 'Designación finalizada.';
          this.loadAssignments();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: (err) => {
          this.errorMsg = err.error?.msg || 'Error al finalizar la designación.';
          this.cdr.detectChanges();
        }
      });
    });
  }
}
