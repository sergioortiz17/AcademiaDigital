import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ProfileData {
  success: boolean;
  id: number;
  username: string;
  lastName: string;
  email: string;
  dni: string | null;
  gender: string | null;
  cuil: string | null;
  birthDate: string | null;
  phoneCode: string | null;
  phone: string | null;
  role: number;
  dateJoined: string;
}

export interface UpdateProfilePayload {
  username?: string;
  lastName?: string;
  gender?: string;
  cuil?: string;
  birthDate?: string | null;
  phoneCode?: string;
  phone?: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private baseURL = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<ProfileData> {
    return this.http.get<ProfileData>(`${this.baseURL}v1/users/profile`);
  }

  updateProfile(data: UpdateProfilePayload): Observable<ProfileData> {
    return this.http.put<ProfileData>(`${this.baseURL}v1/users/profile`, data);
  }

  changePassword(currentPassword: string, newPassword: string): Observable<{ success: boolean; msg: string }> {
    return this.http.put<{ success: boolean; msg: string }>(
      `${this.baseURL}v1/users/change-password`,
      { currentPassword, newPassword }
    );
  }
}
