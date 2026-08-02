import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../shared/material.module';
import { CalendarRoutingModule } from './calendar-routing.module';
import { CalendarComponent } from './calendar.component';

@NgModule({
  declarations: [CalendarComponent],
  imports: [CommonModule, MaterialModule, CalendarRoutingModule]
})
export class CalendarModule {}
