import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { AdminService, UserSummary } from '../../../core/services/admin.service';
import { UserRole } from '../../../store/account/account.actions';
import { Store } from '@ngrx/store';
import { selectUser } from '../../../store/account/account.selectors';
import { Subject, forkJoin  } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { UserService } from '../../../core/services/user.service';
import { UserDetailsDialogComponent } from '../../../shared/user-details-dialog/user-details-dialog.component';
import { StudentService } from '../../../core/services/student.service';



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

  // Search, role filter & pagination
  searchTerm = '';
  selectedRole: number | null = null;
  page = 1;
  pageSize = 20;
  total = 0;
  pageSizeOptions = [20, 50, 100];

  roleFilters = [
    { label: 'Todos',          value: null },
    { label: 'Alumnos',        value: UserRole.Alumno },
    { label: 'Profesores',     value: UserRole.Profesor },
    { label: 'Administradores', value: UserRole.Admin },
  ];

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
    private readonly cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private readonly studentService: StudentService
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

  onRoleFilter(role: number | null): void {
    this.selectedRole = role;
    this.page = 1;
    this.loadUsers();
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
    this.adminService.getUsers(this.searchTerm, this.selectedRole, this.page, this.pageSize)
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

  const dialogRef = this.openConfirmationDialog(
    `MODIFICAR`,
    user
  ); //${this.getRoleLabel(newRole)}

  dialogRef.afterClosed().subscribe(result => {

    if (!result) return;

    this.adminService.updateRole(user.id, newRole).subscribe({
      next: (res) => {

        this.users = this.users.map(u =>
          u.id === user.id
          ? { ...u, role: res.user.role }
          : u
        );

        this.successMsg = `Rol de ${user.username} actualizado.`;
        this.cdr.detectChanges();

        setTimeout(() => {
          this.successMsg = '';
          this.cdr.detectChanges();
        }, 3000);
      },
      error: (err) => {
        this.errorMsg =
          err.error?.msg || 'Error al actualizar rol.';
        this.cdr.detectChanges();
      }
    });
  });
}

deleteUser(user: UserSummary): void {

  const dialogRef =
    this.openConfirmationDialog('ELIMINAR', user);

  dialogRef.afterClosed().subscribe(result => {

    if (!result) return;
  
    this.adminService.deleteUser(user.id).subscribe({
      next: () => {

        this.users = this.users.filter(u => u.id !== user.id);
        this.successMsg =
          `Usuario ${user.username} eliminado.`;
        this.cdr.detectChanges();

        setTimeout(() => {
          this.successMsg = '';
          this.cdr.detectChanges();
        }, 3000);
      },
      error: (err) => {
        this.errorMsg =
          err.error?.msg || 'Error al eliminar usuario.';
        this.cdr.detectChanges();
      }
    });
  });
}

isCurrentUser(user: UserSummary): boolean {
  return this.currentUserId === user.id;
}

disableUser(user: UserSummary): void {

  const dialogRef = this.openConfirmationDialog('DESACTIVAR', user);

  dialogRef.afterClosed().subscribe(result => {

    if (!result) return;

    this.adminService.disableUser(user.id).subscribe({
      next: (res) => {

        this.users = this.users.map(u =>
          u.id === user.id
            ? { ...u, isActive: res.user.isActive }
            : u
        );

        this.successMsg = `Usuario ${user.username} desactivado.`;

        this.cdr.detectChanges();

        setTimeout(() => {
          this.successMsg = '';
          this.cdr.detectChanges();
        }, 3000);

      },
      error: (err) => {
        this.errorMsg = err.error?.msg || 'Error al desactivar usuario.';
        this.cdr.detectChanges();
      }
    });

  });
}

activateUser(user: UserSummary): void {

  const dialogRef =
    this.openConfirmationDialog('ACTIVAR', user);

  dialogRef.afterClosed().subscribe(result => {

    if (!result) return;

    this.adminService.activateUser(user.id).subscribe({
      next: (res) => {

        this.users = this.users.map(u =>
          u.id === user.id
          ? { ...u, isActive: res.user.isActive }
          : u
    );

        this.successMsg =
          `Usuario ${user.username} activado.`;
        this.cdr.detectChanges();

        setTimeout(() => {
          this.successMsg = '';
          this.cdr.detectChanges();
        }, 3000);
      },
      error: (err) => {
        this.errorMsg =
          err.error?.msg || 'Error al activar usuario.';
        this.cdr.detectChanges();
      }
    });

  });
}

private openConfirmationDialog(
  action: string,
  user: UserSummary
) {
  return this.dialog.open(ConfirmDialogComponent, {
    width: '450px',
    disableClose: true,
    data: {
      title: 'Acción importante',
      action,
      username: user.username,
      dni: user.dni,
      role: this.getRoleLabel(user.role)
    }
  });
}

//AGREGAR CON GET POR ID ANTES DE HABILITAR
//openUserDetails(user: UserSummary): void {
//  this.userService.getProfile(user.id)
//    .subscribe({

//      next: (profile) => {

//        this.dialog.open(UserDetailsDialogComponent, {
//          width: '700px',
//          disableClose: true,
//          data: profile
//        });

//      },

//      error: () => {
//        this.errorMsg = 'No se pudieron cargar los datos.';
//      }

//    });

//}

openUserDetail(user: UserSummary): void {

  this.studentService.getStudentByUserId(user.id)
    .subscribe({

     next: (student) => {

        this.dialog.open(UserDetailsDialogComponent, {
          width: '600px',
          panelClass: 'user-dialog-panel',
          data: {

            username: user.username,
            //lastName: user.lastName,
            email: user.email,
            dni: user.dni,

            carrera: student.careerName,
            legajo: student.legajoNumber,

            fechaIngreso: student.enrollmentDate,
            estado: student.status,
            plan: student.currentStudyPlanName
          }
        });

      },

      error: () => {

        this.dialog.open(UserDetailsDialogComponent, {
          width: '600px',
          panelClass: 'user-dialog-panel',
          data: {

            username: user.username,
            email: user.email,
            dni: user.dni,

            carrera: '-',
            legajo: '-',
            fechaIngreso: '-',
            estado: '-',
            plan: '-'
          }
        });

      }

    });

}

}
