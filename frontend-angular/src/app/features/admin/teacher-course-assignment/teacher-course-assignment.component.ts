import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Teacher, TeacherAssignment, TeacherService } from '../../../core/services/teacher.service';
import { AssignCoursesOutcome } from '../assign-courses-form/assign-courses-form.component';

const POSITION_LABELS: Record<string, string> = {
  Titular: 'Titular', Adjunct: 'Adjunto', JTP: 'JTP', Assistant: 'Ayudante'
};

/**
 * Docentes → Asignar materia: se elige el docente, se ven sus materias actuales y se le asignan
 * materias nuevas con el mismo formulario del legajo.
 */
@Component({
  selector: 'app-teacher-course-assignment',
  templateUrl: './teacher-course-assignment.component.html',
  styleUrls: ['./teacher-course-assignment.component.scss'],
  standalone: false
})
export class TeacherCourseAssignmentComponent implements OnInit {
  teachers: Teacher[] = [];
  teacherSearch = '';
  teacherId: number | null = null;
  assignments: TeacherAssignment[] = [];
  isLoadingAssignments = false;
  errorMsg = '';

  constructor(
    private readonly teacherService: TeacherService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.teacherService.getTeachers().subscribe({
      next: teachers => {
        this.teachers = teachers
          .filter(t => t.isActive)
          .sort((a, b) => `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`));
        this.cdr.detectChanges();
      },
      error: err => { this.errorMsg = err?.message || 'No se pudieron cargar los docentes.'; this.cdr.detectChanges(); }
    });
  }

  /** Docentes que coinciden con la búsqueda (nombre, apellido, legajo o email). */
  get filteredTeachers(): Teacher[] {
    const term = this.teacherSearch.trim().toLowerCase();
    if (!term) return this.teachers;
    return this.teachers.filter(t =>
      `${t.lastName} ${t.firstName} ${t.employeeNumber} ${t.email}`.toLowerCase().includes(term));
  }

  get selectedTeacher(): Teacher | undefined {
    return this.teachers.find(t => t.id === this.teacherId);
  }

  onTeacherChange(): void {
    this.loadAssignments();
  }

  onSubmitted(outcome: AssignCoursesOutcome): void {
    if (outcome.assigned) this.loadAssignments();
  }

  positionLabel(type: string): string {
    return POSITION_LABELS[type] ?? type;
  }

  private loadAssignments(): void {
    if (!this.teacherId) { this.assignments = []; return; }
    this.isLoadingAssignments = true;
    this.teacherService.getAssignments(this.teacherId).subscribe({
      next: assignments => {
        this.assignments = assignments.filter(a => a.isCurrent);
        this.isLoadingAssignments = false;
        this.cdr.detectChanges();
      },
      error: () => { this.isLoadingAssignments = false; this.cdr.detectChanges(); }
    });
  }
}
