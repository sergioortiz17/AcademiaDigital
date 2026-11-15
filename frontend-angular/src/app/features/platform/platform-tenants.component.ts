import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { PlatformService, TenantSummary } from '../../core/services/platform.service';
import { DEFAULT_TENANT_SLUG } from '../../core/services/tenant.service';

/**
 * Alta de instituciones (multitenant). Pantalla de operador de la plataforma: no pertenece a
 * ninguna institución ni usa la sesión de un usuario; se desbloquea con la clave de plataforma.
 * Crear una institución provisiona su base de datos, su administrador y sus dominios.
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

  toggleForm(): void {
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
        this.resetForm();
        this.showForm = false;
        this.load();
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
    this.slug = this.name = this.domainsText = this.teacherEmailDomain = '';
    this.adminEmail = this.adminName = this.adminLastName = this.adminDni = this.adminPassword = '';
  }
}
