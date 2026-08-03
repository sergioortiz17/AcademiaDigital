import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export type AttendanceStatus = 'P' | 'A' | 'J';

export interface AttendanceStudent {
  id: number;
  fullName: string;
  cohort: number;
  legajo: string;
}

export interface AttendanceStudentRow extends AttendanceStudent {
  records: Record<number, AttendanceStatus>;
  averageAttendance: number;
}

export interface AttendanceFilters {
  careerName: string;
  sede: string | null;
  yearNumber: number | null;
  subjectName: string;
  month: Date;
}

// TODO: no existe todavía un endpoint de asistencia en el backend.
// Cuando exista, reemplazar el cuerpo de getAttendance() por:
// return this.http.get<AttendanceStudentRow[]>(`${this.baseURL}v1/attendance`, { params: {...} });
@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly students: AttendanceStudent[] = [
    { id: 1, fullName: 'Jazmín Luna', cohort: 2023, legajo: 'DC-2023-12' },
    { id: 2, fullName: 'Agostina Arce', cohort: 2023, legajo: 'DC-2023-18' },
    { id: 3, fullName: 'Gimena Galán', cohort: 2023, legajo: 'DC-2023-25' },
    { id: 4, fullName: 'Ejemplo Alumno', cohort: 2023, legajo: 'DC-2023-40' },
    { id: 5, fullName: 'Bruno Sosa', cohort: 2023, legajo: 'DC-2023-33' },
    { id: 6, fullName: 'Lucía Fernández', cohort: 2023, legajo: 'DC-2023-07' }
  ];

  getAttendance(filters: AttendanceFilters): Observable<AttendanceStudentRow[]> {
    const daysWithData = Math.min(12, this.daysInMonth(filters.month));
    const subjectSeed = this.hashString(filters.subjectName);

    const rows: AttendanceStudentRow[] = this.students.map(student => {
      const records: Record<number, AttendanceStatus> = {};
      for (let day = 1; day <= daysWithData; day++) {
        records[day] = this.pseudoStatus(student.id, day, subjectSeed);
      }
      return { ...student, records, averageAttendance: this.average(records) };
    });

    return of(rows);
  }

  private pseudoStatus(studentId: number, day: number, subjectSeed: number): AttendanceStatus {
    const seed = studentId * 97 + day * 13 + subjectSeed;
    const frac = Math.abs(Math.sin(seed)) % 1;
    if (frac < 0.78) return 'P';
    if (frac < 0.92) return 'A';
    return 'J';
  }

  private average(records: Record<number, AttendanceStatus>): number {
    const values = Object.values(records);
    if (values.length === 0) return 0;
    const present = values.filter(v => v === 'P' || v === 'J').length;
    return Math.round((present / values.length) * 100);
  }

  private hashString(value: string): number {
    let hash = 0;
    for (let i = 0; i < value.length; i++) hash = (hash * 31 + value.charCodeAt(i)) | 0;
    return hash;
  }

  private daysInMonth(month: Date): number {
    return new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
  }
}
