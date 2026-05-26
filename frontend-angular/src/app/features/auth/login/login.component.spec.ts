import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let authService: { login: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let store: { dispatch: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authService = { login: vi.fn() };
    router = { navigate: vi.fn() };
    store = { dispatch: vi.fn() };

    await TestBed.configureTestingModule({
      declarations: [LoginComponent],
      imports: [ReactiveFormsModule, TranslateModule.forRoot()],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: Store, useValue: store }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => localStorage.removeItem('remember-user'));

  describe('form initialization', () => {
    it('starts invalid with empty fields', () => {
      expect(component.loginForm.invalid).toBe(true);
    });

    it('is valid with correct email and password', () => {
      component.loginForm.setValue({ email: 'a@b.com', password: 'secret', rememberMe: false });
      expect(component.loginForm.valid).toBe(true);
    });

    it('pre-fills email and password from localStorage remember-user', () => {
      localStorage.setItem('remember-user', JSON.stringify({ email: 's@s.com', password: 'pass' }));
      const fixture2 = TestBed.createComponent(LoginComponent);
      const c2 = fixture2.componentInstance;
      expect(c2.loginForm.value.email).toBe('s@s.com');
      expect(c2.loginForm.value.rememberMe).toBe(true);
    });
  });

  describe('onSubmit()', () => {
    it('marks all fields as touched when form is invalid', () => {
      vi.spyOn(component.loginForm, 'markAllAsTouched');
      component.onSubmit();
      expect(component.loginForm.markAllAsTouched).toHaveBeenCalled();
      expect(authService.login).not.toHaveBeenCalled();
    });

    it('calls authService.login with email and password', () => {
      authService.login.mockReturnValue(of({ success: false, msg: 'bad' }));
      component.loginForm.setValue({ email: 'a@b.com', password: '123', rememberMe: false });
      component.onSubmit();
      expect(authService.login).toHaveBeenCalledWith({ email: 'a@b.com', password: '123' });
    });

    it('dispatches accountInitialize and navigates on success', () => {
      authService.login.mockReturnValue(
        of({ success: true, token: 'tok', user: { _id: 1, email: 'a@b.com', role: 1 } })
      );
      component.loginForm.setValue({ email: 'a@b.com', password: '123', rememberMe: false });
      component.onSubmit();
      expect(store.dispatch).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/app/dashboard/default']);
    });

    it('sets errorMessage when success is false', () => {
      authService.login.mockReturnValue(of({ success: false, msg: 'Credenciales inválidas' }));
      component.loginForm.setValue({ email: 'a@b.com', password: '123', rememberMe: false });
      component.onSubmit();
      expect(component.errorMessage).toBe('Credenciales inválidas');
    });

    it('sets errorMessage on HTTP error', () => {
      authService.login.mockReturnValue(throwError(() => ({ message: 'Network error' })));
      component.loginForm.setValue({ email: 'a@b.com', password: '123', rememberMe: false });
      component.onSubmit();
      expect(component.errorMessage).toBe('Network error');
      expect(component.isLoading).toBe(false);
    });

    it('saves remember-user to localStorage when rememberMe is true', () => {
      authService.login.mockReturnValue(
        of({ success: true, token: 'tok', user: { _id: 1, email: 'a@b.com', role: 1 } })
      );
      component.loginForm.setValue({ email: 'a@b.com', password: 'pw', rememberMe: true });
      component.onSubmit();
      const stored = JSON.parse(localStorage.getItem('remember-user') ?? '{}');
      expect(stored).toEqual({ email: 'a@b.com', password: 'pw' });
    });
  });
});
