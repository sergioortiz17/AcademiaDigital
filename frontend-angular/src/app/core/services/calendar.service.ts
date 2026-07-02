import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CalendarEvent {
  id: number;
  title: string;
  description: string | null;
  eventType: string; // Examen | EntregaTP | Clase | Otro
  date: string;       // yyyy-MM-dd
  startTime: string | null; // HH:mm
}

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private base = environment.apiServer;

  constructor(private http: HttpClient) {}

  getEvents(year: number, month: number): Observable<{ success: boolean; data: CalendarEvent[] }> {
    return this.http.get<{ success: boolean; data: CalendarEvent[] }>(
      `${this.base}v1/calendar/events?year=${year}&month=${month}`
    );
  }
}
