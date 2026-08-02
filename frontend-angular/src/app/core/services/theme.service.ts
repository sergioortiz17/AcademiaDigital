import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'academia-theme';
  isDark = false;

  constructor() {
    // Dark mode disabled until implemented — always force light
    localStorage.removeItem(this.STORAGE_KEY);
    this.apply();
  }

  toggle(): void {
    this.isDark = !this.isDark;
    localStorage.setItem(this.STORAGE_KEY, this.isDark ? 'dark' : 'light');
    this.apply();
  }

  private apply(): void {
    document.body.classList.toggle('dark-theme', this.isDark);
  }
}
