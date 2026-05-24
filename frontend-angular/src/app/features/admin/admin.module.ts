import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../shared/material.module';
import { AdminRoutingModule } from './admin-routing.module';
import { UsersManagementComponent } from './users-management/users-management.component';

@NgModule({
  declarations: [UsersManagementComponent],
  imports: [CommonModule, MaterialModule, AdminRoutingModule]
})
export class AdminModule {}
