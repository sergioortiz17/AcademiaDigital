import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CalendarSection, CalendarService } from '../../../core/services/calendar.service';
import { UserRole } from '../../../store/account/account.actions';

export interface CreateEventDialogData {
  role: UserRole;
  preselectedDate?: string;
}

@Component({
  selector: 'app-create-event-dialog',
  templateUrl: './create-event-dialog.component.html',
  styleUrls: ['./create-event-dialog.component.scss'],
  standalone: false
})
export class CreateEventDialogComponent implements OnInit {
  title = '';
  description = '';
  eventDate: Date | null = null;
  startTime = '';
  eventType = 'Otro';
  modality: string | null = null;
  courseSectionId: number | null = null;

  sections: CalendarSection[] = [];
  loadingSections = false;
  isProfesor = false;
  isAdmin = false;

  eventTypes = [
    { value: 'Examen', label: 'Examen' },
    { value: 'EntregaTP', label: 'Entrega TP' },
    { value: 'Clase', label: 'Clase' },
    { value: 'Otro', label: 'Otro' }
  ];

  modalities = [
    { value: null, label: 'Sin definir' },
    { value: 'Presencial', label: 'Presencial' },
    { value: 'Virtual', label: 'Virtual' }
  ];

  constructor(
    public dialogRef: MatDialogRef<CreateEventDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEventDialogData,
    private calendarService: CalendarService
  ) {
    this.isProfesor = data.role === UserRole.Profesor;
    this.isAdmin = data.role === UserRole.Admin;
    if (data.preselectedDate) {
      this.eventDate = new Date(data.preselectedDate + 'T12:00:00');
    }
  }

  ngOnInit(): void {
    if (this.isProfesor) {
      this.loadingSections = true;
      this.calendarService.getMySections().subscribe({
        next: res => {
          this.sections = res.data;
          this.loadingSections = false;
        },
        error: () => this.loadingSections = false
      });
    }
  }

  get isValid(): boolean {
    if (!this.title.trim() || !this.eventDate) return false;
    if (this.isProfesor && !this.courseSectionId) return false;
    return true;
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    if (!this.isValid || !this.eventDate) return;
    this.dialogRef.close({
      title: this.title.trim(),
      description: this.description.trim() || null,
      date: this.toDateOnly(this.eventDate),
      startTime: this.startTime || null,
      eventType: this.eventType,
      courseSectionId: this.courseSectionId,
      modality: this.modality
    });
  }

  private toDateOnly(date: Date): string {
    const y = date.getFullYear();
    const m = (date.getMonth() + 1).toString().padStart(2, '0');
    const d = date.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
