import { ApiRequestExecutor } from '../utils/api-request';

export class GradesClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  gradebooks(query: { academicYear?: number; courseId?: number; commissionId?: number } = {}) {
    return this.api.send({ operation: 'Listar planillas de calificaciones', method: 'GET', path: '/api/v1/gradebooks', query });
  }

  gradebook(id: number) {
    return this.api.send({ operation: 'Consultar planilla de calificaciones', method: 'GET', path: `/api/v1/gradebooks/${id}` });
  }

  myGrades(query: { courseId?: number } = {}) {
    return this.api.send({ operation: 'Consultar mis calificaciones publicadas', method: 'GET', path: '/api/v1/gradebooks/me', query });
  }

  createGradebook(idempotencyKey: string, body: unknown) {
    return this.api.send({
      operation: 'Crear planilla de calificaciones', method: 'POST', path: '/api/v1/gradebooks',
      headers: { 'Idempotency-Key': idempotencyKey }, body
    });
  }

  saveGrades(id: number, body: unknown) {
    return this.api.send({ operation: 'Guardar calificaciones masivas', method: 'PUT', path: `/api/v1/gradebooks/${id}/grades`, body });
  }

  submitGradebook(id: number) {
    return this.api.send({ operation: 'Enviar planilla a SecretarÃ­a', method: 'POST', path: `/api/v1/gradebooks/${id}/submit` });
  }

  approveGradebook(id: number) {
    return this.api.send({ operation: 'Aprobar planilla', method: 'POST', path: `/api/v1/gradebooks/${id}/approve` });
  }

  publishGradebook(id: number) {
    return this.api.send({ operation: 'Publicar planilla', method: 'POST', path: `/api/v1/gradebooks/${id}/publish` });
  }

  closeGradebook(id: number) {
    return this.api.send({ operation: 'Cerrar cursada', method: 'POST', path: `/api/v1/gradebooks/${id}/close` });
  }

  reopenGradebook(id: number, reason: string) {
    return this.api.send({
      operation: 'Reabrir planilla', method: 'POST', path: `/api/v1/gradebooks/${id}/reopen`, body: { reason }
    });
  }

  examTables(query: { academicYear?: number; courseId?: number } = {}) {
    return this.api.send({ operation: 'Listar mesas de examen', method: 'GET', path: '/api/v1/exam-tables', query });
  }

  examTable(id: number) {
    return this.api.send({ operation: 'Consultar mesa de examen', method: 'GET', path: `/api/v1/exam-tables/${id}` });
  }

  myExamTables() {
    return this.api.send({ operation: 'Consultar mis mesas de examen', method: 'GET', path: '/api/v1/exam-tables/me' });
  }

  createExamTable(idempotencyKey: string, body: unknown) {
    return this.api.send({
      operation: 'Crear mesa de examen', method: 'POST', path: '/api/v1/exam-tables',
      headers: { 'Idempotency-Key': idempotencyKey }, body
    });
  }

  registerForExam(id: number, enrollmentId: number) {
    return this.api.send({
      operation: 'Inscribir a mesa de examen', method: 'POST', path: `/api/v1/exam-tables/${id}/registrations`,
      body: { enrollmentId }
    });
  }

  startExamGrading(id: number) {
    return this.api.send({ operation: 'Iniciar acta de examen', method: 'POST', path: `/api/v1/exam-tables/${id}/start-grading` });
  }

  saveExamResults(id: number, body: unknown) {
    return this.api.send({ operation: 'Guardar resultados de examen', method: 'PUT', path: `/api/v1/exam-tables/${id}/results`, body });
  }

  publishExamTable(id: number) {
    return this.api.send({ operation: 'Publicar acta de examen', method: 'POST', path: `/api/v1/exam-tables/${id}/publish` });
  }

  reopenExamTable(id: number, reason: string) {
    return this.api.send({
      operation: 'Reabrir acta de examen', method: 'POST', path: `/api/v1/exam-tables/${id}/reopen`, body: { reason }
    });
  }
}
