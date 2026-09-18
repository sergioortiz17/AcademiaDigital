import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { TeacherService, TeacherAssignment } from '../../core/services/teacher.service';

@Component({
  selector: 'app-teachers',
  templateUrl: './teachers.component.html',
  styleUrls: ['./teachers.component.scss'],
  standalone: false
})
export class TeachersComponent implements OnInit {
  assignments: TeacherAssignment[] = [];
  includeEnded = false;
  isLoading = false;
  errorMsg = '';

  constructor(
    private readonly teacherService: TeacherService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAssignments();
  }

  loadAssignments(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.teacherService.getMyAssignments(this.includeEnded).subscribe({
      next: (assignments) => {
        this.assignments = assignments;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMsg = err.error?.message || 'No se pudieron cargar tus asignaciones.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleIncludeEnded(): void {
    this.includeEnded = !this.includeEnded;
    this.loadAssignments();
  }
}
