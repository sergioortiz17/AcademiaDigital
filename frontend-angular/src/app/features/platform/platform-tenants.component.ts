import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import {
  PlatformRequestItem, PlatformRequestStatus, PlatformService, TenantSummary
} from '../../core/services/platform.service';
import { siteBaseDomain } from '../../core/routing/platform-hosts';
import { DEFAULT_TENANT_SLUG } from '../../core/services/tenant.service';

/**
 * Alta de instituciones (multitenant). Pantalla de operador de la plataforma: no pertenece a
 * ninguna institución ni usa la sesión de un usuario; se desbloquea con la clave de plataforma.
 * Crear una institución provisiona su esquema en la base compartida, su administrador y sus
 * dominios. También muestra los pedidos del formulario "Pide tu plataforma" de la landing:
 * "Crear institución" precarga el alta y, al crearla, el pedido queda aprobado.
 */
@Component({
  selector: 'app-platform-tenants',
  templateUrl: './platform-tenants.component.html',
  styleUrls: ['./platform-tenants.component.scss'],
  standalone: false
})
export class PlatformTenantsComponent implements OnInit {
  readonly defaultSlug = DEFAULT_TENANT_SLUG;

  unlocked = false;
  keyInput = '';
  hideKey = true;

  tenants: TenantSummary[] = [];
  requests: PlatformRequestItem[] = [];
  showReviewed = false;
  readonly RequestStatus = PlatformRequestStatus;
  /** Pedido que originó el alta en curso: se aprueba al crear la institución. */
  private sourceRequestId: string | null = null;
  isLoading = false;
  isCreating = false;
  errorMsg = '';
  successMsg = '';
  showForm = false;

  slug = '';
  name = '';
  domainsText = '';
  teacherEmailDomain = '';
  adminEmail = '';
  adminName = '';
  adminLastName = '';
  adminDni = '';
  adminPassword = '';
  hidePassword = true;

  constructor(
    private readonly platform: PlatformService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (this.platform.hasKey) {
      this.unlocked = true;
      this.load();
    }
  }

  unlock(): void {
    if (!this.keyInput.trim()) return;
    this.platform.setKey(this.keyInput);
    this.keyInput = '';
    this.errorMsg = '';
    this.load(true);
  }

  lock(): void {
    this.platform.clearKey();
    this.unlocked = false;
    this.tenants = [];
    this.showForm = false;
  }

  get domains(): string[] {
    return this.domainsText
      .split(/[\s,;]+/)
      .map(d => d.trim())
      .filter(d => d.length > 0);
  }

  get isFormValid(): boolean {
    return !!(
      this.slug.trim() && this.name.trim() && this.domains.length &&
      this.adminEmail.trim() && this.adminName.trim() && this.adminLastName.trim() &&
      this.adminDni.trim() && this.adminPassword.length >= 8
    );
  }

  load(fromUnlock = false): void {
    this.isLoading = true;
    this.platform.list().subscribe({
      next: tenants => {
        this.tenants = tenants;
        this.unlocked = true;
        this.isLoading = false;
        this.loadRequests();
        this.cdr.detectChanges();
      },
      error: err => {
        this.isLoading = false;
        if (err.status === 401 || err.status === 404) {
          // 401: clave incorrecta. 404: la API de plataforma está deshabilitada en el servidor.
          this.platform.clearKey();
          this.unlocked = false;
          this.errorMsg = err.status === 401
            ? 'Clave de plataforma incorrecta.'
            : 'La administración de instituciones no está habilitada en este servidor.';
        } else if (!fromUnlock) {
          this.errorMsg = err.message || 'No se pudo cargar el listado.';
        } else {
          this.errorMsg = err.message;
        }
        this.cdr.detectChanges();
      }
    });
  }

  get visibleRequests(): PlatformRequestItem[] {
    return this.showReviewed ? this.requests : this.requests.filter(r => r.status === PlatformRequestStatus.Pending);
  }

  get pendingCount(): number {
    return this.requests.filter(r => r.status === PlatformRequestStatus.Pending).length;
  }

  requestStatusLabel(status: PlatformRequestStatus): string {
    return status === PlatformRequestStatus.Approved ? 'Aprobado'
      : status === PlatformRequestStatus.Rejected ? 'Rechazado' : 'Pendiente';
  }

  loadRequests(): void {
    this.platform.listRequests().subscribe({
      next: requests => { this.requests = requests; this.cdr.detectChanges(); },
      error: () => { this.errorMsg = 'No se pudieron cargar los pedidos.'; this.cdr.detectChanges(); }
    });
  }

  /** Precarga el alta con los datos del pedido: subdominio, nombre, dominio y admin inicial. */
  createFromRequest(request: PlatformRequestItem): void {
    const slug = request.requestedSlug || this.slugify(request.institutionName);
    const [first, ...rest] = request.contactName.trim().split(/\s+/);
    this.resetForm();
    this.slug = slug;
    this.name = request.institutionName;
    this.domainsText = `${slug}.${siteBaseDomain(location.hostname)}`;
    this.adminEmail = request.contactEmail;
    this.adminName = first ?? '';
    this.adminLastName = rest.join(' ');
    this.sourceRequestId = request.id;
    this.showForm = true;
    this.errorMsg = '';
    this.successMsg = `Datos precargados del pedido de "${request.institutionName}". Completa el DNI y la contraseña del administrador.`;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  rejectRequest(request: PlatformRequestItem): void {
    if (!confirm(`¿Rechazar el pedido de "${request.institutionName}"?`)) return;
    this.platform.reviewRequest(request.id, PlatformRequestStatus.Rejected).subscribe({
      next: () => { this.successMsg = `Pedido de "${request.institutionName}" rechazado.`; this.loadRequests(); },
      error: err => { this.errorMsg = err?.error?.msg || 'No se pudo rechazar el pedido.'; this.cdr.detectChanges(); }
    });
  }

  private slugify(name: string): string {
    return name.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()
      .replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 30).replace(/-+$/g, '');
  }

  toggleForm(): void {
    if (this.showForm) this.sourceRequestId = null;
    this.showForm = !this.showForm;
    this.errorMsg = '';
    this.successMsg = '';
  }

  create(): void {
    if (!this.isFormValid) {
      this.errorMsg = 'Completá todos los campos. La contraseña debe tener al menos 8 caracteres.';
      return;
    }
    this.isCreating = true;
    this.errorMsg = '';
    this.successMsg = '';

    this.platform.create({
      slug: this.slug.trim(),
      name: this.name.trim(),
      domains: this.domains,
      adminEmail: this.adminEmail.trim(),
      adminName: this.adminName.trim(),
      adminLastName: this.adminLastName.trim(),
      adminDni: this.adminDni.trim(),
      adminPassword: this.adminPassword,
      teacherEmailDomain: this.teacherEmailDomain.trim() || null
    }).subscribe({
      next: tenant => {
        this.isCreating = false;
        this.successMsg = `Institución "${tenant.name}" creada. Ya se puede ingresar desde: ${tenant.domains.join(', ')}.`;
        const requestId = this.sourceRequestId;
        this.resetForm();
        this.showForm = false;
        if (requestId) {
          this.platform.reviewRequest(requestId, PlatformRequestStatus.Approved, tenant.slug).subscribe({
            next: () => this.load(),
            error: () => {
              this.errorMsg = 'La institución se creó, pero no se pudo marcar el pedido como aprobado.';
              this.load();
            }
          });
        } else {
          this.load();
        }
      },
      error: err => {
        this.isCreating = false;
        if (err.status === 401) {
          this.lock();
          this.errorMsg = 'Clave de plataforma incorrecta.';
        } else {
          this.errorMsg = err.message || 'No se pudo crear la institución.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  private resetForm(): void {
    this.sourceRequestId = null;
    this.slug = this.name = this.domainsText = this.teacherEmailDomain = '';
    this.adminEmail = this.adminName = this.adminLastName = this.adminDni = this.adminPassword = '';
  }
}
