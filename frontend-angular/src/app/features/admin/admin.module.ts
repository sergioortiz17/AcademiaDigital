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
import { SumCountPipe } from '../../shared/pipes/sum-count.pipe';
import { AttendanceManagementComponent } from './attendance-management/attendance-management.component';
import { NewSessionDialogComponent } from './attendance-management/new-session-dialog/new-session-dialog.component';
import { ReopenSessionDialogComponent } from './attendance-management/reopen-session-dialog/reopen-session-dialog.component';
import { JustifyAttendanceDialogComponent } from './attendance-management/justify-attendance-dialog/justify-attendance-dialog.component';
import { AttendanceSessionDetailComponent } from './attendance-session-detail/attendance-session-detail.component';

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
    NewSessionDialogComponent,
    ReopenSessionDialogComponent,
    JustifyAttendanceDialogComponent,
    AttendanceSessionDetailComponent
  ],
  imports: [CommonModule, FormsModule, MaterialModule, AdminRoutingModule]
})
export class AdminModule {}
