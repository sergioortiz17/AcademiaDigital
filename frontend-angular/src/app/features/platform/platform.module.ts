import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MaterialModule } from '../../shared/material.module';
import { PlatformTenantsComponent } from './platform-tenants.component';

@NgModule({
  declarations: [PlatformTenantsComponent],
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule,
    RouterModule.forChild([{ path: '', component: PlatformTenantsComponent }])
  ]
})
export class PlatformModule {}
