import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { GuestGuard } from '../../core/guards/guest.guard';

const routes: Routes = [
  { path: 'signin', component: LoginComponent, canActivate: [GuestGuard] },
  { path: 'signup', component: RegisterComponent, canActivate: [GuestGuard] },
  { path: '', redirectTo: 'signin', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AuthRoutingModule {}
