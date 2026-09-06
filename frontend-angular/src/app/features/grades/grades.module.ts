import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';
import { GradesRoutingModule } from './grades-routing.module';
import { GradesComponent } from './grades.component';
import { EvaluationSetupDialogComponent } from './evaluation-setup-dialog/evaluation-setup-dialog.component';

@NgModule({
  declarations: [GradesComponent, EvaluationSetupDialogComponent],
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule,
    GradesRoutingModule
  ]
})
export class GradesModule {}
