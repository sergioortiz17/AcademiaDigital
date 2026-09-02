import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CareerService, Career } from '../../../../core/services/career.service';
import { CourseService, Course } from '../../../../core/services/course.service';
import { CommissionService, Commission } from '../../../../core/services/commission.service';
import { SaveTeachingPositionRequest, TeachingPosition } from '../../../../core/services/teaching-position.service';

export interface TeachingPositionFormDialogData {
  position: TeachingPosition | null;
}

const POSITION_TYPES = [
  { value: 'Titular', label: 'Titular' },
  { value: 'Adjunct', label: 'Adjunto' },
  { value: 'JTP', label: 'Jefe de Trabajos Prácticos' },
  { value: 'Assistant', label: 'Ayudante' }
];

@Component({
  selector: 'app-teaching-position-form-dialog',
  templateUrl: './teaching-position-form-dialog.component.html',
  styleUrls: ['./teaching-position-form-dialog.component.scss'],
  standalone: false
})
export class TeachingPositionFormDialogComponent implements OnInit {
  isEdit: boolean;
  positionTypes = POSITION_TYPES;

  careers: Career[] = [];
  courses: Course[] = [];
  commissions: Commission[] = [];

  selectedCareerId: number | null = null;
  courseId: number | null = null;
  commissionId: number | null = null;
  academicYear = new Date().getFullYear();
  semester = 1;
  positionType = 'Titular';
  maxStudents = 30;

  constructor(
    public dialogRef: MatDialogRef<TeachingPositionFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TeachingPositionFormDialogData,
    private readonly careerService: CareerService,
    private readonly courseService: CourseService,
    private readonly commissionService: CommissionService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.isEdit = !!data.position;
    if (data.position) {
      this.courseId = data.position.courseId;
      this.commissionId = data.position.commissionId;
      this.academicYear = data.position.academicYear;
      this.semester = data.position.semester;
      this.positionType = data.position.positionType;
      this.maxStudents = data.position.maxStudents;
    }
  }

  ngOnInit(): void {
    if (!this.isEdit) {
      this.careerService.getCareers().subscribe(careers => {
        this.careers = careers;
        this.cdr.detectChanges();
      });
    }
  }

  onCareerChange(): void {
    this.courseId = null;
    this.commissionId = null;
    this.courses = [];
    this.commissions = [];
    if (!this.selectedCareerId) return;

    this.courseService.getCoursesByCareer(this.selectedCareerId).subscribe(courses => {
      this.courses = courses;
      this.cdr.detectChanges();
    });
    this.commissionService.getCommissions(this.selectedCareerId).subscribe(commissions => {
      this.commissions = commissions;
      this.cdr.detectChanges();
    });
  }

  get isValid(): boolean {
    if (!this.isEdit && (!this.courseId || !this.commissionId)) return false;
    return this.academicYear >= 2000 && (this.semester === 1 || this.semester === 2) && this.maxStudents > 0;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;
    const request: SaveTeachingPositionRequest = {
      courseId: this.courseId!,
      commissionId: this.commissionId!,
      academicYear: this.academicYear,
      semester: this.semester,
      positionType: this.positionType,
      maxStudents: this.maxStudents
    };
    this.dialogRef.close(request);
  }
}
