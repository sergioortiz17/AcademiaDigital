import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MessagesRoutingModule } from './messages-routing.module';
import { MessagesComponent } from './messages.component';

@NgModule({
  declarations: [MessagesComponent],
  imports: [
    CommonModule,
    TranslateModule,
    MessagesRoutingModule
  ]
})
export class MessagesModule {}
