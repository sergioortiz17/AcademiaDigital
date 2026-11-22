import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Campus, CareerService } from '../../../core/services/career.service';

/**
 * Sedes (escuelas) del instituto, dentro de Gestión de Carreras. Cada división se asigna a una
 * sede, y en Asignar materia se elige la sede para ver solo sus divisiones y comisiones.
 */
@Component({
  selector: 'app-campus-management',
  templateUrl: './campus-management.component.html',
  styleUrls: ['./campus-management.component.scss'],
  standalone: false
})
export class CampusManagementComponent implements OnInit {
  campuses: Campus[] = [];
  /** Sede en edición (null = alta de una nueva). */
  editingId: number | null = null;
  name = '';
  address = '';
  isSaving = false;
  errorMsg = '';
  successMsg = '';

  constructor(private readonly careerService: CareerService, private readonly cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.careerService.getCampuses(true).subscribe({
      next: campuses => { this.campuses = campuses; this.cdr.detectChanges(); },
      error: err => this.fail(err, 'No se pudieron cargar las sedes.')
    });
  }

  edit(campus: Campus): void {
    this.editingId = campus.id;
    this.name = campus.name;
    this.address = campus.address ?? '';
    this.clearMessages();
  }

  cancel(): void {
    this.editingId = null;
    this.name = '';
    this.address = '';
  }

  save(): void {
    if (!this.name.trim() || this.isSaving) return;
    this.isSaving = true;
    this.clearMessages();
    const isNew = this.editingId == null;
    this.careerService.saveCampus(this.editingId, { name: this.name.trim(), address: this.address.trim() || null }).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMsg = isNew ? 'Sede creada.' : 'Sede actualizada.';
        this.cancel();
        this.load();
      },
      error: err => { this.isSaving = false; this.fail(err, 'No se pudo guardar la sede.'); }
    });
  }

  deactivate(campus: Campus): void {
    if (!confirm(`¿Dar de baja la sede "${campus.name}"?`)) return;
    this.clearMessages();
    this.careerService.deactivateCampus(campus.id).subscribe({
      next: () => { this.successMsg = `Sede ${campus.name} dada de baja.`; this.load(); },
      error: err => this.fail(err, 'No se pudo dar de baja la sede.')
    });
  }

  private clearMessages(): void { this.errorMsg = ''; this.successMsg = ''; }

  private fail(err: any, fallback: string): void {
    this.errorMsg = err?.error?.detail || err?.error?.msg || err?.error?.title || fallback;
    this.cdr.detectChanges();
  }
}
