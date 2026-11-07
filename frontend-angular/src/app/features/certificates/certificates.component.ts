import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { forkJoin, of, Subject } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, take, takeUntil } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { CertificatesService, CertificateRequest, CERTIFICATE_TYPES } from '../../core/services/certificates.service';
import { PersonNameLookupService } from '../../core/services/person-name-lookup.service';
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

  displayedColumnsAlumno = ['certificateType', 'status', 'createdAt', 'actions'];
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

  processingIds = new Set<number>();
  downloadingIds = new Set<number>();

  /** Indica si la fila tiene un certificado emitido y listo para descargar. */
  canDownload(request: CertificateRequest): boolean {
    return request.status === 'Approved'
      && !!request.issuance
      && request.issuance.status === 'Ready'
      && !!request.issuance.id;
  }

  downloadCertificate(request: CertificateRequest): void {
    if (!this.canDownload(request) || !request.issuance) return;
    if (this.downloadingIds.has(request.id)) return;

    const issuance = request.issuance;
    this.downloadingIds.add(request.id);
    this.errorMsg = '';
    this.cdr.detectChanges();

    this.certificatesService.downloadCertificate(issuance.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = issuance.fileName || `${issuance.certificateNumber || 'certificado'}.pdf`;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        URL.revokeObjectURL(url);
        this.downloadingIds.delete(request.id);
        this.cdr.detectChanges();
      },
      error: () => {
        this.downloadingIds.delete(request.id);
        this.errorMsg = 'No se pudo descargar el certificado. Intente nuevamente.';
        this.cdr.detectChanges();
      }
    });
  }

  constructor(
    private readonly certificatesService: CertificatesService,
    private readonly store: Store,
    private readonly cdr: ChangeDetectorRef,
    private readonly personNames: PersonNameLookupService
  ) {}

  ngOnInit(): void {
    this.store.select(selectUserRole).pipe(take(1)).subscribe(role => {
      this.userRole = role as UserRole;
      if (this.userRole === UserRole.Admin) {
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

    const names$ = this.userRole === UserRole.Admin
      ? this.personNames.getNamesByUserId().pipe(catchError(() => of(null)))
      : of(null);

    forkJoin({ response: obs, names: names$ }).pipe(takeUntil(this.destroy$)).subscribe({
      next: ({ response, names }) => {
         this.allRequests = response.requests.map(request => ({
           ...request,
           displayName: names?.get(request.userId) || request.username || ''
         }));
         this.applyFilters();
         if (this.userRole === UserRole.Admin && !names) {
           this.errorMsg = 'No se pudieron consultar todos los nombres completos; se muestran los nombres de usuario disponibles.';
         }
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
    if (!this.selectedType || this.isSubmitting) return;
    this.isSubmitting = true;
    this.errorMsg = '';
    this.certificatesService.requestCertificate(this.selectedType).subscribe({
      next: (res) => {
        this.allRequests.unshift(res.request);
        this.applyFilters();
        this.successMsg = 'Solicitud enviada correctamente.';
        //this.showForm = false;
        this.selectedType = '';
        this.isSubmitting = false;
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.errorMsg = err.message || 'Error al enviar la solicitud.';
        this.isSubmitting = false;
        this.cdr.detectChanges();
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
        valueA = (a.displayName || a.username)?.toLowerCase();
        valueB = (b.displayName || b.username)?.toLowerCase();
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

async approveRequest(request: CertificateRequest): Promise<void> {
    if (this.processingIds.has(request.id)) return;
    const confirmation = await Swal.fire({
      icon: 'warning',
      titleText: 'Aprobar solicitud',
      text: `¿Aprobar la solicitud de ${request.certificateType} de ${this.applicantName(request)}?`,
      showCancelButton: true,
      confirmButtonText: 'Aprobar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#198754',
      reverseButtons: true,
      focusCancel: true
    });
    if (!confirmation.isConfirmed) return;

    this.processingIds.add(request.id);
    this.certificatesService.approveCertificate(request.id).subscribe({
      next: (updated) => {
        this.replaceRequest(updated);
        this.processingIds.delete(request.id);
        this.successMsg = 'Solicitud aprobada correctamente.';
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.processingIds.delete(request.id);
        this.errorMsg = err.message || 'Error al aprobar la solicitud.';
        this.cdr.detectChanges();
      }
    });
}

async rejectRequest(request: CertificateRequest): Promise<void> {
    if (this.processingIds.has(request.id)) return;

    const result = await Swal.fire<string>({
      icon: 'warning',
      titleText: 'Rechazar solicitud',
      text: `Ingresá el motivo para rechazar la solicitud de ${request.certificateType} de ${this.applicantName(request)}.`,
      input: 'textarea',
      inputPlaceholder: 'Explicá brevemente por qué se rechaza la solicitud',
      inputAttributes: {
        'aria-label': 'Motivo del rechazo',
        maxlength: '1000',
        rows: '3'
      },
      showCancelButton: true,
      confirmButtonText: 'Rechazar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#dc3545',
      reverseButtons: true,
      focusCancel: true,
      preConfirm: (value) => {
        const reason = value.trim();
        if (reason.length < 3) {
          Swal.showValidationMessage('El motivo debe tener al menos 3 caracteres.');
          return false;
        }
        return reason;
      }
    });
    if (!result.isConfirmed || !result.value) return;

    this.processingIds.add(request.id);
    this.certificatesService.rejectCertificate(request.id, result.value).subscribe({
      next: (updated) => {
        this.replaceRequest(updated);
        this.processingIds.delete(request.id);
        this.successMsg = 'Solicitud rechazada correctamente.';
        this.cdr.detectChanges();
        setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
      },
      error: (err) => {
        this.processingIds.delete(request.id);
        this.errorMsg = err.message || 'Error al rechazar la solicitud.';
        this.cdr.detectChanges();
      }
    });
}

private replaceRequest(updated: CertificateRequest): void {
    this.allRequests = this.allRequests.map(r => r.id === updated.id
      ? { ...updated, displayName: updated.displayName || r.displayName || updated.username || '' }
      : r);
    this.applyFilters();
}

applicantName(request: CertificateRequest): string {
    return request.displayName || request.username || 'Solicitante sin nombre';
}

private applyFilters(): void {

    this.requests = [...this.allRequests];

    if (this.selectedStatus) {

        this.requests = this.requests.filter(
            x => x.status === this.selectedStatus
        );
    }
}

selectCertificate(type: string): void {
  if (this.isSubmitting) {
    return;
  }
  this.selectedType = type;
  this.submitRequest();
}

}
