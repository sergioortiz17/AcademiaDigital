import { ApiRequestExecutor } from '../utils/api-request';

export class ReceiptsClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  byStudent(studentId: number) {
    return this.api.send({ operation: 'Consultar recibos del alumno', method: 'GET', path: '/api/v1/receipts', query: { studentId } });
  }

  mine() {
    return this.api.send({ operation: 'Consultar mis recibos', method: 'GET', path: '/api/v1/students/me/receipts' });
  }

  get(publicId: string) {
    return this.api.send({ operation: 'Consultar recibo', method: 'GET', path: `/api/v1/receipts/${publicId}` });
  }

  generate(publicId: string) {
    return this.api.send({ operation: 'Reintentar generación de recibo', method: 'POST', path: `/api/v1/receipts/${publicId}/generate` });
  }

  download(publicId: string) {
    return this.api.send({ operation: 'Descargar recibo PDF', method: 'GET', path: `/api/v1/receipts/${publicId}/download` });
  }
}
