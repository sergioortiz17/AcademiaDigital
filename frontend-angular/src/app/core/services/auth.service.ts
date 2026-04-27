import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  email: string;
  username: string;
  password: string;
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

  register(credentials: RegisterCredentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseURL}v1/users/register`, credentials);
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
