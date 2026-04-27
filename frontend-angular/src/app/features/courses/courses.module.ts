import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { CoursesRoutingModule } from './courses-routing.module';
import { CoursesComponent } from './courses.component';

@NgModule({
  declarations: [CoursesComponent],
  imports: [
    CommonModule,
    TranslateModule,
    CoursesRoutingModule
  ]
})
export class CoursesModule {}
