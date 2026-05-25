import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { GuestGuard } from '../../core/guards/guest.guard';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { CompleteProfileComponent } from './complete-profile/complete-profile.component';

const routes: Routes = [
  { path: 'signin', component: LoginComponent, canActivate: [GuestGuard] },
  { path: 'signup', component: RegisterComponent, canActivate: [GuestGuard] },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: '', redirectTo: 'signin', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AuthRoutingModule {}
