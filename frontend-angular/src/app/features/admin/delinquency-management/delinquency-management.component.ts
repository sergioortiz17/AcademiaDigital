import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { DelinquencyDashboard, DelinquencyFilter, DelinquencyRule, FinanceOperationsService } from '../../../core/services/finance-operations.service';
import { MaterialModule } from '../../../shared/material.module';

@Component({
  selector: 'app-delinquency-management',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  template: `
    <section class="finance-page">
      <header>
        <div><h1>Morosidad</h1><p>Deuda vencida y alertas por alumno.</p></div>
        <div class="exports">
          <button mat-stroked-button (click)="export('excel')">Excel</button>
          <button mat-stroked-button (click)="export('pdf')">PDF</button>
        </div>
      </header>
      <mat-card><mat-card-content class="filters">
        <mat-form-field appearance="outline"><mat-label>ID carrera</mat-label><input matInput type="number" [(ngModel)]="filter.careerId"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>ID comisión</mat-label><input matInput type="number" [(ngModel)]="filter.commissionId"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Ciclo</mat-label><input matInput type="number" [(ngModel)]="filter.academicYear"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Fecha de corte</mat-label><input matInput type="date" [(ngModel)]="filter.asOf"></mat-form-field>
        <button mat-flat-button color="primary" (click)="load()" [disabled]="loading">Aplicar</button>
      </mat-card-content></mat-card>
      <mat-card><mat-card-header><mat-card-title>Reglas de alerta</mat-card-title></mat-card-header><mat-card-content>
        <div class="filters">
          <mat-form-field appearance="outline"><mat-label>Nombre</mat-label><input matInput [(ngModel)]="newRule.name" maxlength="150"></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Deuda mínima</mat-label><input matInput type="number" min="0" [(ngModel)]="newRule.minimumOutstandingAmount"></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Días de mora</mat-label><input matInput type="number" min="0" [(ngModel)]="newRule.minimumDaysOverdue"></mat-form-field>
          <button mat-stroked-button (click)="createRule()" [disabled]="!newRule.name.trim()">Agregar regla</button>
        </div>
        <mat-chip-set><mat-chip *ngFor="let rule of rules">{{ rule.name }}: {{ rule.minimumOutstandingAmount | currency:'ARS':'symbol-narrow' }} / {{ rule.minimumDaysOverdue }} días</mat-chip></mat-chip-set>
      </mat-card-content></mat-card>
      <p class="error" role="alert" *ngIf="error">{{ error }}</p>
      <mat-progress-bar mode="indeterminate" *ngIf="loading"></mat-progress-bar>
      <div class="totals" *ngIf="dashboard">
        <mat-card><strong>{{ dashboard.studentCount }}</strong><span>Alumnos</span></mat-card>
        <mat-card><strong>{{ dashboard.outstandingTotal | currency:'ARS':'symbol-narrow' }}</strong><span>Saldo abierto</span></mat-card>
        <mat-card><strong>{{ dashboard.overdueTotal | currency:'ARS':'symbol-narrow' }}</strong><span>Saldo vencido</span></mat-card>
      </div>
      <mat-card *ngIf="dashboard"><mat-card-content class="table-wrap">
        <table mat-table [dataSource]="dashboard.students">
          <ng-container matColumnDef="student"><th mat-header-cell *matHeaderCellDef>Alumno</th><td mat-cell *matCellDef="let row"><b>{{ row.studentName }}</b><small>{{ row.legajoNumber }} · DNI {{ row.dni }}</small></td></ng-container>
          <ng-container matColumnDef="career"><th mat-header-cell *matHeaderCellDef>Carrera / comisión</th><td mat-cell *matCellDef="let row">{{ row.careerName }}<small>{{ row.commissionName || 'Sin comisión' }} · {{ row.academicYear }}</small></td></ng-container>
          <ng-container matColumnDef="amount"><th mat-header-cell *matHeaderCellDef>Vencido</th><td mat-cell *matCellDef="let row">{{ row.overdueAmount | currency:'ARS':'symbol-narrow' }}<small>{{ row.overdueInstallments }} cuotas · {{ row.maximumDaysOverdue }} días</small></td></ng-container>
          <ng-container matColumnDef="alerts"><th mat-header-cell *matHeaderCellDef>Alertas</th><td mat-cell *matCellDef="let row"><mat-chip-set><mat-chip *ngFor="let alert of row.triggeredAlerts">{{ alert }}</mat-chip></mat-chip-set></td></ng-container>
          <tr mat-header-row *matHeaderRowDef="columns"></tr><tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>
        <p class="empty" *ngIf="dashboard.students.length === 0">No hay deuda vencida para los filtros seleccionados.</p>
      </mat-card-content></mat-card>
    </section>`,
  styles: [`
    .finance-page{padding:24px;display:grid;gap:18px}header{display:flex;justify-content:space-between;gap:16px;align-items:center}h1{margin:0}header p{margin:4px 0;color:#64748b}.exports,.filters,.totals{display:flex;gap:12px;flex-wrap:wrap;align-items:center}.filters mat-form-field{flex:1 1 150px}.totals mat-card{padding:18px;display:grid;min-width:180px;flex:1}.totals strong{font-size:1.35rem}.totals span,small{color:#64748b}table{width:100%}td small{display:block;margin-top:3px}.table-wrap{overflow:auto}.error{color:#b91c1c}.empty{text-align:center;padding:28px;color:#64748b}@media(max-width:700px){.finance-page{padding:12px}header{align-items:flex-start;flex-direction:column}.exports{width:100%}}
  `]
})
export class DelinquencyManagementComponent implements OnInit {
  readonly columns = ['student', 'career', 'amount', 'alerts'];
  filter: DelinquencyFilter = {};
  dashboard: DelinquencyDashboard | null = null;
  rules: DelinquencyRule[] = [];
  newRule = { name: '', minimumOutstandingAmount: 0, minimumDaysOverdue: 0, isActive: true };
  loading = false;
  error = '';

  constructor(private readonly finance: FinanceOperationsService) {}

  ngOnInit(): void { this.load(); this.loadRules(); }

  load(): void {
    this.loading = true;
    this.error = '';
    this.finance.getDelinquency(this.filter).pipe(finalize(() => this.loading = false)).subscribe({
      next: result => this.dashboard = result,
      error: () => this.error = 'Finance no está disponible temporalmente. Volvé a intentar.'
    });
  }

  export(format: 'pdf' | 'excel'): void {
    this.finance.exportDelinquency(format, this.filter).subscribe({
      next: blob => this.save(blob, `morosidad.${format === 'pdf' ? 'pdf' : 'xls'}`),
      error: () => this.error = 'No se pudo generar la exportación.'
    });
  }

  createRule(): void {
    this.finance.createDelinquencyRule(this.newRule).subscribe({
      next: rule => {
        this.rules = [...this.rules, rule];
        this.newRule = { name: '', minimumOutstandingAmount: 0, minimumDaysOverdue: 0, isActive: true };
        this.load();
      },
      error: () => this.error = 'No se pudo crear la regla de alerta.'
    });
  }

  private loadRules(): void {
    this.finance.getDelinquencyRules().subscribe({
      next: rules => this.rules = rules,
      error: () => this.error = 'No se pudieron cargar las reglas de alerta.'
    });
  }

  private save(blob: Blob, name: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    link.click();
    URL.revokeObjectURL(url);
  }
}
