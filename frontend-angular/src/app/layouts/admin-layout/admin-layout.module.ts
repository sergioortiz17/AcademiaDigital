import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AdminLayoutComponent } from './admin-layout.component';
import { NavbarComponent } from './navbar/navbar.component';
import { NavRightComponent } from './nav-right/nav-right.component';
import { NavigationComponent } from './navigation/navigation.component';

@NgModule({
  declarations: [
    AdminLayoutComponent,
    NavbarComponent,
    NavRightComponent,
    NavigationComponent
  ],
  imports: [
    CommonModule,
    RouterModule,
    TranslateModule
  ],
  exports: [AdminLayoutComponent]
})
export class AdminLayoutModule {}
