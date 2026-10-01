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
  generate(planId: number, idempotencyKey?: string) {
    return this.api.send({
      operation: 'Generar deudas del plan', method: 'POST', path: `/api/v1/finance/plans/${planId}/generate`,
      headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined
    });
  }
  studentDebts(studentId: number) { return this.api.send({ operation: 'Consultar deuda del alumno', method: 'GET', path: '/api/v1/finance/debts', query: { studentId } }); }
  myDebts() { return this.api.send({ operation: 'Consultar mi deuda', method: 'GET', path: '/api/v1/finance/debts/me' }); }
}
