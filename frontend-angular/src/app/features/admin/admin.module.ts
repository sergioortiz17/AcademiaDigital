import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../shared/material.module';
import { AdminRoutingModule } from './admin-routing.module';
import { UsersManagementComponent } from './users-management/users-management.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';
import { UserDetailsDialogComponent } from '../../shared/user-details-dialog/user-details-dialog.component';
import { FormsModule } from '@angular/forms';


@NgModule({
  declarations: [UsersManagementComponent,ConfirmDialogComponent, UserDetailsDialogComponent],
  imports: [CommonModule, MaterialModule, AdminRoutingModule,FormsModule]
})
export class AdminModule {}
