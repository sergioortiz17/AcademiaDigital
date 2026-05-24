import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { AdminService, UserSummary } from '../../../core/services/admin.service';
import { UserRole } from '../../../store/account/account.actions';
import { Store } from '@ngrx/store';
import { selectUser } from '../../../store/account/account.selectors';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-users-management',
  templateUrl: './users-management.component.html',
  styleUrls: ['./users-management.component.scss'],
  standalone: false
})
export class UsersManagementComponent implements OnInit, OnDestroy {
  users: UserSummary[] = [];
  isLoading = false;
  errorMsg = '';
  successMsg = '';
  UserRole = UserRole;
  currentUserId: number | null = null;

  displayedColumns = ['username', 'email', 'role', 'dateJoined', 'actions'];

  private readonly destroy$ = new Subject<void>();

  getRoleLabel(role: number): string {
    const labels: Record<number, string> = { 1: 'Alumno', 2: 'Profesor', 3: 'Admin' };
    return labels[role] ?? 'Desconocido';
  }

  constructor(
    private readonly adminService: AdminService,
    private readonly store: Store,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.store.select(selectUser).pipe(takeUntil(this.destroy$)).subscribe(u => {
      this.currentUserId = u ? Number(u._id) : null;
    });
    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadUsers(): void {
    if (this.isLoading) return;
    this.isLoading = true;
    this.errorMsg = '';
    this.adminService.getUsers().pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.users = res.users;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'Error al cargar usuarios.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  changeRole(user: UserSummary, newRole: UserRole): void {
    this.adminService.updateRole(user.id, newRole).subscribe({
      next: (res) => {
        user.role = res.user.role;
        this.successMsg = `Rol de ${user.username} actualizado.`;
        setTimeout(() => this.successMsg = '', 3000);
      },
      error: (err) => { this.errorMsg = err.error?.msg || 'Error al actualizar rol.'; }
    });
  }

  deleteUser(user: UserSummary): void {
    if (!confirm(`¿Eliminar al usuario "${user.username}"?`)) return;
    this.adminService.deleteUser(user.id).subscribe({
      next: () => {
        this.users = this.users.filter(u => u.id !== user.id);
        this.successMsg = `Usuario ${user.username} eliminado.`;
        setTimeout(() => this.successMsg = '', 3000);
      },
      error: (err) => { this.errorMsg = err.error?.msg || 'Error al eliminar usuario.'; }
    });
  }

  isCurrentUser(user: UserSummary): boolean {
    return this.currentUserId === user.id;
  }
}
