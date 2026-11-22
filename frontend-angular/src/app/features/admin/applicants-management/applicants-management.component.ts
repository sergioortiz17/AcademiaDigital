import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { AdminService, ApplicantStatus, ApplicantSummary } from '../../../core/services/admin.service';
import { UserRole } from '../../../store/account/account.actions';
import { ROLE_LABELS } from '../../../core/auth/permissions';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

/**
 * Sección "Aspirantes": cuentas del registro público. Las pendientes no pueden iniciar sesión
 * hasta que un Admin las da de alta (de a una o en masa). Los docentes (email del dominio
 * institucional) quedan activos para asignarles materias y divisiones.
 * Reutiliza el layout y los estilos de Gestión de Usuarios.
 */
@Component({
  selector: 'app-applicants-management',
  templateUrl: './applicants-management.component.html',
  styleUrls: [
    '../users-management/users-management.component.scss',
    './applicants-management.component.scss'
  ],
  standalone: false
})
export class ApplicantsManagementComponent implements OnInit, OnDestroy {
  readonly UserRole = UserRole;

  applicants: ApplicantSummary[] = [];
  isLoading = false;
  isApproving = false;
  errorMsg = '';
  successMsg = '';

  status: ApplicantStatus = 'pending';
  searchTerm = '';
  selectedRole: number | null = null;
  roleFiltersOpen = false;
  page = 1;
  pageSize = 20;
  total = 0;
  pageSizeOptions = [20, 50, 100];

  /** Selección por id: se conserva al cambiar de página. */
  readonly selected = new Set<number>();

  readonly roleFilters = [
    { label: 'Todos',      value: null },
    { label: 'Alumnos',    value: UserRole.Alumno },
    { label: 'Profesores', value: UserRole.Profesor }
  ];

  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();
  private requestId = 0;

  constructor(
    private readonly adminService: AdminService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef
  ) {}

  get displayedColumns(): string[] {
    const base = ['username', 'dni', 'role', 'career', 'dateJoined'];
    return this.status === 'pending' ? ['select', ...base, 'actions'] : [...base, 'approvedAt'];
  }

  get selectedRoleLabel(): string {
    return this.roleFilters.find(f => f.value === this.selectedRole)?.label ?? 'Todos';
  }

  get visibleRoleFilters(): typeof this.roleFilters {
    return this.selectedRole === null ? this.roleFilters.slice(1) : this.roleFilters;
  }

  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize);
  }

  get allOnPageSelected(): boolean {
    return this.applicants.length > 0 && this.applicants.every(a => this.selected.has(a.id));
  }

  get someOnPageSelected(): boolean {
    return this.applicants.some(a => this.selected.has(a.id)) && !this.allOnPageSelected;
  }

  ngOnInit(): void {
    this.search$.pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => { this.page = 1; this.load(); });
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getRoleLabel(role: number): string {
    return ROLE_LABELS[role as UserRole] ?? 'Alumno';
  }

  onStatusChange(status: ApplicantStatus): void {
    if (status === this.status) return;
    this.status = status;
    this.selected.clear();
    this.page = 1;
    this.load();
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.search$.next(value);
  }

  onRoleFilter(role: number | null): void {
    this.selectedRole = role;
    this.roleFiltersOpen = false;
    this.selected.clear();
    this.page = 1;
    this.load();
  }

  toggleRoleFilters(): void {
    this.roleFiltersOpen = !this.roleFiltersOpen;
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.load();
  }

  prevPage(): void {
    if (this.page > 1) { this.page--; this.load(); }
  }

  nextPage(): void {
    if (this.page < this.totalPages) { this.page++; this.load(); }
  }

  toggle(applicant: ApplicantSummary, checked: boolean): void {
    if (checked) this.selected.add(applicant.id); else this.selected.delete(applicant.id);
  }

  /** "Seleccionar todos": marca (o desmarca) todos los aspirantes de la página visible. */
  toggleAllOnPage(checked: boolean): void {
    for (const a of this.applicants) {
      if (checked) this.selected.add(a.id); else this.selected.delete(a.id);
    }
  }

  clearSelection(): void {
    this.selected.clear();
  }

  approveOne(applicant: ApplicantSummary): void {
    this.confirmAndApprove([applicant.id], `${applicant.username} ${applicant.lastName}`.trim());
  }

  approveSelected(): void {
    if (this.selected.size === 0) return;
    this.confirmAndApprove([...this.selected], `${this.selected.size} aspirante(s)`);
  }

  load(): void {
    const requestId = ++this.requestId;
    this.isLoading = true;
    this.errorMsg = '';
    this.adminService.getApplicants(this.status, this.searchTerm, this.selectedRole, this.page, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          if (requestId !== this.requestId) return;
          this.applicants = res.items;
          this.total = res.total;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          if (requestId !== this.requestId) return;
          this.errorMsg = 'Error al cargar los aspirantes.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }

  private confirmAndApprove(ids: number[], label: string): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '450px',
      disableClose: true,
      data: { title: 'Dar de alta', action: 'DAR DE ALTA', username: label, dni: '', role: '' }
    });

    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.isApproving = true;
      this.adminService.approveApplicants(ids).subscribe({
        next: res => {
          res.approvedIds.forEach(id => this.selected.delete(id));
          this.isApproving = false;
          this.successMsg = res.skipped > 0
            ? `${res.approvedIds.length} dado(s) de alta. ${res.skipped} ya no estaban pendientes.`
            : `${res.approvedIds.length} dado(s) de alta. Ya pueden iniciar sesión.`;
          this.load();
          setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
        },
        error: err => {
          this.isApproving = false;
          this.errorMsg = err?.error?.msg || err?.message || 'No se pudo dar de alta.';
          this.cdr.detectChanges();
        }
      });
    });
  }
}
