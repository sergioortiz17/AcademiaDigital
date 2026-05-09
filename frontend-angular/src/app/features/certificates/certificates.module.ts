import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../shared/material.module';
import { CertificatesRoutingModule } from './certificates-routing.module';
import { CertificatesComponent } from './certificates.component';

@NgModule({
  declarations: [CertificatesComponent],
  imports: [
    CommonModule,
    TranslateModule,
    MaterialModule,
    CertificatesRoutingModule
  ]
})
export class CertificatesModule {}
