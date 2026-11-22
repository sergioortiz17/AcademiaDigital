import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Career, CareerService } from '../../../core/services/career.service';
import {
  ReportsService,
  StudentReportFilter,
  StudentReportKind,
  StudentReportRow,
  StudentStatusFilter
} from '../../../core/services/reports.service';
import { downloadExcel, ExcelColumn } from '../../../core/utils/excel';

const COLUMNS: ExcelColumn<StudentReportRow>[] = [
  { header: 'Legajo', value: r => r.legajoNumber, width: 12 },
  { header: 'Apellido', value: r => r.lastName, width: 20 },
  { header: 'Nombre', value: r => r.firstName, width: 20 },
  { header: 'DNI', value: r => r.dni, width: 12 },
  { header: 'CUIL', value: r => r.cuil, width: 15 },
  { header: 'Sexo', value: r => r.gender, width: 7 },
  { header: 'Fecha de nacimiento', value: r => formatDate(r.birthDate), width: 14 },
  { header: 'Correo', value: r => r.email, width: 28 },
  { header: 'Teléfono', value: r => r.phone, width: 14 },
  { header: 'Domicilio', value: r => r.addressLine, width: 26 },
  { header: 'Ciudad', value: r => r.city },
  { header: 'Provincia', value: r => r.province },
  { header: 'Código postal', value: r => r.postalCode, width: 10 },
  { header: 'Carrera', value: r => r.careerName, width: 26 },
  { header: 'Estado', value: r => r.status, width: 11 },
  { header: 'Fecha de prematrícula', value: r => formatDate(r.registeredOn), width: 14 },
  { header: 'Fecha de matrícula', value: r => formatDate(r.enrolledOn), width: 14 },
  { header: 'Fecha de egreso', value: r => formatDate(r.graduatedOn), width: 14 },
  { header: 'Contacto de emergencia', value: r => r.emergencyContactName, width: 22 },
  { header: 'Teléfono de emergencia', value: r => r.emergencyContactPhone, width: 16 },
  { header: 'Obra social', value: r => r.healthInsurance },
  { header: 'Libro matriz', value: r => r.registryBook, width: 10 },
  { header: 'Folio', value: r => r.registryFolio, width: 8 }
];

function formatDate(value: string | null): string {
  if (!value) return '';
  const [y, m, d] = value.slice(0, 10).split('-');
  return `${d}/${m}/${y}`;
}

/** Listados de alumnos, egresados y aspirantes para descargar en Excel. */
@Component({
  selector: 'app-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
  standalone: false
})
export class ReportsComponent implements OnInit {
  careers: Career[] = [];
  filter: StudentReportFilter = { kind: 'Students', status: null, careerId: null, from: null, to: null };
  rows: StudentReportRow[] = [];
  loaded = false;
  isLoading = false;
  errorMsg = '';

  readonly kinds: { value: StudentReportKind; label: string }[] = [
    { value: 'Students', label: 'Alumnos' },
    { value: 'Applicants', label: 'Aspirantes (prematriculados)' }
  ];
  readonly statuses: { value: StudentStatusFilter | null; label: string }[] = [
    { value: null, label: 'Todos' },
    { value: 'Regular', label: 'Activos (regulares)' },
    { value: 'Libre', label: 'Libres' },
    { value: 'Graduated', label: 'Egresados' },
    { value: 'Withdrawn', label: 'Bajas' }
  ];
  readonly previewColumns = COLUMNS.slice(0, 6).concat(COLUMNS.filter(c => ['Carrera', 'Estado'].includes(c.header)));

  constructor(
    private readonly reports: ReportsService,
    private readonly careerService: CareerService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe(careers => {
      this.careers = careers;
      this.cdr.markForCheck();
    });
  }

  get dateLabel(): string {
    if (this.filter.kind === 'Applicants') return 'Fecha de prematrícula';
    return this.filter.status === 'Graduated' ? 'Fecha de egreso' : 'Fecha de matrícula';
  }

  search(): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.reports.getStudents(this.filter).subscribe({
      next: rows => {
        this.rows = rows;
        this.loaded = true;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: err => {
        this.errorMsg = err?.message || 'No se pudo armar el listado.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  async download(): Promise<void> {
    const kind = this.kinds.find(k => k.value === this.filter.kind)?.label ?? 'Alumnos';
    const status = this.filter.kind === 'Students' ? this.statuses.find(s => s.value === this.filter.status)?.label : null;
    const career = this.careers.find(c => c.id === this.filter.careerId)?.name;
    const title = [kind, status, career].filter(Boolean).join(' · ');
    const today = new Date().toISOString().slice(0, 10);
    await downloadExcel(`${kind.split(' ')[0].toLowerCase()}_${today}.xlsx`, kind, title, COLUMNS, this.rows);
  }

  cell(row: StudentReportRow, column: ExcelColumn<StudentReportRow>): string {
    const value = column.value(row);
    return value == null || value === '' ? '—' : String(value);
  }
}
