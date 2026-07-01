import { Component } from '@angular/core';

@Component({
  selector: 'app-enrollments',
  templateUrl: './enrollments.component.html',
  styleUrls: ['./enrollments.component.scss'],
  standalone: false
})
export class EnrollmentsComponent {
   showMyEnrollments = false;

  myEnrollments = [
    { title: 'Inscripción a Carrera Desarrollo de Software – 1er y 2do año' },
    { title: 'Inscripción a Carrera Enfermería – 1er año' }
  ];

  openMyEnrollments(): void {
    this.showMyEnrollments = true;
  }

  backToPlan(): void {
    this.showMyEnrollments = false;
  }

  

}
