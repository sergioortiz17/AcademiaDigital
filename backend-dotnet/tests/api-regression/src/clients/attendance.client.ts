import { ApiRequestExecutor } from '../utils/api-request';

export class AttendanceClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  sessions(query: { academicYear?: number; courseId?: number; commissionId?: number } = {}) {
    return this.api.send({ operation: 'Listar sesiones de asistencia', method: 'GET', path: '/api/v1/attendance/sessions', query });
  }

  session(id: number) {
    return this.api.send({ operation: 'Consultar planilla de asistencia', method: 'GET', path: `/api/v1/attendance/sessions/${id}` });
  }

  createSession(idempotencyKey: string, body: unknown) {
    return this.api.send({
      operation: 'Crear sesiÃ³n de asistencia', method: 'POST', path: '/api/v1/attendance/sessions',
      headers: { 'Idempotency-Key': idempotencyKey }, body
    });
  }

  saveRecords(id: number, body: unknown) {
    return this.api.send({
      operation: 'Guardar asistencia masiva', method: 'PUT', path: `/api/v1/attendance/sessions/${id}/records`, body
    });
  }

  close(id: number) {
    return this.api.send({ operation: 'Cerrar sesiÃ³n de asistencia', method: 'POST', path: `/api/v1/attendance/sessions/${id}/close` });
  }

  reopen(id: number, reason: string) {
    return this.api.send({
      operation: 'Reabrir sesiÃ³n de asistencia', method: 'POST', path: `/api/v1/attendance/sessions/${id}/reopen`, body: { reason }
    });
  }

  justify(recordId: number, body: unknown) {
    return this.api.send({
      operation: 'Justificar inasistencia', method: 'POST', path: `/api/v1/attendance/records/${recordId}/justifications`, body
    });
  }

  studentSummary(studentId: number, query: { courseId?: number; commissionId?: number } = {}) {
    return this.api.send({
      operation: 'Consultar resumen de asistencia del alumno', method: 'GET',
      path: `/api/v1/attendance/students/${studentId}/summary`, query
    });
  }

  mySummary(query: { courseId?: number; commissionId?: number } = {}) {
    return this.api.send({ operation: 'Consultar mi asistencia', method: 'GET', path: '/api/v1/attendance/me/summary', query });
  }

  export(id: number, format: 'csv' | 'pdf') {
    return this.api.send({
      operation: `Exportar asistencia ${format.toUpperCase()}`, method: 'GET',
      path: `/api/v1/attendance/sessions/${id}/export`, query: { format }
    });
  }
}
