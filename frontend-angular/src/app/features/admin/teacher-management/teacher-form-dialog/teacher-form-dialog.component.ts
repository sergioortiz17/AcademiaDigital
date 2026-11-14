import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { AdminService, UserSummary } from '../../../../core/services/admin.service';
import { UserRole } from '../../../../store/account/account.actions';
import { SaveTeacherRequest, Teacher } from '../../../../core/services/teacher.service';

export interface TeacherFormDialogData {
  teacher: Teacher | null;
  existingTeacherUserIds?: number[];
  existingEmployeeNumbers?: string[];
}

@Component({
  selector: 'app-teacher-form-dialog',
  templateUrl: './teacher-form-dialog.component.html',
  styleUrls: ['./teacher-form-dialog.component.scss'],
  standalone: false
})
export class TeacherFormDialogComponent implements OnInit {
  isEdit: boolean;

  userSearchTerm = '';
  userResults: UserSummary[] = [];
  selectedUser: UserSummary | null = null;
  private readonly userSearch$ = new Subject<string>();

  employeeNumber = '';
  department = '';
  specializationArea = '';
  hireDate: Date = new Date();
  phoneNumber = '';
  addressLine = '';
  city = '';
  province = '';
  postalCode = '';
  emergencyContactName = '';
  emergencyContactRelationship = '';
  emergencyContactPhone = '';

  constructor(
    public dialogRef: MatDialogRef<TeacherFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TeacherFormDialogData,
    private readonly adminService: AdminService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.isEdit = !!data.teacher;
    if (data.teacher) {
      this.employeeNumber = data.teacher.employeeNumber;
      this.department = data.teacher.department ?? '';
      this.specializationArea = data.teacher.specializationArea ?? '';
      this.hireDate = new Date(data.teacher.hireDate);
      this.phoneNumber = data.teacher.phoneNumber ?? '';
      this.addressLine = data.teacher.addressLine ?? '';
      this.city = data.teacher.city ?? '';
      this.province = data.teacher.province ?? '';
      this.postalCode = data.teacher.postalCode ?? '';
      this.emergencyContactName = data.teacher.emergencyContactName ?? '';
      this.emergencyContactRelationship = data.teacher.emergencyContactRelationship ?? '';
      this.emergencyContactPhone = data.teacher.emergencyContactPhone ?? '';
    }
  }

  get eligibleUserResults(): UserSummary[] {
    const existingTeacherUserIds = new Set(this.data.existingTeacherUserIds ?? []);
    return this.userResults.filter(user => user.isActive && !existingTeacherUserIds.has(user.id));
  }

  get employeeNumberExists(): boolean {
    const employeeNumber = this.employeeNumber.trim().toUpperCase();
    return employeeNumber.length > 0 &&
      (this.data.existingEmployeeNumbers ?? []).some(existing =>
        existing.trim().toUpperCase() === employeeNumber);
  }

  ngOnInit(): void {
    this.userSearch$.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      switchMap(term => this.adminService.getUsers(term, UserRole.Profesor, 1, 10))
    ).subscribe(res => {
      this.userResults = res.users;
      this.cdr.detectChanges();
    });
  }

  onUserSearchChange(value: string): void {
    this.userSearchTerm = value;
    this.selectedUser = null;
    this.userResults = [];
    this.userSearch$.next(value);
  }

  selectUser(user: UserSummary): void {
    this.selectedUser = user;
    this.userResults = [];
    this.userSearchTerm = `${user.username} — ${user.email}`;
  }

  get isValid(): boolean {
    if (!this.isEdit && !this.selectedUser) return false;
    return this.employeeNumber.trim().length > 0 && !this.employeeNumberExists && !!this.hireDate;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid) return;

    const request: SaveTeacherRequest = {
      employeeNumber: this.employeeNumber.trim(),
      department: this.department.trim() || null,
      specializationArea: this.specializationArea.trim() || null,
      hireDate: this.toIsoDate(this.hireDate),
      phoneNumber: this.phoneNumber.trim() || null,
      addressLine: this.addressLine.trim() || null,
      city: this.city.trim() || null,
      province: this.province.trim() || null,
      postalCode: this.postalCode.trim() || null,
      emergencyContactName: this.emergencyContactName.trim() || null,
      emergencyContactRelationship: this.emergencyContactRelationship.trim() || null,
      emergencyContactPhone: this.emergencyContactPhone.trim() || null
    };

    if (!this.isEdit) {
      request.userId = this.selectedUser!.id;
    }

    this.dialogRef.close(request);
  }

  private toIsoDate(date: Date): string {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())).toISOString();
  }
}
