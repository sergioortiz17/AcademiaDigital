import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../shared/material.module';
import { CoursesRoutingModule } from './courses-routing.module';
import { CoursesComponent } from './courses.component';
import { TourAnchorMatMenuDirective } from 'ngx-ui-tour-md-menu';

@NgModule({
  declarations: [CoursesComponent],
  imports: [TourAnchorMatMenuDirective, 
    CommonModule,
    TranslateModule,
    MaterialModule,
    CoursesRoutingModule
  ]
})
export class CoursesModule {}
