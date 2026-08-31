import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../shared/material.module';
import { TeachersRoutingModule } from './teachers-routing.module';
import { TeachersComponent } from './teachers.component';

@NgModule({
  declarations: [TeachersComponent],
  imports: [
    CommonModule,
    MaterialModule,
    TeachersRoutingModule
  ]
})
export class TeachersModule {}
