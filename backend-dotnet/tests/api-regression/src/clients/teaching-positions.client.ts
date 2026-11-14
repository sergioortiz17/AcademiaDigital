import { ApiRequestExecutor } from '../utils/api-request';

export class TeachingPositionsClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  list(query: { academicYear?: number; semester?: number; isVacant?: boolean; includeInactive?: boolean } = {}) {
    return this.api.send({
      operation: 'Listar cargos docentes', method: 'GET', path: '/api/v1/course-sections', query
    });
  }

  get(id: number) {
    return this.api.send({
      operation: 'Consultar cargo docente', method: 'GET', path: `/api/v1/course-sections/${id}`
    });
  }

  create(body: unknown) {
    return this.api.send({
      operation: 'Crear cargo docente', method: 'POST', path: '/api/v1/course-sections', body
    });
  }

  update(id: number, body: unknown) {
    return this.api.send({
      operation: 'Actualizar cargo docente', method: 'PUT', path: `/api/v1/course-sections/${id}`, body
    });
  }

  deactivate(id: number, reason: string) {
    return this.api.send({
      operation: 'Desactivar cargo docente',
      method: 'DELETE',
      path: `/api/v1/course-sections/${id}`,
      query: { reason }
    });
  }
}
