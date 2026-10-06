import { ApiRequestExecutor } from '../utils/api-request';

export class PaymentsClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  methods() {
    return this.api.send({ operation: 'Listar medios de pago', method: 'GET', path: '/api/v1/finance/payment-methods' });
  }

  create(body: unknown) {
    return this.api.send({ operation: 'Crear pago', method: 'POST', path: '/api/v1/payments', body });
  }

  confirm(publicId: string, idempotencyKey?: string) {
    return this.api.send({
      operation: 'Confirmar pago', method: 'POST', path: `/api/v1/payments/${publicId}/confirm`,
      headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined
    });
  }

  reconcile(publicId: string, body: unknown) {
    return this.api.send({ operation: 'Conciliar transferencia', method: 'POST', path: `/api/v1/payments/${publicId}/reconcile`, body });
  }

  reverse(publicId: string, body: unknown) {
    return this.api.send({ operation: 'Revertir pago', method: 'POST', path: `/api/v1/payments/${publicId}/reverse`, body });
  }

  byStudent(studentId: number) {
    return this.api.send({ operation: 'Consultar pagos del alumno', method: 'GET', path: '/api/v1/payments', query: { studentId } });
  }

  mine() {
    return this.api.send({ operation: 'Consultar mis pagos', method: 'GET', path: '/api/v1/payments/me' });
  }
}
