import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { selectUser, selectUserRole } from '../../../store/account/account.selectors';
import { UserModel, UserRole } from '../../../store/account/account.actions';
import { CalendarService, CalendarEvent } from '../../../core/services/calendar.service';

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
    { title: 'Nueva convocatoria a becas', description: 'Postulate antes del 15 de noviembre para acceder a las becas academicas 2025.' },
    { title: 'Jornadas de Innovacion Educativa', description: 'Participa de los talleres de tecnologia aplicada a la ensenanza.' },
    { title: 'Inscripciones abiertas 2025', description: 'Ya podes registrarte para el ciclo lectivo 2025 desde el portal de alumnos.' }
  ];

  activeSlide = 0;
  private slideInterval: any;

  upcomingEvents: CalendarEvent[] = [];
  loadingEvents = false;

  constructor(
    private readonly store: Store,
    private readonly calendarService: CalendarService
  ) {
    this.user$ = this.store.select(selectUser);
    this.userRole$ = this.store.select(selectUserRole) as Observable<UserRole | null>;
  }

  ngOnInit(): void {
    this.startCarousel();
    this.loadUpcomingEvents();
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

  eventTypeClass(type: string): string {
    const map: Record<string, string> = {
      Examen: 'type-examen', EntregaTP: 'type-entrega', Clase: 'type-clase', Feriado: 'type-feriado', Otro: 'type-otro'
    };
    return map[type] ?? 'type-otro';
  }

  eventTypeLabel(type: string): string {
    const map: Record<string, string> = {
      Examen: 'Examen', EntregaTP: 'Entrega TP', Clase: 'Clase', Feriado: 'Feriado', Otro: 'Evento'
    };
    return map[type] ?? type;
  }

  formatEventDate(dateStr: string): string {
    const [y, m, d] = dateStr.split('-').map(Number);
    const date = new Date(y, m - 1, d);
    const dayNames = ['Dom', 'Lun', 'Mar', 'Mie', 'Jue', 'Vie', 'Sab'];
    return `${dayNames[date.getDay()]} ${d}/${m}`;
  }

  private loadUpcomingEvents(): void {
    this.loadingEvents = true;
    this.calendarService.getUpcoming().subscribe({
      next: res => {
        this.upcomingEvents = res.data;
        this.loadingEvents = false;
      },
      error: () => this.loadingEvents = false
    });
  }
}
