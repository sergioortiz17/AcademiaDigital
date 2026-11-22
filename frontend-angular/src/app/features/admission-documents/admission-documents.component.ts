import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
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

/** Archivo elegido o documento enviado que se muestra en pantalla antes de enviar o al tocar "Ver". */
interface Preview {
  name: string;
  sizeLabel: string;
  url: string;
  safeUrl: SafeResourceUrl;
  isPdf: boolean;
  /** Solo en el archivo elegido: lo que se envía al tocar "Enviar". */
  file?: File;
}

const MAX_SIZE_BYTES = 5 * 1024 * 1024;
const ACCEPTED_TYPES = ['application/pdf', 'image/jpeg', 'image/png'];

/** "Requisitos de ingreso": el alumno sube el analítico y los demás documentos que pide la institución. */
@Component({
  selector: 'app-admission-documents',
  templateUrl: './admission-documents.component.html',
  styleUrls: ['./admission-documents.component.scss'],
  standalone: false
})
export class AdmissionDocumentsComponent implements OnInit, OnDestroy {
  rows: RequirementRow[] = [];
  isLoading = true;
  uploadingId: number | null = null;
  errorMsg = '';
  successMsg = '';
  readonly preEnrollmentUrl = environment.preEnrollmentUrl;
  readonly statusLabel = documentStatusLabel;

  /** Archivo elegido por requisito, todavía sin enviar (con su vista previa). */
  pending: Record<number, Preview> = {};
  /** Documento ya enviado que se está viendo, por requisito. */
  viewing: Record<number, Preview> = {};
  loadingViewId: number | null = null;

  constructor(
    private readonly service: AdmissionDocumentsService,
    private readonly sanitizer: DomSanitizer,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnDestroy(): void {
    [...Object.values(this.pending), ...Object.values(this.viewing)].forEach(p => URL.revokeObjectURL(p.url));
  }

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

  /** Elegir un archivo no lo envía: muestra la vista previa y espera "Enviar". */
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
      this.errorMsg = 'El archivo debe pesar hasta 5 MB.';
      return;
    }
    this.cancelPending(row);
    this.closeView(row);
    this.pending[row.requirement.id] = { ...this.preview(file, file.name), file };
    this.cdr.markForCheck();
  }

  cancelPending(row: RequirementRow): void {
    const current = this.pending[row.requirement.id];
    if (current) URL.revokeObjectURL(current.url);
    delete this.pending[row.requirement.id];
  }

  send(row: RequirementRow): void {
    const file = this.pending[row.requirement.id]?.file;
    if (!file || this.uploadingId !== null) return;
    this.uploadingId = row.requirement.id;
    this.errorMsg = '';
    this.service.upload(row.requirement.id, file).subscribe({
      next: () => {
        this.uploadingId = null;
        this.cancelPending(row);
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

  /** Muestra en pantalla el documento ya enviado (o lo oculta si ya se estaba viendo). */
  toggleView(row: RequirementRow): void {
    const doc = row.latest;
    if (!doc) return;
    if (this.viewing[row.requirement.id]) { this.closeView(row); return; }
    this.loadingViewId = row.requirement.id;
    this.service.downloadMine(doc.id).subscribe({
      next: blob => {
        this.loadingViewId = null;
        const typed = blob.type ? blob : new Blob([blob], { type: doc.contentType });
        this.viewing[row.requirement.id] = this.preview(typed, doc.originalFileName);
        this.cdr.markForCheck();
      },
      error: err => {
        this.loadingViewId = null;
        this.errorMsg = err?.message || 'No se pudo abrir el archivo.';
        this.cdr.markForCheck();
      }
    });
  }

  closeView(row: RequirementRow): void {
    const current = this.viewing[row.requirement.id];
    if (current) URL.revokeObjectURL(current.url);
    delete this.viewing[row.requirement.id];
  }

  private preview(blob: Blob, name: string): Preview {
    const url = URL.createObjectURL(blob);
    const kb = blob.size / 1024;
    return {
      name,
      sizeLabel: kb >= 1024 ? `${(kb / 1024).toFixed(1).replace('.', ',')} MB` : `${Math.max(1, Math.round(kb))} KB`,
      url,
      // Es un blob local creado en este navegador a partir del archivo del propio usuario.
      safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url),
      isPdf: blob.type === 'application/pdf'
    };
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
