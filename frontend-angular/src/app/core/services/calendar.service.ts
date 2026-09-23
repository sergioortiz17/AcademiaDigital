import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CalendarEvent {
  id: number;
  title: string;
  description: string | null;
  eventType: string;
  date: string;
  startTime: string | null;
  scope: string;
  modality: string | null;
  courseSectionId: number | null;
  courseName: string | null;
  createdByName: string | null;
  createdByUserId: number | null;
  isHoliday: boolean;
}

export interface CalendarSection {
  id: number;
  courseName: string;
  courseCode: string;
  academicYear: number;
  semester: number;
}

export interface CreateEventRequest {
  title: string;
  description?: string | null;
  date: string;
  startTime?: string | null;
  eventType?: string;
  courseSectionId?: number | null;
  modality?: string | null;
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

  getUpcoming(): Observable<{ success: boolean; data: CalendarEvent[] }> {
    return this.http.get<{ success: boolean; data: CalendarEvent[] }>(
      `${this.base}v1/calendar/upcoming`
    );
  }

  getMySections(): Observable<{ success: boolean; data: CalendarSection[] }> {
    return this.http.get<{ success: boolean; data: CalendarSection[] }>(
      `${this.base}v1/calendar/my-sections`
    );
  }

  createEvent(req: CreateEventRequest): Observable<{ success: boolean; data: any }> {
    return this.http.post<{ success: boolean; data: any }>(
      `${this.base}v1/calendar/events`, req
    );
  }

  updateEvent(id: number, req: CreateEventRequest): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(
      `${this.base}v1/calendar/events/${id}`, req
    );
  }

  deleteEvent(id: number): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(
      `${this.base}v1/calendar/events/${id}`
    );
  }
}
