import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminLayoutComponent } from './layouts/admin-layout/admin-layout.component';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  {
    path: 'app',
    component: AdminLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule)
      },
      {
        path: 'courses',
        loadChildren: () => import('./features/courses/courses.module').then(m => m.CoursesModule)
      },
      {
        path: 'enrollments',
        loadChildren: () => import('./features/enrollments/enrollments.module').then(m => m.EnrollmentsModule)
      },
      {
        path: 'calendar',
        loadChildren: () => import('./features/calendar/calendar.module').then(m => m.CalendarModule)
      },
      {
        path: 'teachers',
        loadChildren: () => import('./features/teachers/teachers.module').then(m => m.TeachersModule)
      },
      {
        path: 'certificates',
        loadChildren: () => import('./features/certificates/certificates.module').then(m => m.CertificatesModule)
      },
      {
        path: 'grades',
        loadChildren: () => import('./features/grades/grades.module').then(m => m.GradesModule)
      },
      {
        path: 'attendance',
        loadChildren: () => import('./features/attendance/attendance.module').then(m => m.AttendanceModule)
      },
      {
        path: 'messages',
        loadChildren: () => import('./features/messages/messages.module').then(m => m.MessagesModule)
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule)
      },
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule)
      },
      { path: '', redirectTo: 'dashboard/default', pathMatch: 'full' }
    ]
  },
  { path: '', redirectTo: 'auth/signin', pathMatch: 'full' },
  { path: '**', redirectTo: 'auth/signin' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
