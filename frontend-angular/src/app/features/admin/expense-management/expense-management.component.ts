import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { ExpenseCategory, ExpenseLedger, FinanceOperationsService } from '../../../core/services/finance-operations.service';
import { MaterialModule } from '../../../shared/material.module';

@Component({
  selector: 'app-expense-management',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  template: `
    <section class="finance-page">
      <header><h1>Egresos</h1><p>Registro inmutable con adjuntos y movimientos compensatorios.</p></header>
      <p class="success" *ngIf="message">{{ message }}</p><p class="error" role="alert" *ngIf="error">{{ error }}</p>
      <mat-card><mat-card-header><mat-card-title>Nuevo egreso</mat-card-title></mat-card-header><mat-card-content class="form-grid">
        <mat-form-field appearance="outline"><mat-label>Categoría</mat-label><mat-select [(ngModel)]="form.categoryId"><mat-option *ngFor="let c of categories" [value]="c.id">{{ c.name }}</mat-option></mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Proveedor</mat-label><input matInput [(ngModel)]="form.supplierName" maxlength="200"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Fecha</mat-label><input matInput type="date" [(ngModel)]="form.expenseDate"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Monto</mat-label><input matInput type="number" min="0.01" step="0.01" [(ngModel)]="form.amount"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Comprobante</mat-label><input matInput [(ngModel)]="form.purchaseNote" maxlength="100"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Observación</mat-label><input matInput [(ngModel)]="form.observation" maxlength="1000"></mat-form-field>
        <label class="file">Adjunto PDF, PNG o JPEG (máx. 10 MB)<input type="file" accept="application/pdf,image/png,image/jpeg" (change)="selectFile($event)"></label>
        <button mat-flat-button color="primary" (click)="register()" [disabled]="saving || !validForm()">{{ saving ? 'Guardando…' : 'Registrar egreso' }}</button>
      </mat-card-content></mat-card>
      <mat-card><mat-card-header><mat-card-title>Categorías</mat-card-title></mat-card-header><mat-card-content class="category-form">
        <mat-form-field appearance="outline"><mat-label>Código</mat-label><input matInput [(ngModel)]="newCategory.code" maxlength="30"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Nombre</mat-label><input matInput [(ngModel)]="newCategory.name" maxlength="120"></mat-form-field>
        <button mat-stroked-button (click)="createCategory()" [disabled]="!newCategory.code.trim() || !newCategory.name.trim()">Agregar</button>
      </mat-card-content></mat-card>
      <mat-card><mat-card-header><mat-card-title>Filtros del historial</mat-card-title></mat-card-header><mat-card-content class="category-form">
        <mat-form-field appearance="outline"><mat-label>Desde</mat-label><input matInput type="date" [(ngModel)]="filter.from"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Hasta</mat-label><input matInput type="date" [(ngModel)]="filter.to"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Categoría</mat-label><mat-select [(ngModel)]="filter.categoryId"><mat-option [value]="null">Todas</mat-option><mat-option *ngFor="let c of categories" [value]="c.id">{{ c.name }}</mat-option></mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Proveedor</mat-label><input matInput [(ngModel)]="filter.supplier"></mat-form-field>
        <button mat-stroked-button (click)="load()">Aplicar</button>
      </mat-card-content></mat-card>
      <mat-progress-bar mode="indeterminate" *ngIf="loading"></mat-progress-bar>
      <div class="totals" *ngIf="ledger"><mat-card><strong>{{ ledger.expenseTotal | currency:'ARS':'symbol-narrow' }}</strong><span>Egresos</span></mat-card><mat-card><strong>{{ ledger.reversalTotal | currency:'ARS':'symbol-narrow' }}</strong><span>Reversiones</span></mat-card><mat-card><strong>{{ ledger.netTotal | currency:'ARS':'symbol-narrow' }}</strong><span>Neto</span></mat-card></div>
      <mat-card *ngIf="ledger"><mat-card-content class="table-wrap"><table mat-table [dataSource]="ledger.entries">
        <ng-container matColumnDef="date"><th mat-header-cell *matHeaderCellDef>Fecha</th><td mat-cell *matCellDef="let row">{{ row.expenseDate | date:'dd/MM/yyyy' }}</td></ng-container>
        <ng-container matColumnDef="supplier"><th mat-header-cell *matHeaderCellDef>Proveedor</th><td mat-cell *matCellDef="let row"><b>{{ row.supplierName }}</b><small>{{ row.category }}</small></td></ng-container>
        <ng-container matColumnDef="kind"><th mat-header-cell *matHeaderCellDef>Movimiento</th><td mat-cell *matCellDef="let row">{{ row.kind === 1 ? 'Reversión' : 'Egreso' }}</td></ng-container>
        <ng-container matColumnDef="amount"><th mat-header-cell *matHeaderCellDef>Monto</th><td mat-cell *matCellDef="let row">{{ row.amount | currency:'ARS':'symbol-narrow' }}</td></ng-container>
        <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Acciones</th><td mat-cell *matCellDef="let row"><button mat-icon-button *ngIf="row.hasAttachment" (click)="download(row.publicId)" matTooltip="Descargar adjunto"><mat-icon>download</mat-icon></button><button mat-icon-button *ngIf="row.kind === 0" (click)="reverse(row.publicId)" matTooltip="Revertir"><mat-icon>undo</mat-icon></button></td></ng-container>
        <tr mat-header-row *matHeaderRowDef="columns"></tr><tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table><p class="empty" *ngIf="ledger.entries.length === 0">Todavía no hay egresos.</p></mat-card-content></mat-card>
    </section>`,
  styles: [`
    .finance-page{padding:24px;display:grid;gap:18px}h1{margin:0}header p{margin:4px 0;color:#64748b}.form-grid{display:grid;grid-template-columns:repeat(3,minmax(160px,1fr));gap:12px;align-items:center;padding-top:16px}.category-form,.totals{display:flex;gap:12px;flex-wrap:wrap;align-items:center;padding-top:16px}.category-form mat-form-field{flex:1}.file{display:grid;gap:6px;color:#475569}.totals mat-card{padding:18px;display:grid;min-width:180px;flex:1}.totals strong{font-size:1.35rem}.totals span,small{color:#64748b}table{width:100%}td small{display:block}.table-wrap{overflow:auto}.error{color:#b91c1c}.success{color:#166534}.empty{text-align:center;padding:28px;color:#64748b}@media(max-width:800px){.finance-page{padding:12px}.form-grid{grid-template-columns:1fr}}
  `]
})
export class ExpenseManagementComponent implements OnInit {
  readonly columns = ['date', 'supplier', 'kind', 'amount', 'actions'];
  categories: ExpenseCategory[] = [];
  ledger: ExpenseLedger | null = null;
  form = { categoryId: 0, supplierName: '', expenseDate: new Date().toISOString().slice(0, 10), amount: 0, purchaseNote: '', observation: '', attachment: null as File | null };
  newCategory = { code: '', name: '' };
  filter: { from?: string; to?: string; categoryId?: number | null; supplier?: string } = {};
  loading = false;
  saving = false;
  error = '';
  message = '';

  constructor(private readonly finance: FinanceOperationsService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.error = '';
    forkJoin({ categories: this.finance.getExpenseCategories(), ledger: this.finance.getExpenses(this.filter) })
      .pipe(finalize(() => this.loading = false)).subscribe({
        next: value => {
          this.categories = value.categories;
          this.ledger = value.ledger;
          if (!this.form.categoryId && this.categories.length) this.form.categoryId = this.categories[0].id;
        },
        error: () => this.error = 'Finance no está disponible temporalmente. Volvé a intentar.'
      });
  }

  selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.form.attachment = input.files?.[0] ?? null;
  }

  validForm(): boolean {
    return this.form.categoryId > 0 && !!this.form.supplierName.trim() && this.form.amount > 0 && !!this.form.expenseDate;
  }

  register(): void {
    if (!this.validForm()) return;
    this.saving = true;
    this.error = '';
    this.finance.registerExpense(this.form).pipe(finalize(() => this.saving = false)).subscribe({
      next: () => {
        this.message = 'Egreso registrado.';
        this.form = { ...this.form, supplierName: '', amount: 0, purchaseNote: '', observation: '', attachment: null };
        this.load();
      },
      error: () => this.error = 'No se pudo registrar el egreso. Revisá los datos y el adjunto.'
    });
  }

  createCategory(): void {
    this.finance.createExpenseCategory(this.newCategory.code, this.newCategory.name).subscribe({
      next: category => {
        this.categories = [...this.categories, category];
        this.form.categoryId = category.id;
        this.newCategory = { code: '', name: '' };
        this.message = 'Categoría creada.';
      },
      error: () => this.error = 'No se pudo crear la categoría.'
    });
  }

  reverse(publicId: string): void {
    const reason = window.prompt('Motivo de la reversión (mínimo 3 caracteres):')?.trim();
    if (!reason || reason.length < 3) return;
    this.finance.reverseExpense(publicId, reason).subscribe({
      next: () => { this.message = 'Se registró el movimiento compensatorio.'; this.load(); },
      error: () => this.error = 'No se pudo revertir el egreso o ya estaba revertido.'
    });
  }

  download(publicId: string): void {
    this.finance.downloadExpenseAttachment(publicId).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'adjunto-egreso';
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.error = 'No se pudo descargar el adjunto.'
    });
  }
}
