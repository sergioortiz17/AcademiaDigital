import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CareerService, Career } from '../../../core/services/career.service';
import { CommissionService, Commission } from '../../../core/services/commission.service';
import {
  StudentService,
  StudentListItem,
  Student
} from '../../../core/services/student.service';

/**
 * Pantalla admin "Asignar comisión a alumno".
 *
 * El admin elige un alumno (de una carrera) y una comisión de esa misma carrera. El
 * StudyPlanId, AcademicYear y YearNumber NO se piden: se derivan de la comisión elegida
 * (que ya trae academicYear/yearNumber) + el plan de estudios actual del alumno. Así el
 * admin no tiene que conocer esos IDs internos. El backend
 * (POST /api/v1/students/{id}/academic-assignments) valida que la comisión sea de la misma
 * carrera y ciclo, y deduplica las asignaciones vigentes.
 */
@Component({
  selector: 'app-student-commission-assignment',
  templateUrl: './student-commission-assignment.component.html',
  styleUrls: ['./student-commission-assignment.component.scss'],
  standalone: false
})
export class StudentCommissionAssignmentComponent implements OnInit {
  careers: Career[] = [];
  students: StudentListItem[] = [];
  commissions: Commission[] = [];

  selectedCareerId: number | null = null;
  selectedStudentId: number | null = null;
  selectedCommissionId: number | null = null;
  reason = '';

  selectedStudent: Student | null = null;   // ficha completa (para currentStudyPlanId)
  isLoading = false;
  isSubmitting = false;
  errorMsg = '';
  successMsg = '';

  constructor(
    private readonly careerService: CareerService,
    private readonly commissionService: CommissionService,
    private readonly studentService: StudentService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe({
      next: (careers) => { this.careers = careers; this.cdr.detectChanges(); },
      error: (err) => this.fail(err)
    });
  }

  get selectedCommission(): Commission | undefined {
    return this.commissions.find(c => c.id === this.selectedCommissionId);
  }

  onCareerChange(): void {
    this.students = [];
    this.commissions = [];
    this.selectedStudentId = null;
    this.selectedCommissionId = null;
    this.selectedStudent = null;
    this.clearMessages();
    if (!this.selectedCareerId) return;

    this.isLoading = true;
    this.studentService.searchStudents(undefined, this.selectedCareerId, 1, 200).subscribe({
      next: (page) => { this.students = page.items; this.isLoading = false; this.cdr.detectChanges(); },
      error: (err) => this.fail(err)
    });
    this.commissionService.getCommissions(this.selectedCareerId).subscribe({
      next: (commissions) => { this.commissions = commissions.filter(c => c.isActive); this.cdr.detectChanges(); },
      error: (err) => this.fail(err)
    });
  }

  onStudentChange(): void {
    this.selectedStudent = null;
    this.clearMessages();
    if (!this.selectedStudentId) return;
    // Ficha completa para derivar el plan actual (currentStudyPlanId).
    this.studentService.getStudent(this.selectedStudentId).subscribe({
      next: (student) => { this.selectedStudent = student; this.cdr.detectChanges(); },
      error: (err) => this.fail(err)
    });
  }

  canSubmit(): boolean {
    return !!this.selectedStudentId && !!this.selectedCommissionId
      && !!this.selectedStudent?.currentStudyPlanId && !this.isSubmitting;
  }

  submit(): void {
    this.clearMessages();
    const student = this.selectedStudent;
    const commission = this.selectedCommission;
    if (!student || !commission) return;
    if (!student.currentStudyPlanId) {
      this.errorMsg = 'El alumno no tiene un plan de estudios actual asignado. No se puede vincular la comisión.';
      return;
    }

    this.isSubmitting = true;
    this.studentService.assignAcademic(student.id, {
      careerId: commission.careerId,
      studyPlanId: student.currentStudyPlanId,   // derivado del plan actual del alumno
      commissionId: commission.id,
      academicYear: commission.academicYear,      // derivado de la comisión
      yearNumber: commission.yearNumber,          // derivado de la comisión
      reason: this.reason?.trim() || null
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successMsg = `Comisión "${commission.code}" asignada al alumno correctamente ` +
          `(año ${commission.academicYear}, ${commission.yearNumber}° año del plan).`;
        this.reason = '';
        this.cdr.detectChanges();
      },
      error: (err) => { this.isSubmitting = false; this.fail(err); }
    });
  }

  private clearMessages(): void { this.errorMsg = ''; this.successMsg = ''; }

  private fail(err: any): void {
    this.isLoading = false;
    this.errorMsg = err?.error?.msg || err?.message || 'Ocurrió un error.';
    this.cdr.detectChanges();
  }
}
