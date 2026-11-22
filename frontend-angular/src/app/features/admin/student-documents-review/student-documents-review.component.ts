import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import Swal from 'sweetalert2';
import {
  AdmissionDocumentsService,
  StudentDocumentReviewItem,
  StudentDocumentStatus,
  documentStatusLabel,
  saveBlob
} from '../../../core/services/admission-documents.service';

/** Bandeja de Administración para revisar los requisitos de ingreso que suben los alumnos. */
@Component({
  selector: 'app-student-documents-review',
  templateUrl: './student-documents-review.component.html',
  styleUrls: ['./student-documents-review.component.scss'],
  standalone: false
})
export class StudentDocumentsReviewComponent implements OnInit {
  items: StudentDocumentReviewItem[] = [];
  status: StudentDocumentStatus | null = 'Submitted';
  searchTerm = '';
  isLoading = false;
  processingId: number | null = null;
  errorMsg = '';
  readonly statusLabel = documentStatusLabel;
  readonly statuses: { value: StudentDocumentStatus | null; label: string }[] = [
    { value: 'Submitted', label: 'En revisión' },
    { value: 'Approved', label: 'Aprobados' },
    { value: 'Rejected', label: 'Rechazados' },
    { value: null, label: 'Todos' }
  ];

  constructor(
    private readonly service: AdmissionDocumentsService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.service.getForReview(this.status).subscribe({
      next: items => {
        this.items = items;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: err => {
        this.errorMsg = err?.message || 'No se pudieron cargar los documentos.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  get filteredItems(): StudentDocumentReviewItem[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.items;
    return this.items.filter(item =>
      item.studentName.toLowerCase().includes(term)
      || (item.legajoNumber ?? '').toLowerCase().includes(term)
      || (item.dni ?? '').includes(term));
  }

  download(item: StudentDocumentReviewItem): void {
    this.service.download(item.document.studentId, item.document.id).subscribe({
      next: blob => saveBlob(blob, item.document.originalFileName),
      error: err => {
        this.errorMsg = err?.message || 'No se pudo descargar el archivo.';
        this.cdr.markForCheck();
      }
    });
  }

  approve(item: StudentDocumentReviewItem): void {
    this.review(item, 'Approved', null);
  }

  async reject(item: StudentDocumentReviewItem): Promise<void> {
    const result = await Swal.fire({
      titleText: 'Rechazar documento',
      text: `Indique a ${item.studentName} qué tiene que corregir.`,
      input: 'textarea',
      inputPlaceholder: 'Ej: el analítico no se lee, vuelva a escanearlo.',
      showCancelButton: true,
      confirmButtonText: 'Rechazar',
      cancelButtonText: 'Cancelar',
      inputValidator: value => (value?.trim().length ?? 0) < 3 ? 'Escriba el motivo (al menos 3 caracteres).' : null
    });
    if (result.isConfirmed) this.review(item, 'Rejected', String(result.value).trim());
  }

  private review(item: StudentDocumentReviewItem, status: 'Approved' | 'Rejected', observation: string | null): void {
    this.processingId = item.document.id;
    this.service.review(item.document.studentId, item.document.id, status, observation).subscribe({
      next: () => {
        this.processingId = null;
        this.load();
      },
      error: err => {
        this.processingId = null;
        this.errorMsg = err?.message || 'No se pudo guardar la revisión.';
        this.cdr.markForCheck();
      }
    });
  }
}
