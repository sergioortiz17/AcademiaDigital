import { Component } from '@angular/core';

@Component({
  selector: 'app-enrollments',
  templateUrl: './enrollments.component.html',
  styleUrls: ['./enrollments.component.scss'],
  standalone: false
})
export class EnrollmentsComponent {
   showMyEnrollments = false;

  openMyEnrollments(): void {
    this.showMyEnrollments = true;
  }

  backToPlan(): void {
    this.showMyEnrollments = false;
  }

  

}
