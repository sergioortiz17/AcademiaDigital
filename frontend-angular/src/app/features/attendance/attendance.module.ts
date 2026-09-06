import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';
import { AttendanceRoutingModule } from './attendance-routing.module';
import { AttendanceComponent } from './attendance.component';
import { NewSessionDialogComponent } from './new-session-dialog/new-session-dialog.component';
import { MyAttendanceComponent } from './my-attendance/my-attendance.component';

@NgModule({
  declarations: [AttendanceComponent, NewSessionDialogComponent, MyAttendanceComponent],
  imports: [CommonModule, FormsModule, MaterialModule, AttendanceRoutingModule]
})
export class AttendanceModule {}
