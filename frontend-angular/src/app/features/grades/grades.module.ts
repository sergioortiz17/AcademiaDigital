import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';
import { GradesRoutingModule } from './grades-routing.module';
import { GradesComponent } from './grades.component';
import { EvaluationSetupDialogComponent } from './evaluation-setup-dialog/evaluation-setup-dialog.component';
import { TourAnchorMatMenuDirective } from 'ngx-ui-tour-md-menu';

@NgModule({
  declarations: [GradesComponent, EvaluationSetupDialogComponent],
  imports: [TourAnchorMatMenuDirective, 
    CommonModule,
    FormsModule,
    MaterialModule,
    GradesRoutingModule
  ]
})
export class GradesModule {}
