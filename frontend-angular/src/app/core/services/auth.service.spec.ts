import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('login()', () => {
    it('posts credentials to the login endpoint', () => {
      service.login({ email: 'a@b.com', password: '123' }).subscribe();
      const req = http.expectOne('/api/v1/users/login');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'a@b.com', password: '123' });
      req.flush({ success: true, token: 'tok' });
    });

    it('returns the response as-is', () => {
      let result: any;
      service.login({ email: 'a@b.com', password: '123' }).subscribe((r) => (result = r));
      http.expectOne('/api/v1/users/login').flush({ success: true, token: 'jwt', user: { _id: 1 } });
      expect(result.success).toBe(true);
      expect(result.token).toBe('jwt');
    });
  });

  describe('forgotPassword()', () => {
    it('posts email and dni to the forgot-password endpoint', () => {
      service.forgotPassword('a@b.com', '12345678').subscribe();
      const req = http.expectOne('/api/v1/users/forgot-password');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'a@b.com', dni: '12345678' });
      req.flush({ success: true });
    });
  });

  describe('verifyResetCode()', () => {
    it('posts email and code to the verify-reset-code endpoint', () => {
      service.verifyResetCode('a@b.com', '123456').subscribe();
      const req = http.expectOne('/api/v1/users/verify-reset-code');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'a@b.com', code: '123456' });
      req.flush({ success: true, resetToken: 'reset-jwt' });
    });
  });

  describe('resetPassword()', () => {
    it('posts resetToken and newPassword to the reset-password endpoint', () => {
      service.resetPassword('reset-jwt', 'NewPass1!').subscribe();
      const req = http.expectOne('/api/v1/users/reset-password');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ resetToken: 'reset-jwt', newPassword: 'NewPass1!' });
      req.flush({ success: true, msg: 'ok' });
    });
  });

  describe('getStoredToken()', () => {
    afterEach(() => localStorage.removeItem('academia-account'));

    it('returns null when nothing is stored', () => {
      expect(service.getStoredToken()).toBeNull();
    });

    it('returns the token from localStorage', () => {
      localStorage.setItem('academia-account', JSON.stringify({ token: 'stored-tok' }));
      expect(service.getStoredToken()).toBe('stored-tok');
    });

    it('returns null when stored JSON has no token', () => {
      localStorage.setItem('academia-account', JSON.stringify({ other: 'data' }));
      expect(service.getStoredToken()).toBeNull();
    });
  });

  describe('clearSession()', () => {
    it('removes academia-account from localStorage', () => {
      localStorage.setItem('academia-account', '{"token":"x"}');
      service.clearSession();
      expect(localStorage.getItem('academia-account')).toBeNull();
    });
  });
});
