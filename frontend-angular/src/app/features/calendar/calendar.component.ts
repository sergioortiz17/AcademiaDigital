import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CalendarService, CalendarEvent } from '../../core/services/calendar.service';

interface CalendarDay {
  date: Date | null;
  dayNumber: number | null;
  isToday: boolean;
  isCurrentMonth: boolean;
  events: CalendarEvent[];
}

@Component({
  selector: 'app-calendar',
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.scss'],
  standalone: false
})
export class CalendarComponent implements OnInit {
  weekDays = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];
  monthNames = [
    'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
    'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
  ];

  currentYear!: number;
  currentMonth!: number;
  weeks: CalendarDay[][] = [];
  selectedDay: CalendarDay | null = null;
  loading = false;
  error = '';

  private today = new Date();

  constructor(
    private readonly calendarService: CalendarService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.currentYear = this.today.getFullYear();
    this.currentMonth = this.today.getMonth() + 1;
    this.loadMonth();
  }

  get monthLabel(): string {
    return `${String(this.currentMonth).padStart(2, '0')} ${this.monthNames[this.currentMonth - 1]} ${this.currentYear}`;
  }

  prevMonth(): void {
    if (this.currentMonth === 1) { this.currentMonth = 12; this.currentYear--; }
    else this.currentMonth--;
    this.selectedDay = null;
    this.loadMonth();
  }

  nextMonth(): void {
    if (this.currentMonth === 12) { this.currentMonth = 1; this.currentYear++; }
    else this.currentMonth++;
    this.selectedDay = null;
    this.loadMonth();
  }

  selectDay(day: CalendarDay): void {
    if (!day.date || !day.isCurrentMonth) return;
    this.selectedDay = this.selectedDay?.date?.getTime() === day.date.getTime() ? null : day;
  }

  closeDetail(): void {
    this.selectedDay = null;
  }

  eventTypeLabel(type: string): string {
    const map: Record<string, string> = {
      Examen: 'Examen', EntregaTP: 'Entrega TP', Clase: 'Clase', Otro: 'Evento'
    };
    return map[type] ?? type;
  }

  eventTypeClass(type: string): string {
    const map: Record<string, string> = {
      Examen: 'type-examen', EntregaTP: 'type-entrega', Clase: 'type-clase', Otro: 'type-otro'
    };
    return map[type] ?? 'type-otro';
  }

  formatTime(t: string | null): string {
    return t ? t + ' hs' : '';
  }

  private loadMonth(): void {
    this.loading = true;
    this.error = '';
    this.weeks = this.buildEmptyWeeks();
    this.cdr.detectChanges();

    this.calendarService.getEvents(this.currentYear, this.currentMonth).subscribe({
      next: res => {
        if (res.success) this.populateEvents(res.data);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'No se pudieron cargar los eventos.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private buildEmptyWeeks(): CalendarDay[][] {
    const year = this.currentYear;
    const month = this.currentMonth;
    const firstDay = new Date(year, month - 1, 1);
    const lastDay = new Date(year, month, 0);
    const startOffset = (firstDay.getDay() + 6) % 7; // Mon=0

    const days: CalendarDay[] = [];
    for (let i = 0; i < startOffset; i++)
      days.push({ date: null, dayNumber: null, isToday: false, isCurrentMonth: false, events: [] });

    for (let d = 1; d <= lastDay.getDate(); d++) {
      const date = new Date(year, month - 1, d);
      days.push({ date, dayNumber: d, isToday: date.toDateString() === this.today.toDateString(), isCurrentMonth: true, events: [] });
    }

    while (days.length % 7 !== 0)
      days.push({ date: null, dayNumber: null, isToday: false, isCurrentMonth: false, events: [] });

    const weeks: CalendarDay[][] = [];
    for (let i = 0; i < days.length; i += 7) weeks.push(days.slice(i, i + 7));
    return weeks;
  }

  private populateEvents(events: CalendarEvent[]): void {
    const byDate = new Map<string, CalendarEvent[]>();
    for (const ev of events) byDate.set(ev.date, [...(byDate.get(ev.date) ?? []), ev]);

    for (const week of this.weeks)
      for (const day of week)
        if (day.date) day.events = byDate.get(this.dateKey(day.date)) ?? [];
  }

  private dateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
