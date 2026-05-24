import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { CertificatesService, CertificateRequest, CERTIFICATE_TYPES } from '../../core/services/certificates.service';
import { selectUserRole } from '../../store/account/account.selectors';
import { UserRole } from '../../store/account/account.actions';

@Component({
  selector: 'app-certificates',
  templateUrl: './certificates.component.html',
  styleUrls: ['./certificates.component.scss'],
  standalone: false
})
export class CertificatesComponent implements OnInit {
  requests: CertificateRequest[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMsg = '';
  successMsg = '';
  showForm = false;
  selectedType = '';
  certificateTypes = CERTIFICATE_TYPES;
  UserRole = UserRole;
  userRole: UserRole | null = null;

  displayedColumns = ['certificateType', 'status', 'createdAt'];

  statusLabels: Record<string, string> = {
    Pending: 'Pendiente',
    Approved: 'Aprobado',
    Rejected: 'Rechazado'
  };

  constructor(
    private readonly certificatesService: CertificatesService,
    private readonly store: Store
  ) {}

  ngOnInit(): void {
    this.store.select(selectUserRole).subscribe(role => {
      this.userRole = role as UserRole;
      this.loadRequests();
    });
  }

  loadRequests(): void {
    this.isLoading = true;
    const obs = this.userRole === UserRole.Admin
      ? this.certificatesService.getAllCertificates()
      : this.certificatesService.getMyCertificates();

    obs.subscribe({
      next: (res) => { this.requests = res.requests; this.isLoading = false; },
      error: () => { this.errorMsg = 'Error al cargar certificados.'; this.isLoading = false; }
    });
  }

  submitRequest(): void {
    if (!this.selectedType) return;
    this.isSubmitting = true;
    this.certificatesService.requestCertificate(this.selectedType).subscribe({
      next: (res) => {
        this.requests.unshift(res.request);
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
}
