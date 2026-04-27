import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { TeachersRoutingModule } from './teachers-routing.module';
import { TeachersComponent } from './teachers.component';

@NgModule({
  declarations: [TeachersComponent],
  imports: [
    CommonModule,
    TranslateModule,
    TeachersRoutingModule
  ]
})
export class TeachersModule {}
