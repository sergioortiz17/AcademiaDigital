import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { PlatformService } from '../../core/services/platform.service';
import { siteBaseDomain } from '../../core/routing/platform-hosts';
import { LottieAnimationComponent } from './lottie-animation.component';

interface Benefit {
  icon: string;
  title: string;
  text: string;
}

/**
 * Landing comercial de Academia Digital (academiadigital.<dominio>). Corta, centrada en el valor
 * para la institución, con el formulario "Pide tu plataforma". Los pedidos llegan al panel del
 * operador (admin.<dominio>) y por correo.
 */
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule, LottieAnimationComponent],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent {
  readonly baseDomain = siteBaseDomain(location.hostname);
  readonly year = new Date().getFullYear();

  readonly benefits: Benefit[] = [
    { icon: 'how_to_reg', title: 'Inscripciones en línea',
      text: 'Materias y mesas de examen sin filas ni planillas. Cada alumno se inscribe desde su cuenta.' },
    { icon: 'grading', title: 'Calificaciones y asistencias',
      text: 'Los docentes cargan notas y asistencias; los alumnos las consultan al instante.' },
    { icon: 'workspace_premium', title: 'Certificados digitales',
      text: 'Pedidos y emisión de constancias y certificados, sin papeles de por medio.' },
    { icon: 'domain', title: 'Tu propio espacio',
      text: 'Un subdominio con el nombre de tu institución y tus datos separados de los de las demás.' },
    { icon: 'verified_user', title: 'Altas bajo tu control',
      text: 'Tu equipo aprueba cada cuenta. Los docentes se reconocen por el correo institucional.' },
    { icon: 'cloud_done', title: 'En la nube, sin servidores propios',
      text: 'Funciona en AWS con conexión segura (HTTPS) y respaldos diarios. Nada que instalar.' }
  ];

  readonly steps = [
    { title: 'Pides tu plataforma', text: 'Completas el formulario con los datos de tu institución.' },
    { title: 'La configuramos', text: 'Creamos tu espacio y el usuario administrador.' },
    { title: 'Tu equipo empieza', text: 'Cargan carreras, docentes y alumnos, y listo.' }
  ];

  form: FormGroup;
  sending = false;
  sent = false;
  errorMsg = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly platform: PlatformService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      institutionName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
      requestedSlug: ['', [Validators.pattern(/^[a-z0-9]([a-z0-9-]{0,28}[a-z0-9])?$/)]],
      contactName: ['', [Validators.required, Validators.maxLength(200)]],
      contactEmail: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
      contactPhone: ['', [Validators.maxLength(40)]],
      location: ['', [Validators.maxLength(200)]],
      studentsEstimate: [null, [Validators.min(0), Validators.max(1000000)]],
      message: ['', [Validators.maxLength(2000)]],
      website: ['']
    });
  }

  get slugPreview(): string {
    const slug = (this.form.get('requestedSlug')?.value as string | null)?.trim();
    return `${slug || 'tuinstitucion'}.${this.baseDomain}`;
  }

  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  invalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.sending = true;
    this.errorMsg = '';

    const v = this.form.value;
    const text = (value: string | null | undefined) => value?.trim() || null;
    this.platform.submitRequest({
      institutionName: v.institutionName.trim(),
      requestedSlug: text(v.requestedSlug)?.toLowerCase() ?? null,
      contactName: v.contactName.trim(),
      contactEmail: v.contactEmail.trim(),
      contactPhone: text(v.contactPhone),
      location: text(v.location),
      studentsEstimate: v.studentsEstimate === null || v.studentsEstimate === '' ? null : Number(v.studentsEstimate),
      message: text(v.message),
      website: text(v.website)
    }).subscribe({
      next: () => {
        this.sending = false;
        this.sent = true;
        this.cdr.detectChanges();
      },
      error: err => {
        this.sending = false;
        this.errorMsg = err?.status === 429
          ? 'Recibimos varios pedidos desde tu conexión. Intenta de nuevo en un rato.'
          : err?.error?.msg || 'No pudimos enviar el pedido. Intenta de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }
}
