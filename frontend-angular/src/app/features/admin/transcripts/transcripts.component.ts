import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { Career, CareerService } from '../../../core/services/career.service';
import { StudentListItem, StudentService } from '../../../core/services/student.service';
import { Transcript, TranscriptCourse, TranscriptsService } from '../../../core/services/transcripts.service';
import { TenantService } from '../../../core/services/tenant.service';

const esc = (value: unknown) => String(value ?? '').replace(/[&<>"']/g, ch => (
  { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' } as Record<string, string>)[ch]);
const date = (value: string | null) => value ? value.slice(0, 10).split('-').reverse().join('/') : '';

/**
 * Analíticos: Administración elige al alumno, revisa sus datos y materias, completa libro matriz,
 * folio y el libro/folio de cada acta, y lo imprime (o guarda como PDF).
 */
@Component({
  selector: 'app-transcripts',
  templateUrl: './transcripts.component.html',
  styleUrls: ['./transcripts.component.scss'],
  standalone: false
})
export class TranscriptsComponent implements OnInit {
  careers: Career[] = [];
  careerId: number | null = null;
  students: StudentListItem[] = [];
  search = '';
  transcript: Transcript | null = null;
  registryBook = '';
  registryFolio = '';
  acts: Record<number, { book: string; folio: string }> = {};
  isLoading = false;
  saving = false;
  errorMsg = '';
  successMsg = '';
  private institutionName = 'Instituto Técnico Superior Córdoba';

  constructor(
    private readonly careerService: CareerService,
    private readonly studentService: StudentService,
    private readonly transcripts: TranscriptsService,
    private readonly tenantService: TenantService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.careerService.getCareers().subscribe(careers => { this.careers = careers; this.cdr.markForCheck(); });
    this.tenantService.tenant$.subscribe(t => { if (t?.name) this.institutionName = t.name; });
  }

  loadStudents(): void {
    this.transcript = null;
    if (this.careerId == null) return;
    this.studentService.getAllStudentsOfCareer(this.careerId).subscribe(students => {
      this.students = students.sort((a, b) => a.fullName.localeCompare(b.fullName, 'es'));
      this.cdr.markForCheck();
    });
  }

  get visibleStudents(): StudentListItem[] {
    const term = this.search.trim().toLowerCase();
    return this.students.filter(s => !term || s.fullName.toLowerCase().includes(term)
      || (s.legajoNumber ?? '').includes(term) || (s.dni ?? '').includes(term)).slice(0, 100);
  }

  open(student: StudentListItem): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.transcripts.get(student.id).subscribe({
      next: t => {
        this.transcript = t;
        this.registryBook = t.student.registryBook ?? '';
        this.registryFolio = t.student.registryFolio ?? '';
        this.acts = {};
        for (const c of t.courses) if (c.examTableId) this.acts[c.examTableId] = { book: c.actBook ?? '', folio: c.actFolio ?? '' };
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: err => { this.errorMsg = err?.message || 'No se pudo cargar el analítico.'; this.isLoading = false; this.cdr.markForCheck(); }
    });
  }

  /** Guarda libro matriz/folio del alumno y libro/folio de cada acta en un solo paso. */
  save(): void {
    const t = this.transcript;
    if (!t) return;
    this.saving = true;
    const calls = [this.transcripts.setRegistry(t.student.studentId, this.registryBook || null, this.registryFolio || null),
      ...Object.entries(this.acts).map(([id, act]) => this.transcripts.setExamAct(Number(id), act.book || null, act.folio || null))];
    forkJoin(calls).subscribe({
      next: () => {
        this.saving = false;
        this.successMsg = 'Datos del analítico guardados.';
        this.open({ id: t.student.studentId } as StudentListItem);
        setTimeout(() => { this.successMsg = ''; this.cdr.markForCheck(); }, 4000);
      },
      error: err => { this.saving = false; this.errorMsg = err?.message || 'No se pudo guardar.'; this.cdr.markForCheck(); }
    });
  }

  get years(): number[] {
    return [...new Set((this.transcript?.courses ?? []).map(c => c.yearNumber))].sort((a, b) => a - b);
  }

  coursesOfYear(year: number): TranscriptCourse[] {
    return (this.transcript?.courses ?? []).filter(c => c.yearNumber === year);
  }

  /** Analítico listo para imprimir o guardar como PDF (formato provisorio hasta tener el modelo de Secretaría). */
  print(): void {
    const t = this.transcript;
    if (!t) return;
    const s = t.student;
    const rows = this.years.map(year => `
      <tr class="year"><td colspan="7">${year}.º año</td></tr>
      ${this.coursesOfYear(year).map(c => `
        <tr><td>${esc(c.courseName)}</td><td>${esc(c.period)}</td><td>${esc(c.condition)}</td>
        <td class="n">${c.finalGrade ?? ''}</td><td>${date(c.approvedOn)}</td>
        <td>${esc(c.examTableId ? this.acts[c.examTableId]?.book : '')}</td><td>${esc(c.examTableId ? this.acts[c.examTableId]?.folio : '')}</td></tr>`).join('')}`).join('');
    const html = `<!DOCTYPE html><html lang="es"><head><meta charset="utf-8"><title>Analítico ${esc(s.lastName)} ${esc(s.firstName)}</title>
      <style>
        @page { size: A4; margin: 14mm; }
        body { font-family: Arial, sans-serif; font-size: 11px; color: #111; }
        h1 { font-size: 16px; text-align: center; margin: 0; } h2 { font-size: 13px; text-align: center; margin: 4px 0 12px; }
        .grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 2px 16px; margin-bottom: 10px; }
        .grid div b { display: inline-block; min-width: 92px; }
        table { width: 100%; border-collapse: collapse; margin-top: 6px; }
        th, td { border: 1px solid #555; padding: 3px 5px; } th { background: #e8e8e8; }
        tr.year td { background: #f3f3f3; font-weight: bold; } td.n { text-align: center; }
        .summary { margin-top: 8px; font-weight: bold; } .signatures { display: flex; justify-content: space-around; margin-top: 50px; }
        .signatures div { border-top: 1px solid #111; width: 200px; text-align: center; padding-top: 4px; }
      </style></head><body>
      <h1>${esc(this.institutionName)}</h1>
      <h2>Certificado analítico</h2>
      <div class="grid">
        <div><b>Apellido y nombre:</b> ${esc(s.lastName)}, ${esc(s.firstName)}</div>
        <div><b>DNI:</b> ${esc(s.dni)}</div><div><b>CUIL:</b> ${esc(s.cuil)}</div>
        <div><b>Sexo:</b> ${esc(s.gender)}</div><div><b>Nacimiento:</b> ${date(s.birthDate)}</div>
        <div><b>Teléfono:</b> ${esc(s.phone)}</div>
        <div><b>Domicilio:</b> ${esc([s.addressLine, s.city, s.province].filter(Boolean).join(', '))}</div>
        <div><b>Carrera:</b> ${esc(s.careerName)}</div><div><b>Plan:</b> ${esc(s.studyPlanName)}</div>
        <div><b>Legajo:</b> ${esc(s.legajoNumber)}</div><div><b>Libro matriz:</b> ${esc(this.registryBook)}</div>
        <div><b>Folio:</b> ${esc(this.registryFolio)}</div><div><b>Ingreso:</b> ${s.admissionYear ?? ''}</div>
        <div><b>Estado:</b> ${esc(s.status)}</div><div><b>Egreso:</b> ${date(s.graduatedOn)}</div>
      </div>
      <table><thead><tr><th>Materia</th><th>Período</th><th>Condición</th><th>Nota</th><th>Fecha</th><th>Libro</th><th>Folio</th></tr></thead>
      <tbody>${rows}</tbody></table>
      <p class="summary">Materias aprobadas: ${t.approvedCount} de ${t.courses.length}. Promedio: ${t.average ?? '—'}.</p>
      <p>Se extiende el presente a pedido del interesado, el ${new Date().toLocaleDateString('es-AR')}.</p>
      <div class="signatures"><div>Secretaría Académica</div><div>Dirección</div></div>
      </body></html>`;
    const url = URL.createObjectURL(new Blob([html], { type: 'text/html;charset=utf-8' }));
    const win = window.open(url, '_blank');
    win?.addEventListener('load', () => { win.print(); URL.revokeObjectURL(url); });
  }
}
