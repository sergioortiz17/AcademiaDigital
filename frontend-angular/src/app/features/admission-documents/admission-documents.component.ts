import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import {
  AdmissionDocumentsService,
  DocumentRequirement,
  StudentDocument,
  documentStatusLabel,
  saveBlob
} from '../../core/services/admission-documents.service';
import { environment } from '../../../environments/environment';

interface RequirementRow {
  requirement: DocumentRequirement;
  /** Último documento enviado para el requisito (el resto quedó reemplazado). */
  latest: StudentDocument | null;
}

const MAX_SIZE_BYTES = 10 * 1024 * 1024;
const ACCEPTED_TYPES = ['application/pdf', 'image/jpeg', 'image/png'];

/** "Requisitos de ingreso": el alumno sube el analítico y los demás documentos que pide la institución. */
@Component({
  selector: 'app-admission-documents',
  templateUrl: './admission-documents.component.html',
  styleUrls: ['./admission-documents.component.scss'],
  standalone: false
})
export class AdmissionDocumentsComponent implements OnInit {
  rows: RequirementRow[] = [];
  isLoading = true;
  uploadingId: number | null = null;
  errorMsg = '';
  successMsg = '';
  readonly preEnrollmentUrl = environment.preEnrollmentUrl;
  readonly statusLabel = documentStatusLabel;

  constructor(
    private readonly service: AdmissionDocumentsService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.service.getMine().subscribe({
      next: data => {
        this.rows = data.requirements.map(requirement => ({
          requirement,
          latest: data.documents
            .filter(d => d.documentRequirementId === requirement.id && d.status !== 'Expired')
            .sort((a, b) => b.submittedAt.localeCompare(a.submittedAt))[0] ?? null
        }));
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: err => {
        this.errorMsg = err?.message || 'No se pudieron cargar los requisitos de ingreso.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  get approvedCount(): number {
    return this.rows.filter(r => r.latest?.status === 'Approved').length;
  }

  /** Se puede subir (o volver a subir) mientras no esté aprobado. */
  canUpload(row: RequirementRow): boolean {
    return row.latest?.status !== 'Approved';
  }

  onFileSelected(row: RequirementRow, input: HTMLInputElement): void {
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.errorMsg = '';
    if (!ACCEPTED_TYPES.includes(file.type)) {
      this.errorMsg = 'El archivo debe ser PDF, JPG o PNG.';
      return;
    }
    if (file.size > MAX_SIZE_BYTES) {
      this.errorMsg = 'El archivo debe pesar hasta 10 MB.';
      return;
    }
    this.uploadingId = row.requirement.id;
    this.service.upload(row.requirement.id, file).subscribe({
      next: () => {
        this.uploadingId = null;
        this.successMsg = `"${row.requirement.name}" se envió. Administración lo va a revisar.`;
        this.load();
        setTimeout(() => { this.successMsg = ''; this.cdr.markForCheck(); }, 5000);
      },
      error: err => {
        this.uploadingId = null;
        this.errorMsg = err?.message || 'No se pudo subir el archivo.';
        this.cdr.markForCheck();
      }
    });
  }

  download(document: StudentDocument): void {
    this.service.downloadMine(document.id).subscribe({
      next: blob => saveBlob(blob, document.originalFileName),
      error: err => {
        this.errorMsg = err?.message || 'No se pudo descargar el archivo.';
        this.cdr.markForCheck();
      }
    });
  }
}
