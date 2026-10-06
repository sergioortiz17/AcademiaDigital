import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { CalendarService, CalendarEvent, CreateEventRequest } from '../../core/services/calendar.service';
import { selectUserRole, selectUser } from '../../store/account/account.selectors';
import { UserModel, UserRole } from '../../store/account/account.actions';
import { CreateEventDialogComponent, CreateEventDialogData } from './create-event-dialog/create-event-dialog.component';

interface CalendarDay {
  date: Date | null;
  dayNumber: number | null;
  isToday: boolean;
  isCurrentMonth: boolean;
  isHoliday: boolean;
  events: CalendarEvent[];
}

@Component({
  selector: 'app-calendar',
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.scss'],
  standalone: false
})
export class CalendarComponent implements OnInit {
  weekDays = ['Lunes', 'Martes', 'Miercoles', 'Jueves', 'Viernes', 'Sabado', 'Domingo'];
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

  userRole: UserRole | null = null;
  user: UserModel | null = null;
  UserRole = UserRole;

  private today = new Date();

  constructor(
    private readonly calendarService: CalendarService,
    private readonly cdr: ChangeDetectorRef,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly store: Store
  ) {}

  ngOnInit(): void {
    this.currentYear = this.today.getFullYear();
    this.currentMonth = this.today.getMonth() + 1;

    this.store.select(selectUserRole).subscribe(role => this.userRole = role);
    this.store.select(selectUser).subscribe(user => this.user = user);

    this.loadMonth();
  }

  get canCreateEvents(): boolean {
    return this.userRole === UserRole.Admin || this.userRole === UserRole.Profesor;
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
      Examen: 'Examen', EntregaTP: 'Entrega TP', Clase: 'Clase', Feriado: 'Feriado', Otro: 'Evento'
    };
    return map[type] ?? type;
  }

  eventTypeClass(type: string): string {
    const map: Record<string, string> = {
      Examen: 'type-examen', EntregaTP: 'type-entrega', Clase: 'type-clase', Feriado: 'type-feriado', Otro: 'type-otro'
    };
    return map[type] ?? 'type-otro';
  }

  dayDominantType(day: CalendarDay): string {
    if (day.events.length === 0) return '';
    const priority = ['Feriado', 'Examen', 'EntregaTP', 'Clase', 'Otro'];
    for (const p of priority) {
      if (day.events.some(e => e.eventType === p)) return this.eventTypeClass(p);
    }
    return 'type-otro';
  }

  scopeLabel(ev: CalendarEvent): string {
    if (ev.isHoliday) return 'Feriado Nacional';
    if (ev.scope === 'Global') return 'Institucional';
    return ev.courseName ?? 'Materia';
  }

  modalityLabel(ev: CalendarEvent): string | null {
    return ev.modality ?? null;
  }

  canDeleteEvent(ev: CalendarEvent): boolean {
    if (ev.isHoliday) return false;
    if (this.userRole === UserRole.Admin) return true;
    if (this.userRole === UserRole.Profesor && ev.createdByUserId === Number(this.user?._id)) return true;
    return false;
  }

  openCreateDialog(preselectedDate?: string): void {
    if (!this.canCreateEvents || !this.userRole) return;

    const dialogRef = this.dialog.open(CreateEventDialogComponent, {
      panelClass: 'custom-dialog',
      data: { role: this.userRole, preselectedDate } as CreateEventDialogData
    });

    dialogRef.afterClosed().subscribe((result: CreateEventRequest | null) => {
      if (!result) return;
      this.calendarService.createEvent(result).subscribe({
        next: () => {
          this.snackBar.open('Evento creado', 'OK', { duration: 3000 });
          this.loadMonth();
        },
        error: () => this.snackBar.open('Error al crear evento', 'OK', { duration: 3000 })
      });
    });
  }

  deleteEvent(ev: CalendarEvent): void {
    if (!confirm('¿Eliminar este evento?')) return;
    this.calendarService.deleteEvent(ev.id).subscribe({
      next: () => {
        this.snackBar.open('Evento eliminado', 'OK', { duration: 3000 });
        this.selectedDay = null;
        this.loadMonth();
      },
      error: () => this.snackBar.open('Error al eliminar evento', 'OK', { duration: 3000 })
    });
  }

  onDayDoubleClick(day: CalendarDay): void {
    if (!day.date || !day.isCurrentMonth || !this.canCreateEvents) return;
    this.openCreateDialog(this.dateKey(day.date));
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
    const startOffset = (firstDay.getDay() + 6) % 7;

    const days: CalendarDay[] = [];
    for (let i = 0; i < startOffset; i++)
      days.push({ date: null, dayNumber: null, isToday: false, isCurrentMonth: false, isHoliday: false, events: [] });

    for (let d = 1; d <= lastDay.getDate(); d++) {
      const date = new Date(year, month - 1, d);
      days.push({ date, dayNumber: d, isToday: date.toDateString() === this.today.toDateString(), isCurrentMonth: true, isHoliday: false, events: [] });
    }

    while (days.length % 7 !== 0)
      days.push({ date: null, dayNumber: null, isToday: false, isCurrentMonth: false, isHoliday: false, events: [] });

    const weeks: CalendarDay[][] = [];
    for (let i = 0; i < days.length; i += 7) weeks.push(days.slice(i, i + 7));
    return weeks;
  }

  private populateEvents(events: CalendarEvent[]): void {
    const byDate = new Map<string, CalendarEvent[]>();
    for (const ev of events) byDate.set(ev.date, [...(byDate.get(ev.date) ?? []), ev]);

    for (const week of this.weeks)
      for (const day of week)
        if (day.date) {
          const key = this.dateKey(day.date);
          day.events = byDate.get(key) ?? [];
          day.isHoliday = day.events.some(e => e.isHoliday);
        }
  }

  private dateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
