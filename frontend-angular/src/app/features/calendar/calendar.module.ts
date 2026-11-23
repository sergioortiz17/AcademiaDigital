import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';
import { CalendarRoutingModule } from './calendar-routing.module';
import { CalendarComponent } from './calendar.component';
import { CreateEventDialogComponent } from './create-event-dialog/create-event-dialog.component';
import { TourAnchorMatMenuDirective } from 'ngx-ui-tour-md-menu';

@NgModule({
  declarations: [CalendarComponent, CreateEventDialogComponent],
  imports: [TourAnchorMatMenuDirective, CommonModule, FormsModule, MaterialModule, CalendarRoutingModule]
})
export class CalendarModule {}
