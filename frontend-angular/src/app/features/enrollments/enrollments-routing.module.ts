import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { EnrollmentsComponent } from './enrollments.component';

const routes: Routes = [
  { path: 'exam', loadComponent: () => import('./exam-enrollment/exam-enrollment.component').then(m => m.ExamEnrollmentComponent) },
  { path: '', component: EnrollmentsComponent, pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EnrollmentsRoutingModule {}
