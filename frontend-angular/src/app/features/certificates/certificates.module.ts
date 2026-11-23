import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';
import { CertificatesRoutingModule } from './certificates-routing.module';
import { CertificatesComponent } from './certificates.component';
import { RejectCertificateDialogComponent } from './reject-certificate-dialog/reject-certificate-dialog.component';
import { TourAnchorMatMenuDirective } from 'ngx-ui-tour-md-menu';

@NgModule({
  declarations: [CertificatesComponent, RejectCertificateDialogComponent],
  imports: [TourAnchorMatMenuDirective, CommonModule, FormsModule, MaterialModule, CertificatesRoutingModule]
})
export class CertificatesModule {}
