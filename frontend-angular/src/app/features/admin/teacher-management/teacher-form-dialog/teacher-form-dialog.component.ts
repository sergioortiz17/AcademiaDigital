import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { AdminService, UserSummary } from '../../../../core/services/admin.service';
import { UserRole } from '../../../../store/account/account.actions';
import { SaveTeacherRequest, Teacher } from '../../../../core/services/teacher.service';

export interface TeacherFormDialogData {
  teacher: Teacher | null;
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
    this.userSearch$.next(value);
  }

  selectUser(user: UserSummary): void {
    this.selectedUser = user;
    this.userResults = [];
    this.userSearchTerm = `${user.username} — ${user.email}`;
  }

  get isValid(): boolean {
    if (!this.isEdit && !this.selectedUser) return false;
    return this.employeeNumber.trim().length > 0 && !!this.hireDate;
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
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
