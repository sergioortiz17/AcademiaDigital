import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  name: string;
  lastname: string;
  email: string;
  password: string;
  DNI: string;
  /** Opcional solo para emails del dominio docente de la institución. */
  careerId?: number | null;
}

export interface RegisterResponse {
  success: boolean;
  msg: string;
  role: number;
  pendingApproval: boolean;
}

export interface AuthResponse {
  success: boolean;
  token?: string;
  user?: any;
  userID?: string;
  msg?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseURL = environment.apiServer;

  constructor(private http: HttpClient) {}

  login(credentials: LoginCredentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseURL}v1/users/login`, credentials);
  }

  /** Client ID de Google (vacío = el botón "Continuar con Google" no se muestra). */
  getGoogleConfig(): Observable<{ clientId: string }> {
    return this.http.get<{ clientId: string }>(`${this.baseURL}v1/users/login/google/config`);
  }

  /** Inicia sesión con el token que devuelve Google; solo para cuentas que ya existen con ese correo. */
  loginWithGoogle(credential: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseURL}v1/users/login/google`, { credential });
  }

  register(credentials: RegisterCredentials): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.baseURL}v1/users/register`, credentials);
  }

  /** Pide el correo de recuperación. La respuesta es siempre la misma: el token solo viaja por email. */
  forgotPassword(email: string, dni: string): Observable<{ success: boolean; msg: string }> {
    return this.http.post<{ success: boolean; msg: string }>(
      `${this.baseURL}v1/users/forgot-password`, { email, dni }
    );
  }

  resetPassword(resetToken: string, newPassword: string): Observable<{ success: boolean; msg: string }> {
    return this.http.post<{ success: boolean; msg: string }>(
      `${this.baseURL}v1/users/reset-password`, { resetToken, newPassword }
    );
  }

  logoutApi(token: string): Observable<any> {
    return this.http.post(`${this.baseURL}v1/users/logout`, { token });
  }

  checkSession(): Observable<any> {
    return this.http.post(`${this.baseURL}v1/users/checkSession`, {});
  }

  getStoredToken(): string | null {
    try {
      const raw = localStorage.getItem('academia-account');
      if (raw) {
        const state = JSON.parse(raw);
        return state.token || null;
      }
    } catch {
      // ignore
    }
    return null;
  }

  clearSession(): void {
    localStorage.removeItem('academia-account');
  }
}
