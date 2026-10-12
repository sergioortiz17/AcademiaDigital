import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CareerService, Career } from '../../../core/services/career.service';
import { CommissionService, Commission } from '../../../core/services/commission.service';
import {
  StudentService,
  StudentListItem,
  Student
} from '../../../core/services/student.service';

@Component({
  selector: 'app-student-commission-assignment',
  templateUrl: './student-commission-assignment.component.html',
  styleUrls: ['./student-commission-assignment.component.scss'],
  standalone: false
})
export class StudentCommissionAssignmentComponent implements OnInit, OnDestroy {
  careers: Career[] = [];
  students: StudentListItem[] = [];
  filteredStudents: StudentListItem[] = [];
  commissions: Commission[] = [];

  studentFilterCtrl: FormControl = new FormControl('');

  selectedCareerId: number | null = null;
  selectedStudentId: number | null = null;
  selectedCommissionId: number | null = null;
  reason = '';

  selectedStudent: Student | null = null;   // ficha completa (para currentStudyPlanId)
  isLoading = false;
  isSubmitting = false;
  errorMsg = '';
  successMsg = '';

  private readonly destroy$ = new Subject<void>();

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

    this.studentFilterCtrl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.filterStudents();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get selectedCommission(): Commission | undefined {
    return this.commissions.find(c => c.id === this.selectedCommissionId);
  }

  onCareerChange(): void {
    this.students = [];
    this.filteredStudents = [];
    this.commissions = [];
    this.selectedStudentId = null;
    this.selectedCommissionId = null;
    this.selectedStudent = null;
    this.studentFilterCtrl.setValue('', { emitEvent: false });
    this.clearMessages();
    if (!this.selectedCareerId) return;

    this.isLoading = true;
    this.studentService.searchStudents(undefined, this.selectedCareerId, 1, 100).subscribe({
      next: (page) => { this.students = page.items; this.filteredStudents = [...this.students]; this.isLoading = false; this.cdr.detectChanges(); },
      error: (err) => this.fail(err)
    });
    this.commissionService.getCommissions(this.selectedCareerId).subscribe({
      next: (commissions) => { this.commissions = commissions.filter(c => c.isActive); this.cdr.detectChanges(); },
      error: (err) => this.fail(err)
    });
  }

  private filterStudents(): void {
    if (!this.students) return;

    let search = this.studentFilterCtrl.value;
    if (!search) {
      this.filteredStudents = [...this.students];
      this.cdr.detectChanges();
      return;
    } else {
      search = search.toLowerCase();
    }

    // Filtra coincidencia por Nombre, Legajo o DNI
    this.filteredStudents = this.students.filter(s =>
      (s.fullName && s.fullName.toLowerCase().includes(search)) ||
      (s.legajoNumber && s.legajoNumber.toString().toLowerCase().includes(search)) ||
      (s.dni && s.dni.toString().toLowerCase().includes(search))
    );
    this.cdr.detectChanges();
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
      this.errorMsg = 'El alumno no tiene un plan de estudios actual asignado. No se puede vincular la división.';
      return;
    }

    this.isSubmitting = true;
    this.studentService.assignAcademic(student.id, {
      careerId: commission.careerId,
      studyPlanId: student.currentStudyPlanId,   // derivado del plan actual del alumno
      divisionId: commission.id,
      academicYear: commission.academicYear,      // derivado de la comisión
      yearNumber: commission.yearNumber,          // derivado de la comisión
      reason: this.reason?.trim() || null
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successMsg = `División "${commission.code}" asignada al alumno correctamente ` +
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
