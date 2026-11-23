import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';
import { AcademicHistoryRoutingModule } from './academic-history-routing.module';
import { AcademicHistoryComponent } from './academic-history.component';
import { TourAnchorMatMenuDirective } from 'ngx-ui-tour-md-menu';

@NgModule({
  declarations: [AcademicHistoryComponent],
  imports: [TourAnchorMatMenuDirective, 
    CommonModule,
    FormsModule,
    MaterialModule,
    AcademicHistoryRoutingModule
  ]
})
export class AcademicHistoryModule {}
