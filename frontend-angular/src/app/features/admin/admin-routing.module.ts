import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UsersManagementComponent } from './users-management/users-management.component';
import { EnrollmentManagementComponent } from './enrollment-management/enrollment-management.component';
import { EnrolledStudentsComponent } from './enrolled-students/enrolled-students.component';
import { EnrollmentReportsComponent } from './enrollment-reports/enrollment-reports.component';
import { AttendanceManagementComponent } from './attendance-management/attendance-management.component';
import { TeacherManagementComponent } from './teacher-management/teacher-management.component';
import { TeacherDetailComponent } from './teacher-detail/teacher-detail.component';
import { TeachingPositionManagementComponent } from './teaching-position-management/teaching-position-management.component';
import { GradebookManagementComponent } from './gradebook-management/gradebook-management.component';
import { CareerManagementComponent } from './career-management/career-management.component';
import { RoleGuard } from '../../core/guards/role.guard';
import { UserRole } from '../../store/account/account.actions';

const routes: Routes = [
  {
    path: 'users',
    component: UsersManagementComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'gradebooks',
    component: GradebookManagementComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'teachers',
    component: TeacherManagementComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'teachers/:id',
    component: TeacherDetailComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'teaching-positions',
    component: TeachingPositionManagementComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'attendance',
    component: AttendanceManagementComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'careers',
    component: CareerManagementComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'enrollments',
    component: EnrollmentManagementComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'enrollments/:id/students',
    component: EnrolledStudentsComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  {
    path: 'enrollments/:id/reports',
    component: EnrollmentReportsComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Admin] }
  },
  { path: '', redirectTo: 'users', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
