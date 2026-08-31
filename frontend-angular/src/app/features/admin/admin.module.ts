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
import { StudyPlanImportComponent } from './study-plan-import/study-plan-import.component';
import { CareerCreateComponent } from './career-create/career-create.component';
import { StudyPlanDiffComponent } from './study-plan-diff/study-plan-diff.component';
import { CareerManagementComponent } from './career-management/career-management.component';
import { SumCountPipe } from '../../shared/pipes/sum-count.pipe';

@NgModule({
  declarations: [
    UsersManagementComponent,
    ConfirmDialogComponent,
    UserDetailsDialogComponent,
    EnrollmentManagementComponent,
    EnrolledStudentsComponent,
    EnrollmentReportsComponent,
    StudyPlanImportComponent,
    CareerCreateComponent,
    StudyPlanDiffComponent,
    CareerManagementComponent,
    SumCountPipe
  ],
  imports: [CommonModule, FormsModule, MaterialModule, AdminRoutingModule]
})
export class AdminModule {}
