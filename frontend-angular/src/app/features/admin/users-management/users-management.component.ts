import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { AdminService, UserSummary } from '../../../core/services/admin.service';
import { UserRole } from '../../../store/account/account.actions';
import { Store } from '@ngrx/store';
import { selectUser } from '../../../store/account/account.selectors';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

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

  // Search & pagination
  searchTerm = '';
  page = 1;
  pageSize = 20;
  total = 0;
  pageSizeOptions = [20, 50, 100];

  displayedColumns = ['username', 'dni', 'role', 'dateJoined', 'actions'];

  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  getRoleLabel(role: number): string {
    const labels: Record<number, string> = { 1: 'Alumno', 2: 'Profesor', 3: 'Admin' };
    return labels[role] ?? 'Desconocido';
  }

  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize);
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

    this.search$.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.page = 1;
      this.loadUsers();
    });

    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.search$.next(value);
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.loadUsers();
  }

  prevPage(): void {
    if (this.page > 1) { this.page--; this.loadUsers(); }
  }

  nextPage(): void {
    if (this.page < this.totalPages) { this.page++; this.loadUsers(); }
  }

  loadUsers(): void {
    if (this.isLoading) return;
    this.isLoading = true;
    this.errorMsg = '';
    this.adminService.getUsers(this.searchTerm, this.page, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.users = res.users;
          this.total = res.total;
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
        this.total = Math.max(0, this.total - 1);
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
