import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AcademicHistoryComponent } from './academic-history.component';
import { RoleGuard } from '../../core/guards/role.guard';
import { UserRole } from '../../store/account/account.actions';

const routes: Routes = [
  {
    path: '',
    component: AcademicHistoryComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Alumno] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AcademicHistoryRoutingModule {}
