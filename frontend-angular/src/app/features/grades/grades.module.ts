import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../shared/material.module';
import { GradesRoutingModule } from './grades-routing.module';
import { GradesComponent } from './grades.component';

@NgModule({
  declarations: [GradesComponent],
  imports: [
    CommonModule,
    TranslateModule,
    MaterialModule,
    GradesRoutingModule
  ]
})
export class GradesModule {}
