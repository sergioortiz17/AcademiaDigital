import { TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Store } from '@ngrx/store';
import { vi } from 'vitest';
import { App } from './app';
import { ThemeService } from './core/services/theme.service';

describe('App', () => {
  const translate = { setDefaultLang: vi.fn(), use: vi.fn() };
  const store = { dispatch: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [
        RouterModule.forRoot([])
      ],
      declarations: [
        App
      ],
      providers: [
        { provide: TranslateService, useValue: translate },
        { provide: Store, useValue: store },
        { provide: ThemeService, useValue: {} }
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should initialize Spanish as the application language', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(translate.setDefaultLang).toHaveBeenCalledWith('es');
    expect(translate.use).toHaveBeenCalledWith('es');
  });
});
