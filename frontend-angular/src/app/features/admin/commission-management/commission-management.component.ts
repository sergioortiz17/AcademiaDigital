import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { Career, CareerService } from '../../../core/services/career.service';
import { Commission, CommissionService, UpsertCommissionRequest } from '../../../core/services/commission.service';
import { CommissionFormDialogComponent, CommissionFormDialogData } from './commission-form-dialog/commission-form-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

/**
 * Pantalla admin "Comisiones": alta / edición / baja de comisiones REALES por carrera.
 *
 * A diferencia de dev-tools (que auto-crea comisiones "fantasma"), acá el admin las crea
 * con datos reales. Es el prerequisito para poder crear cargos docentes (teaching-positions)
 * sobre comisiones existentes y luego asignarles un profesor.
 *
 * El turno (Shift) se guarda unificado en español (Mañana/Tarde/Noche), igual que Enrollment.Shift.
 *
 * Acepta query params opcionales careerId + academicYear (atajo desde el aviso de cobertura de la
 * Parte 11): preselecciona la carrera y precarga el año al abrir el alta de comisión.
 */
@Component({
  selector: 'app-commission-management',
  templateUrl: './commission-management.component.html',
  styleUrls: ['./commission-management.component.scss'],
  standalone: false
})
export class CommissionManagementComponent implements OnInit {
  careers: Career[] = [];
  commissions: Commission[] = [];

  selectedCareerId: number | null = null;
  private presetAcademicYear: number | null = null;

  isLoading = false;
  errorMsg = '';
  successMsg = '';

  displayedColumns = ['code', 'name', 'academicYear', 'yearNumber', 'shift', 'isActive', 'actions'];

  constructor(
    private readonly careerService: CareerService,
    private readonly commissionService: CommissionService,
    private readonly dialog: MatDialog,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const careerId = qp.get('careerId');
    const academicYear = qp.get('academicYear');
    if (careerId) this.selectedCareerId = Number(careerId);
    if (academicYear) this.presetAcademicYear = Number(academicYear);

    this.careerService.getCareers().subscribe({
      next: (careers) => {
        this.careers = careers;
        // Si vino una carrera por query param, cargar sus comisiones directamente.
        if (this.selectedCareerId && careers.some(c => c.id === this.selectedCareerId)) {
          this.loadCommissions();
        }
        this.cdr.detectChanges();
      },
      error: (err) => this.fail(err, 'Error al cargar las carreras.')
    });
  }

  shiftLabel(shift: string): string {
    return CommissionFormDialogComponent.shiftLabel(shift);
  }

  onCareerChange(): void {
    this.commissions = [];
    this.clearMessages();
    if (!this.selectedCareerId) return;
    this.loadCommissions();
  }

  loadCommissions(): void {
    if (!this.selectedCareerId) return;
    this.isLoading = true;
    this.errorMsg = '';
    this.commissionService.getCommissions(this.selectedCareerId).subscribe({
      next: (commissions) => {
        this.commissions = commissions;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => this.fail(err, 'Error al cargar las comisiones.')
    });
  }

  openCreateDialog(): void {
    if (!this.selectedCareerId) return;
    const data: CommissionFormDialogData = { commission: null, presetAcademicYear: this.presetAcademicYear };
    const dialogRef = this.dialog.open(CommissionFormDialogComponent, {
      width: '520px', maxWidth: '95vw', disableClose: true, data
    });
    dialogRef.afterClosed().subscribe((request: UpsertCommissionRequest | null) => {
      if (!request) return;
      this.commissionService.createCommission(this.selectedCareerId!, request).subscribe({
        next: () => this.succeed('Comisión creada correctamente.'),
        error: (err) => this.fail(err, 'Error al crear la comisión.')
      });
    });
  }

  openEditDialog(commission: Commission): void {
    if (!this.selectedCareerId) return;
    const data: CommissionFormDialogData = { commission };
    const dialogRef = this.dialog.open(CommissionFormDialogComponent, {
      width: '520px', maxWidth: '95vw', disableClose: true, data
    });
    dialogRef.afterClosed().subscribe((request: UpsertCommissionRequest | null) => {
      if (!request) return;
      this.commissionService.updateCommission(this.selectedCareerId!, commission.id, request).subscribe({
        next: () => this.succeed('Comisión actualizada correctamente.'),
        error: (err) => this.fail(err, 'Error al actualizar la comisión.')
      });
    });
  }

  deactivateCommission(commission: Commission): void {
    if (!this.selectedCareerId) return;
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '450px', disableClose: true,
      data: { title: 'Dar de baja comisión', action: 'DAR DE BAJA', username: `${commission.code} · ${commission.name}` }
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (!result) return;
      this.commissionService.deactivateCommission(this.selectedCareerId!, commission.id).subscribe({
        next: () => this.succeed(`Comisión ${commission.code} dada de baja.`),
        error: (err) => this.fail(err, 'Error al dar de baja la comisión.')
      });
    });
  }

  private succeed(message: string): void {
    this.successMsg = message;
    this.loadCommissions();
    setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 4000);
  }

  private clearMessages(): void { this.errorMsg = ''; this.successMsg = ''; }

  private fail(err: any, fallback: string): void {
    this.isLoading = false;
    this.errorMsg = err?.error?.msg || err?.message || fallback;
    this.cdr.detectChanges();
  }
}
