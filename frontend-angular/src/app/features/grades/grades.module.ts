import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { GradesRoutingModule } from './grades-routing.module';
import { GradesComponent } from './grades.component';

@NgModule({
  declarations: [GradesComponent],
  imports: [
    CommonModule,
    TranslateModule,
    GradesRoutingModule
  ]
})
export class GradesModule {}
