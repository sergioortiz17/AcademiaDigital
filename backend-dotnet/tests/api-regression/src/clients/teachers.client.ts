import { ApiRequestExecutor } from '../utils/api-request';

export class TeachersClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  list(includeInactive = false) {
    return this.api.send({
      operation: 'Listar legajos docentes', method: 'GET', path: '/api/v1/teachers', query: { includeInactive }
    });
  }

  get(id: number) {
    return this.api.send({ operation: 'Consultar legajo docente', method: 'GET', path: `/api/v1/teachers/${id}` });
  }

  create(body: unknown) {
    return this.api.send({ operation: 'Crear legajo docente', method: 'POST', path: '/api/v1/teachers', body });
  }

  update(id: number, body: unknown) {
    return this.api.send({ operation: 'Actualizar legajo docente', method: 'PUT', path: `/api/v1/teachers/${id}`, body });
  }

  deactivate(id: number, reason?: string) {
    return this.api.send({
      operation: 'Dar de baja legajo docente', method: 'DELETE', path: `/api/v1/teachers/${id}`, query: { reason }
    });
  }

  documents(id: number) {
    return this.api.send({
      operation: 'Listar documentos docentes', method: 'GET', path: `/api/v1/teachers/${id}/documents`
    });
  }

  submitDocument(id: number, body: unknown) {
    return this.api.send({
      operation: 'Presentar documento docente', method: 'POST', path: `/api/v1/teachers/${id}/documents`, body
    });
  }

  reviewDocument(id: number, documentId: number, body: unknown) {
    return this.api.send({
      operation: 'Revisar documento docente',
      method: 'PATCH',
      path: `/api/v1/teachers/${id}/documents/${documentId}/review`,
      body
    });
  }

  assignments(id: number, includeEnded = false) {
    return this.api.send({
      operation: 'Listar asignaciones docentes',
      method: 'GET',
      path: `/api/v1/teachers/${id}/assignments`,
      query: { includeEnded }
    });
  }

  assign(id: number, body: unknown) {
    return this.api.send({
      operation: 'Asignar cargo al docente', method: 'POST', path: `/api/v1/teachers/${id}/assignments`, body
    });
  }

  endAssignment(id: number, assignmentId: number, body: unknown) {
    return this.api.send({
      operation: 'Finalizar asignación docente',
      method: 'DELETE',
      path: `/api/v1/teachers/${id}/assignments/${assignmentId}`,
      body
    });
  }

  myAssignments(includeEnded = false) {
    return this.api.send({
      operation: 'Consultar asignaciones propias',
      method: 'GET',
      path: '/api/v1/teachers/me/assignments',
      query: { includeEnded }
    });
  }
}
