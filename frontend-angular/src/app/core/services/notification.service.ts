import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

type Level = 'success' | 'error' | 'warning' | 'info';

const DURATIONS: Record<Level, number> = {
  success: 3000,
  info: 3500,
  warning: 5000,
  error: 6000
};

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private snackBar: MatSnackBar) {}

  success(message: string) { this.show(message, 'success'); }
  info(message: string)    { this.show(message, 'info'); }
  warning(message: string) { this.show(message, 'warning'); }
  error(message: string)   { this.show(message, 'error'); }

  show(message: string, level: Level = 'info', config: MatSnackBarConfig = {}) {
    if (!message) return;
    this.snackBar.open(message, 'Cerrar', {
      duration: DURATIONS[level],
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['app-snackbar', `app-snackbar--${level}`],
      ...config
    });
  }
}
