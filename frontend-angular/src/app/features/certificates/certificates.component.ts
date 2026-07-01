import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, take, takeUntil } from 'rxjs/operators';
import { CertificatesService, CertificateRequest, CERTIFICATE_TYPES } from '../../core/services/certificates.service';
import { selectUserRole } from '../../store/account/account.selectors';
import { UserRole } from '../../store/account/account.actions';

@Component({
  selector: 'app-certificates',
  templateUrl: './certificates.component.html',
  styleUrls: ['./certificates.component.scss'],
  standalone: false
})
export class CertificatesComponent implements OnInit, OnDestroy {
  requests: CertificateRequest[] = [];
  allRequests: CertificateRequest[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMsg = '';
  successMsg = '';
  showForm = false;
  selectedType = '';
  certificateTypes = CERTIFICATE_TYPES;
  UserRole = UserRole;
  userRole: UserRole | null = null;
  showMyRequests = false;

  // Admin filters
  searchTerm = '';
  selectedStatus: string | null = null;

  statusFilters = [
    { label: 'Todos',     value: null },
    { label: 'Pendientes', value: 'Pending' },
    { label: 'Aprobados',  value: 'Approved' },
    { label: 'Rechazados', value: 'Rejected' },
  ];

  displayedColumnsAlumno = ['certificateType', 'status', 'createdAt'];
  displayedColumnsAdmin  = ['username', 'certificateType', 'status', 'createdAt', 'actions'];

  get displayedColumns() {
    return this.userRole === UserRole.Admin
      ? this.displayedColumnsAdmin
      : this.displayedColumnsAlumno;
  }

  statusLabels: Record<string, string> = {
    Pending:  'Pendiente',
    Approved: 'Aprobado',
    Rejected: 'Rechazado'
  };

  private readonly destroy$ = new Subject<void>();
  private readonly search$  = new Subject<string>();

  sortColumn: 'username' | 'certificateType' | 'status' | 'createdAt' = 'createdAt';
  sortDirection: 'asc' | 'desc' = 'desc';

  constructor(
    private readonly certificatesService: CertificatesService,
    private readonly store: Store,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.store.select(selectUserRole).pipe(take(1)).subscribe(role => {
      this.userRole = role as UserRole;
      if(this.userRole===UserRole.Admin){
        this.loadRequests();
      }
    });

    this.search$.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => this.loadRequests());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.search$.next(value);
  }

  onStatusFilter(status: string | null): void {
    this.selectedStatus = status;
    if (this.userRole === UserRole.Admin) {
        this.loadRequests();
    }
    else {
        this.applyFilters();
    }
  }

  loadRequests(): void {
    if (this.isLoading) return;
    this.isLoading = true;
    this.errorMsg = '';

    const obs = this.userRole === UserRole.Admin
      ? this.certificatesService.getAllCertificates(this.searchTerm, this.selectedStatus)
      : this.certificatesService.getMyCertificates();

    obs.pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
         this.allRequests = res.requests;
         this.applyFilters();
         this.isLoading = false;
         this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'Error al cargar certificados.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  submitRequest(): void {
    if (!this.selectedType) return;
    this.isSubmitting = true;
    this.certificatesService.requestCertificate(this.selectedType).subscribe({
      next: (res) => {
        this.allRequests.unshift(res.request);
        this.applyFilters();
        this.successMsg = 'Solicitud enviada correctamente.';
        this.showForm = false;
        this.selectedType = '';
        this.isSubmitting = false;
        setTimeout(() => this.successMsg = '', 4000);
      },
      error: (err) => {
        this.errorMsg = err.error?.msg || 'Error al enviar la solicitud.';
        this.isSubmitting = false;
      }
    });
  }

  sortBy(column: 'username' | 'certificateType' | 'status' | 'createdAt'): void {

  if (this.sortColumn === column) {

    this.sortDirection =
      this.sortDirection === 'asc'
      ? 'desc'
      : 'asc';

  } else {

    this.sortColumn = column;
    this.sortDirection = 'asc';

  }

  this.requests.sort((a, b) => {

    let valueA: any;
    let valueB: any;

    switch (column) {

      case 'username':
        valueA = a.username?.toLowerCase();
        valueB = b.username?.toLowerCase();
        break;

      case 'certificateType':
        valueA = a.certificateType?.toLowerCase();
        valueB = b.certificateType?.toLowerCase();
        break;

      case 'status':
        valueA = a.status?.toLowerCase();
        valueB = b.status?.toLowerCase();
        break;

      case 'createdAt':
        valueA = new Date(a.createdAt).getTime();
        valueB = new Date(b.createdAt).getTime();
        break;

    }

    if (valueA < valueB)
      return this.sortDirection === 'asc' ? -1 : 1;

    if (valueA > valueB)
      return this.sortDirection === 'asc' ? 1 : -1;

    return 0;

  });

  this.requests = [...this.requests];

this.cdr.detectChanges();
}

openMyRequests(): void {

    this.showMyRequests = true;
    this.selectedStatus = null;
    this.searchTerm = '';
    this.loadRequests();

}

closeMyRequests(): void {

    this.showMyRequests = false;
    this.selectedStatus = null;
    this.requests = [];
    this.allRequests = [];

}

approveRequest(request: CertificateRequest): void {

    console.log('Aprobar', request);

    // Aquí luego llamaremos al endpoint
    //this.certificatesService
    //    .approveCertificate(request.id)
    //    .subscribe({
    //        next:()=>{
    //        this.loadRequests();
    //      }
    //  });

}

rejectRequest(request: CertificateRequest): void {

    console.log('Rechazar', request);

    // Aquí luego llamaremos al endpoint
    //this.certificatesService
    //    .rejectCertificate(request.id)
    //    .subscribe({
    //      next:()=>{
    //          this.loadRequests();
    //      }
    //  });

}

private applyFilters(): void {

    this.requests = [...this.allRequests];

    if (this.selectedStatus) {

        this.requests = this.requests.filter(
            x => x.status === this.selectedStatus
        );

    }

}

}
