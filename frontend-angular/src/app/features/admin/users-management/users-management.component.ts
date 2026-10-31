import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { AdminService, UserSummary } from '../../../core/services/admin.service';
import { UserRole } from '../../../store/account/account.actions';
import { Store } from '@ngrx/store';
import { selectUser } from '../../../store/account/account.selectors';
import { forkJoin, Observable, of, Subject } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs/operators';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { UserDetailsDialogComponent, UserDetailsDialogData } from '../../../shared/user-details-dialog/user-details-dialog.component';
import { Student, StudentListItem, StudentRecord, StudentService } from '../../../core/services/student.service';
import { PersonNameLookupService } from '../../../core/services/person-name-lookup.service';


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
  roleFiltersOpen = false;
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
  private usersRequestId = 0;

  getRoleLabel(role: number): string {
    const labels: Record<number, string> = { 1: 'Alumno', 2: 'Profesor', 3: 'Admin' };
    return labels[role] ?? 'Desconocido';
  }

  get selectedRoleLabel(): string {
    return this.roleFilters.find(filter => filter.value === this.selectedRole)?.label ?? 'Todos';
  }

  get visibleRoleFilters(): typeof this.roleFilters {
    return this.selectedRole === null ? this.roleFilters.slice(1) : this.roleFilters;
  }

  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize);
  }

  constructor(
    private readonly adminService: AdminService,
    private readonly store: Store,
    private readonly cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private readonly studentService: StudentService,
    private readonly personNames: PersonNameLookupService
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
    this.roleFiltersOpen = false;
    this.page = 1;
    this.loadUsers();
  }

  toggleRoleFilters(): void {
    this.roleFiltersOpen = !this.roleFiltersOpen;
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
    const requestId = ++this.usersRequestId;
    this.isLoading = true;
    this.errorMsg = '';
    forkJoin({
      response: this.adminService.getUsers(this.searchTerm, this.selectedRole, this.page, this.pageSize),
      names: this.personNames.getNamesByUserId().pipe(catchError(() => of(null)))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ response, names }) => {
          if (requestId !== this.usersRequestId) return;
          this.users = response.users.map(user => ({
            ...user,
            displayName: names?.get(user.id) || user.username
          }));
          this.total = response.total;
          if (!names) {
            this.errorMsg = 'No se pudieron consultar todos los nombres completos; se muestran los nombres de usuario disponibles.';
          }
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          if (requestId !== this.usersRequestId) return;
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

openUserDetail(user: UserSummary): void {
  const searchTerm = user.dni || user.username;
  this.findStudentForUser(user.id, searchTerm).subscribe({
    next: student => {
      if (!student) {
        this.openUserDetailsDialog(user, this.emptyUserDetails(user));
        return;
      }
      this.loadStudentDetails(user, student.id);
    },
    error: err => {
      this.errorMsg = err.error?.msg || err.error?.detail || err.error?.message || err.message || 'No se pudo cargar la ficha del usuario.';
      this.cdr.detectChanges();
    }
  });
}

private findStudentForUser(userId: number, searchTerm: string, page = 1): Observable<StudentListItem | null> {
  return this.studentService.searchStudents(searchTerm, undefined, page, 100).pipe(
    switchMap(response => {
      const student = response.items.find(item => item.userId === userId);
      if (student || response.page * response.pageSize >= response.total) return of(student ?? null);
      return this.findStudentForUser(userId, searchTerm, page + 1);
    })
  );
}

private loadStudentDetails(user: UserSummary, studentId: number): void {
  forkJoin({
    student: this.studentService.getStudent(studentId),
    record: this.studentService.getStudentRecord(studentId)
  }).subscribe({
    next: result => this.openUserDetailsDialog(user, this.studentDetails(user, result.student, result.record)),
    error: err => {
      this.errorMsg = err.error?.msg || err.error?.detail || err.error?.message || err.message || 'No se pudo cargar la ficha del alumno.';
      this.cdr.detectChanges();
    }
  });
}

private openUserDetailsDialog(user: UserSummary, data: UserDetailsDialogData): void {
    const dialogRef = this.dialog.open(UserDetailsDialogComponent, {
      width: '600px',
      panelClass: 'user-dialog-panel',
      data
    });
    dialogRef.afterClosed().subscribe(saved => {
      if (!saved) return;
      this.successMsg = `Legajo de ${user.username} actualizado.`;
      this.cdr.detectChanges();
      setTimeout(() => {
        this.successMsg = '';
        this.cdr.detectChanges();
      }, 3000);
    });
}

private studentDetails(user: UserSummary, student: Student, record: StudentRecord): UserDetailsDialogData {
    return {
      studentId: student.id,
      username: record.personalData.firstName || user.username,
      lastName: record.personalData.lastName,
      email: record.personalData.email || user.email,
      dni: record.personalData.dni ?? user.dni,
      carrera: student.careerName,
      legajo: record.student.legajoNumber,
      birthDate: record.personalData.birthDate,
      phone: record.personalData.phone,
      fechaIngreso: record.student.enrollmentDate,
      estado: record.student.status,
      plan: student.currentStudyPlanName || '-',
      addressLine: record.address.addressLine,
      city: record.address.city,
      province: record.address.province,
      postalCode: record.address.postalCode,
      emergencyContactName: record.emergencyContact.name,
      emergencyContactRelationship: record.emergencyContact.relationship,
      emergencyContactPhone: record.emergencyContact.phone
    };
}

private emptyUserDetails(user: UserSummary): UserDetailsDialogData {
    return {
      studentId: null,
      username: user.username,
      email: user.email,
      dni: user.dni,
      carrera: '-',
      legajo: '-',
      birthDate: null,
      phone: null,
      fechaIngreso: '-',
      estado: '-',
      plan: '-',
      addressLine: null,
      city: null,
      province: null,
      postalCode: null,
      emergencyContactName: null,
      emergencyContactRelationship: null,
      emergencyContactPhone: null
    };
}

}
