import { ApiRequestExecutor } from '../utils/api-request';

export class FinanceClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  concepts() { return this.api.send({ operation: 'Listar conceptos financieros', method: 'GET', path: '/api/v1/finance/concepts' }); }
  createConcept(body: unknown) { return this.api.send({ operation: 'Crear concepto financiero', method: 'POST', path: '/api/v1/finance/concepts', body }); }
  updateConcept(id: number, body: unknown) { return this.api.send({ operation: 'Actualizar concepto financiero', method: 'PUT', path: `/api/v1/finance/concepts/${id}`, body }); }
  rates(query: { careerId?: number; academicYear?: number } = {}) { return this.api.send({ operation: 'Listar tarifas financieras', method: 'GET', path: '/api/v1/finance/rates', query }); }
  createRate(body: unknown) { return this.api.send({ operation: 'Crear tarifa financiera', method: 'POST', path: '/api/v1/finance/rates', body }); }
  updateRate(id: number, body: unknown) { return this.api.send({ operation: 'Actualizar tarifa financiera', method: 'PUT', path: `/api/v1/finance/rates/${id}`, body }); }
  benefits() { return this.api.send({ operation: 'Listar beneficios financieros', method: 'GET', path: '/api/v1/finance/benefits' }); }
  createBenefit(body: unknown) { return this.api.send({ operation: 'Crear beneficio financiero', method: 'POST', path: '/api/v1/finance/benefits', body }); }
  plans(query: { careerId?: number; academicYear?: number } = {}) { return this.api.send({ operation: 'Listar planes de cobro', method: 'GET', path: '/api/v1/finance/plans', query }); }
  createPlan(body: unknown) { return this.api.send({ operation: 'Crear plan de cobro', method: 'POST', path: '/api/v1/finance/plans', body }); }
  generate(planId: number, target?: {
    studentId: number; careerId: number; studentCareerId: number; academicYear: number;
    condition: string; grantedScholarshipIds: number[]; studentName: string; dni: string;
    legajoNumber: string; careerName: string;
  }, idempotencyKey?: string) {
    return this.api.send({
      operation: 'Generar deudas del plan', method: 'POST', path: '/api/v1/finance/debts/generate',
      body: target ? { ...target, billingPlanId: planId } : { billingPlanId: planId },
      headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined
    });
  }
  studentDebts(studentId: number) { return this.api.send({ operation: 'Consultar deuda del alumno', method: 'GET', path: '/api/v1/finance/debts', query: { studentId } }); }
  myDebts() { return this.api.send({ operation: 'Consultar mi deuda', method: 'GET', path: '/api/v1/finance/debts/me' }); }
  syncDirectory(students: unknown[]) { return this.api.send({ operation: 'Sincronizar padrón financiero', method: 'POST', path: '/api/v1/finance/delinquency/directory/sync', body: { students } }); }
  delinquency(query: { careerId?: number; commissionId?: number; academicYear?: number; studentId?: number; asOf?: string } = {}) {
    return this.api.send({ operation: 'Consultar morosidad', method: 'GET', path: '/api/v1/finance/delinquency', query });
  }
  delinquencyStudent(studentId: number) { return this.api.send({ operation: 'Consultar detalle de mora', method: 'GET', path: `/api/v1/finance/delinquency/students/${studentId}` }); }
  delinquencyRules() { return this.api.send({ operation: 'Listar reglas de mora', method: 'GET', path: '/api/v1/finance/delinquency/rules' }); }
  createDelinquencyRule(body: unknown) { return this.api.send({ operation: 'Crear regla de mora', method: 'POST', path: '/api/v1/finance/delinquency/rules', body }); }
  exportDelinquency(format: 'excel' | 'pdf') { return this.api.send({ operation: `Exportar morosidad ${format}`, method: 'GET', path: '/api/v1/finance/delinquency/export', query: { format } }); }
  expenseCategories(activeOnly = false) { return this.api.send({ operation: 'Listar categorías de egreso', method: 'GET', path: '/api/v1/finance/expenses/categories', query: { activeOnly } }); }
  createExpenseCategory(body: unknown) { return this.api.send({ operation: 'Crear categoría de egreso', method: 'POST', path: '/api/v1/finance/expenses/categories', body }); }
  registerExpense(fields: Record<string, string | number | boolean | { name: string; mimeType: string; buffer: Buffer }>) {
    return this.api.send({ operation: 'Registrar egreso', method: 'POST', path: '/api/v1/finance/expenses', multipart: fields });
  }
  expenses(query: { from?: string; to?: string; categoryId?: number; supplier?: string } = {}) { return this.api.send({ operation: 'Consultar egresos', method: 'GET', path: '/api/v1/finance/expenses', query }); }
  reverseExpense(publicId: string, reason: string) { return this.api.send({ operation: 'Revertir egreso', method: 'POST', path: `/api/v1/finance/expenses/${publicId}/reverse`, body: { reason } }); }
  downloadExpenseAttachment(publicId: string) { return this.api.send({ operation: 'Descargar comprobante de egreso', method: 'GET', path: `/api/v1/finance/expenses/${publicId}/attachment` }); }
}
