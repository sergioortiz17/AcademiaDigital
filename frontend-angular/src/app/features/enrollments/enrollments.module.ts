import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { EnrollmentsRoutingModule } from './enrollments-routing.module';
import { EnrollmentsComponent } from './enrollments.component';

@NgModule({
  declarations: [EnrollmentsComponent],
  imports: [
    CommonModule,
    TranslateModule,
    EnrollmentsRoutingModule
  ]
})
export class EnrollmentsModule {}
