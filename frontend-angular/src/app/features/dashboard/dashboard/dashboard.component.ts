import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { selectUser, selectUserRole } from '../../../store/account/account.selectors';
import { UserModel, UserRole } from '../../../store/account/account.actions';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit {
  today: Date = new Date();
  user$: Observable<UserModel | null>;
  userRole$: Observable<UserRole | null>;
  UserRole = UserRole;

  carouselItems = [
    { title: 'Nueva convocatoria a becas', description: 'Postulate antes del 15 de noviembre para acceder a las becas académicas 2025.' },
    { title: 'Jornadas de Innovación Educativa', description: 'Participá de los talleres de tecnología aplicada a la enseñanza.' },
    { title: 'Inscripciones abiertas 2025', description: 'Ya podés registrarte para el ciclo lectivo 2025 desde el portal de alumnos.' }
  ];

  activeSlide = 0;
  private slideInterval: any;

  constructor(private readonly store: Store) {
    this.user$ = this.store.select(selectUser);
    this.userRole$ = this.store.select(selectUserRole) as Observable<UserRole | null>;
  }

  ngOnInit(): void {
    this.startCarousel();
  }

  startCarousel(): void {
    this.slideInterval = setInterval(() => {
      this.activeSlide = (this.activeSlide + 1) % this.carouselItems.length;
    }, 4000);
  }

  goToSlide(index: number): void {
    this.activeSlide = index;
    clearInterval(this.slideInterval);
    this.startCarousel();
  }
}
