import { ApiRequestExecutor } from '../utils/api-request';

export class EnrollmentsClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  createPeriod(body: unknown) { return this.api.send({ operation: 'Crear período de inscripción', method: 'POST', path: '/api/v1/enrollments/periods', body }); }
  listPeriods() { return this.api.send({ operation: 'Listar períodos', method: 'GET', path: '/api/v1/enrollments/periods' }); }
  activePeriod(careerId: number) { return this.api.send({ operation: 'Consultar período activo', method: 'GET', path: '/api/v1/enrollments/periods/active', query: { careerId } }); }
  updateQuotas(id: number, body: unknown) { return this.api.send({ operation: 'Actualizar cupos del período', method: 'PUT', path: `/api/v1/enrollments/periods/${id}/quotas`, body }); }
  closePeriod(id: number) { return this.api.send({ operation: 'Cerrar período', method: 'PUT', path: `/api/v1/enrollments/periods/${id}/close` }); }
  activatePeriod(id: number) { return this.api.send({ operation: 'Activar período', method: 'PUT', path: `/api/v1/enrollments/periods/${id}/activate` }); }
  periodStudents(id: number) { return this.api.send({ operation: 'Consultar alumnos del período', method: 'GET', path: `/api/v1/enrollments/periods/${id}/students` }); }
  periodReport(id: number) { return this.api.send({ operation: 'Consultar reporte del período', method: 'GET', path: `/api/v1/enrollments/periods/${id}/report` }); }
  enroll(body: unknown) { return this.api.send({ operation: 'Inscribir estudiante a cursos', method: 'POST', path: '/api/v1/enrollments', body }); }
  myEnrollments() { return this.api.send({ operation: 'Consultar mis inscripciones', method: 'GET', path: '/api/v1/enrollments/my' }); }
  removeStudent(periodId: number, studentId: number) { return this.api.send({ operation: 'Eliminar alumno del período', method: 'DELETE', path: `/api/v1/enrollments/periods/${periodId}/students/${studentId}` }); }
  deletePeriod(id: number) { return this.api.send({ operation: 'Eliminar período', method: 'DELETE', path: `/api/v1/enrollments/periods/${id}` }); }
}
