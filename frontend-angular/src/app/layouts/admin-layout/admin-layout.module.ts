import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../shared/material.module';

import { AdminLayoutComponent } from './admin-layout.component';
import { NavbarComponent } from './navbar/navbar.component';
import { NavRightComponent } from './nav-right/nav-right.component';
import { NavigationComponent } from './navigation/navigation.component';
import { TourAnchorMatMenuDirective, TourStepTemplateComponent } from 'ngx-ui-tour-md-menu';

@NgModule({
  declarations: [
    AdminLayoutComponent,
    NavbarComponent,
    NavRightComponent,
    NavigationComponent
  ],
  imports: [TourAnchorMatMenuDirective, TourStepTemplateComponent, 
    CommonModule,
    RouterModule,
    TranslateModule,
    MaterialModule
  ],
  exports: [AdminLayoutComponent]
})
export class AdminLayoutModule {}
