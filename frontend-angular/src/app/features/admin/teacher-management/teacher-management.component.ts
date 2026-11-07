import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { TeacherService, Teacher, SaveTeacherRequest } from '../../../core/services/teacher.service';
import { TeacherFormDialogComponent } from './teacher-form-dialog/teacher-form-dialog.component';

@Component({
  selector: 'app-teacher-management',
  templateUrl: './teacher-management.component.html',
  styleUrls: ['./teacher-management.component.scss'],
  standalone: false
})
export class TeacherManagementComponent implements OnInit {
  teachers: Teacher[] = [];
  searchTerm = '';
  includeInactive = false;

  isLoading = false;
  errorMsg = '';
  successMsg = '';

  displayedColumns = ['name', 'employeeNumber', 'department', 'hireDate', 'status', 'actions'];

  constructor(
    private readonly teacherService: TeacherService,
    private readonly dialog: MatDialog,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTeachers();
  }

  get filteredTeachers(): Teacher[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.teachers;
    return this.teachers.filter(t =>
      `${t.firstName} ${t.lastName}`.toLowerCase().includes(term) ||
      t.employeeNumber.toLowerCase().includes(term) ||
      (t.dni ?? '').includes(term)
    );
  }

  loadTeachers(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.teacherService.getTeachers(this.includeInactive).subscribe({
      next: (teachers) => {
        this.teachers = teachers;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'Error al cargar los docentes.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleIncludeInactive(): void {
    this.includeInactive = !this.includeInactive;
    this.loadTeachers();
  }

  viewTeacher(teacher: Teacher): void {
    this.router.navigate(['/app/admin/teachers', teacher.id]);
  }

  openCreateDialog(): void {
    this.teacherService.getTeachers(true).subscribe({
      next: existingTeachers => {
        const dialogRef = this.dialog.open(TeacherFormDialogComponent, {
          width: '600px',
          disableClose: true,
          data: {
            teacher: null,
            existingTeacherUserIds: existingTeachers.map(teacher => teacher.userId),
            existingEmployeeNumbers: existingTeachers.map(teacher => teacher.employeeNumber)
          }
        });

        dialogRef.afterClosed().subscribe((request: SaveTeacherRequest | null) => {
          if (!request) return;
          this.teacherService.createTeacher(request).subscribe({
            next: () => {
              this.loadTeachers();
              this.showSuccess('Docente creado correctamente.');
            },
            error: (err) => {
              this.errorMsg = err.error?.msg || err.error?.detail || err.error?.message || err.message || 'Error al crear el docente.';
              this.cdr.detectChanges();
            }
          });
        });
      },
      error: err => {
        this.errorMsg = err.error?.msg || err.error?.detail || err.error?.message || err.message || 'No se pudieron verificar los legajos docentes existentes.';
        this.cdr.detectChanges();
      }
    });
  }

  private showSuccess(message: string): void {
    setTimeout(() => {
      this.successMsg = message;
      this.cdr.detectChanges();
      setTimeout(() => {
        this.successMsg = '';
        this.cdr.detectChanges();
      }, 4000);
    });
  }

  deactivateTeacher(teacher: Teacher, event: Event): void {
    event.stopPropagation();
    if (!confirm(`¿Dar de baja a ${teacher.firstName} ${teacher.lastName}?`)) return;
    const reason = prompt('Motivo (opcional):') ?? undefined;

    this.teacherService.deactivateTeacher(teacher.id, reason).subscribe({
      next: () => {
        this.loadTeachers();
      },
      error: (err) => {
        this.errorMsg = err.message || 'Error al dar de baja al docente.';
        this.cdr.detectChanges();
      }
    });
  }
}
