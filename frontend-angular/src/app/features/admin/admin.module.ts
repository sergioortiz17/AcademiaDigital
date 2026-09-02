import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';
import { AdminRoutingModule } from './admin-routing.module';
import { UsersManagementComponent } from './users-management/users-management.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';
import { UserDetailsDialogComponent } from '../../shared/user-details-dialog/user-details-dialog.component';
import { EnrollmentManagementComponent } from './enrollment-management/enrollment-management.component';
import { EnrolledStudentsComponent } from './enrolled-students/enrolled-students.component';
import { EnrollmentReportsComponent } from './enrollment-reports/enrollment-reports.component';
import { AttendanceManagementComponent } from './attendance-management/attendance-management.component';
import { ReopenSessionDialogComponent } from './attendance-management/reopen-session-dialog/reopen-session-dialog.component';
import { JustifyAttendanceDialogComponent } from './attendance-management/justify-attendance-dialog/justify-attendance-dialog.component';
import { TeacherManagementComponent } from './teacher-management/teacher-management.component';
import { TeacherFormDialogComponent } from './teacher-management/teacher-form-dialog/teacher-form-dialog.component';
import { TeacherDetailComponent } from './teacher-detail/teacher-detail.component';
import { AssignPositionDialogComponent } from './teacher-detail/assign-position-dialog/assign-position-dialog.component';
import { EndAssignmentDialogComponent } from './teacher-detail/end-assignment-dialog/end-assignment-dialog.component';
import { TeachingPositionManagementComponent } from './teaching-position-management/teaching-position-management.component';
import { TeachingPositionFormDialogComponent } from './teaching-position-management/teaching-position-form-dialog/teaching-position-form-dialog.component';
import { GradebookManagementComponent } from './gradebook-management/gradebook-management.component';
import { ReopenGradebookDialogComponent } from './gradebook-management/reopen-gradebook-dialog/reopen-gradebook-dialog.component';
import { SumCountPipe } from '../../shared/pipes/sum-count.pipe';

@NgModule({
  declarations: [
    UsersManagementComponent,
    ConfirmDialogComponent,
    UserDetailsDialogComponent,
    EnrollmentManagementComponent,
    EnrolledStudentsComponent,
    EnrollmentReportsComponent,
    SumCountPipe,
    AttendanceManagementComponent,
    ReopenSessionDialogComponent,
    JustifyAttendanceDialogComponent,
    TeacherManagementComponent,
    TeacherFormDialogComponent,
    TeacherDetailComponent,
    AssignPositionDialogComponent,
    EndAssignmentDialogComponent,
    TeachingPositionManagementComponent,
    TeachingPositionFormDialogComponent,
    GradebookManagementComponent,
    ReopenGradebookDialogComponent
  ],
  imports: [CommonModule, FormsModule, MaterialModule, AdminRoutingModule]
})
export class AdminModule {}
