import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TeachersComponent } from './teachers.component';
import { RoleGuard } from '../../core/guards/role.guard';
import { UserRole } from '../../store/account/account.actions';

const routes: Routes = [
  {
    path: '',
    component: TeachersComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Profesor] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TeachersRoutingModule {}
