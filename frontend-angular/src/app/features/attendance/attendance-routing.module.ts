import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AttendanceComponent } from './attendance.component';
import { RoleGuard } from '../../core/guards/role.guard';
import { UserRole } from '../../store/account/account.actions';

const routes: Routes = [
  {
    path: '',
    component: AttendanceComponent,
    canActivate: [RoleGuard],
    data: { roles: [UserRole.Profesor] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AttendanceRoutingModule {}
