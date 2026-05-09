import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../shared/material.module';
import { EnrollmentsRoutingModule } from './enrollments-routing.module';
import { EnrollmentsComponent } from './enrollments.component';

@NgModule({
  declarations: [EnrollmentsComponent],
  imports: [
    CommonModule,
    TranslateModule,
    MaterialModule,
    EnrollmentsRoutingModule
  ]
})
export class EnrollmentsModule {}
