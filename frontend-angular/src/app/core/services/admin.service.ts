import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserRole } from '../../store/account/account.actions';

export interface UserSummary {
  id: number;
  username: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  dateJoined: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getUsers(): Observable<{ success: boolean; users: UserSummary[] }> {
    return this.http.get<{ success: boolean; users: UserSummary[] }>(`${this.base}v1/admin/users`);
  }

  updateRole(userId: number, role: UserRole): Observable<{ success: boolean; user: UserSummary }> {
    return this.http.patch<{ success: boolean; user: UserSummary }>(`${this.base}v1/admin/users/${userId}/role`, { role });
  }

  deleteUser(userId: number): Observable<{ success: boolean; msg: string }> {
    return this.http.delete<{ success: boolean; msg: string }>(`${this.base}v1/admin/users/${userId}`);
  }
}
