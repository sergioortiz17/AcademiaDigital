import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MaterialModule } from '../../shared/material.module';
import { RoleGuard } from '../../core/guards/role.guard';
import { UserRole } from '../../store/account/account.actions';
import { AdmissionDocumentsComponent } from './admission-documents.component';

@NgModule({
  declarations: [AdmissionDocumentsComponent],
  imports: [
    CommonModule,
    MaterialModule,
    RouterModule.forChild([
      { path: '', component: AdmissionDocumentsComponent, canActivate: [RoleGuard], data: { roles: [UserRole.Alumno] } }
    ])
  ]
})
export class AdmissionDocumentsModule {}
