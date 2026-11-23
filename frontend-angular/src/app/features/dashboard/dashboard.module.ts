import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../shared/material.module';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard/dashboard.component';
import { TourAnchorMatMenuDirective } from 'ngx-ui-tour-md-menu';
@NgModule({
  declarations: [DashboardComponent],
  imports: [TourAnchorMatMenuDirective, 
    CommonModule,
    RouterModule,
    TranslateModule,
    MaterialModule,
    DashboardRoutingModule
  ]
})
export class DashboardModule {}
