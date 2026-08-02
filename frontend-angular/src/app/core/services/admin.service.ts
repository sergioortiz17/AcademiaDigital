import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserRole } from '../../store/account/account.actions';

export interface UserSummary {
  id: number;
  username: string;
  email: string;
  dni?: string;
  role: UserRole;
  isActive: boolean;
  dateJoined: string;
}

export interface GetUsersResponse {
  success: boolean;
  users: UserSummary[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly base = environment.apiServer;

  constructor(private readonly http: HttpClient) {}

  getUsers(search?: string, role?: number | null, page = 1, pageSize = 20): Observable<GetUsersResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search?.trim()) params = params.set('search', search.trim());
    if (role != null) params = params.set('role', role.toString());
    return this.http.get<GetUsersResponse>(`${this.base}v1/admin/users`, { params });
  }

  updateRole(userId: number, role: UserRole): Observable<{ success: boolean; user: UserSummary }> {
    return this.http.patch<{ success: boolean; user: UserSummary }>(`${this.base}v1/admin/users/${userId}/role`, { role });
  }

  deleteUser(userId: number): Observable<{ success: boolean; msg: string }> {
    return this.http.delete<{ success: boolean; msg: string }>(`${this.base}v1/admin/users/${userId}`);
  }

  activateUser(userId: number): Observable<{ success: boolean; user: UserSummary }> {
    return this.http.patch<{ success: boolean; user: UserSummary }>(
      `${this.base}v1/admin/users/${userId}/active`,
      { isActive: true }
    );
  }

  disableUser(userId: number): Observable<{ success: boolean; user: UserSummary }> {
    return this.http.patch<{ success: boolean; user: UserSummary }>(
      `${this.base}v1/admin/users/${userId}/active`,
      { isActive: false }
    );
  }

  
}
