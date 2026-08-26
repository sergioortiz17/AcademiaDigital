import { ApiRequestExecutor } from '../utils/api-request';

export class StudentsClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  create(body: unknown) { return this.api.send({ operation: 'Crear estudiante', method: 'POST', path: '/api/v1/students', body }); }
  createMalformed(rawData: string) { return this.api.send({ operation: 'Enviar JSON de estudiante malformado', method: 'POST', path: '/api/v1/students', headers: { 'Content-Type': 'application/json' }, rawData }); }
  list(query: Record<string, string | number | boolean | undefined> = {}) { return this.api.send({ operation: 'Listar estudiantes', method: 'GET', path: '/api/v1/students', query }); }
  get(id: number | string) { return this.api.send({ operation: 'Consultar estudiante', method: 'GET', path: `/api/v1/students/${id}` }); }
  remove(id: number, reason = 'Cleanup Playwright E2E') { return this.api.send({ operation: 'Dar de baja estudiante', method: 'DELETE', path: `/api/v1/students/${id}`, body: { reason } }); }
  listCareers(id: number) { return this.api.send({ operation: 'Listar carreras del estudiante', method: 'GET', path: `/api/v1/students/${id}/careers` }); }
  addCareer(id: number, careerId: number, enrollmentDate?: string) { return this.api.send({ operation: 'Agregar carrera al estudiante', method: 'POST', path: `/api/v1/students/${id}/careers`, body: { careerId, enrollmentDate } }); }
  assignStudyPlan(id: number, studyPlanId: number, migrationReason = 'Playwright E2E') { return this.api.send({ operation: 'Asignar plan al estudiante', method: 'POST', path: `/api/v1/students/${id}/study-plan`, body: { studyPlanId, migrationReason } }); }
  assignAcademic(id: number, body: unknown) { return this.api.send({ operation: 'Asignar comisión al estudiante', method: 'POST', path: `/api/v1/students/${id}/academic-assignments`, body }); }
  assignments(id: number, academicYear?: number) { return this.api.send({ operation: 'Consultar asignaciones académicas', method: 'GET', path: `/api/v1/students/${id}/academic-assignments`, query: { academicYear } }); }
  record(id: number) { return this.api.send({ operation: 'Consultar legajo', method: 'GET', path: `/api/v1/students/${id}/record` }); }
  eligibleCourses(id: number, careerId?: number) { return this.api.send({ operation: 'Consultar cursos elegibles', method: 'GET', path: `/api/v1/students/${id}/eligible-courses`, query: { careerId } }); }
  academicProgress(id: number, careerId?: number) { return this.api.send({ operation: 'Consultar progreso académico', method: 'GET', path: `/api/v1/students/${id}/academic-progress`, query: { careerId } }); }
  academicHistory(id: number, academicYear?: number) { return this.api.send({ operation: 'Consultar historial académico', method: 'GET', path: `/api/v1/students/${id}/academic-history`, query: { academicYear, page: 1, pageSize: 20 } }); }
  rematriculate(id: number, body: unknown) { return this.api.send({ operation: 'Rematricular estudiante', method: 'POST', path: `/api/v1/students/${id}/rematriculations`, body }); }
  listDocuments(id: number) { return this.api.send({ operation: 'Listar documentos del estudiante', method: 'GET', path: `/api/v1/students/${id}/documents` }); }
  getDocument(id: number, documentId: number | string) { return this.api.send({ operation: 'Consultar documento del estudiante', method: 'GET', path: `/api/v1/students/${id}/documents/${documentId}` }); }
  addDocument(id: number, body: unknown) { return this.api.send({ operation: 'Registrar documento del estudiante', method: 'POST', path: `/api/v1/students/${id}/documents`, body }); }
  reviewDocument(id: number, documentId: number, body: unknown) { return this.api.send({ operation: 'Revisar documento del estudiante', method: 'PATCH', path: `/api/v1/students/${id}/documents/${documentId}/status`, body }); }
  deleteDocument(id: number, documentId: number) { return this.api.send({ operation: 'Eliminar documento del estudiante', method: 'DELETE', path: `/api/v1/students/${id}/documents/${documentId}` }); }
  pendingDocuments(id: number) { return this.api.send({ operation: 'Consultar documentos pendientes', method: 'GET', path: `/api/v1/students/${id}/pending-documents` }); }
  listScholarships(id: number) { return this.api.send({ operation: 'Listar becas del estudiante', method: 'GET', path: `/api/v1/students/${id}/scholarships` }); }
  addScholarship(id: number, body: unknown) { return this.api.send({ operation: 'Asignar beca al estudiante', method: 'POST', path: `/api/v1/students/${id}/scholarships`, body }); }
  updateScholarship(id: number, studentScholarshipId: number, body: unknown) { return this.api.send({ operation: 'Actualizar beca del estudiante', method: 'PUT', path: `/api/v1/students/${id}/scholarships/${studentScholarshipId}`, body }); }
  revokeScholarship(id: number, studentScholarshipId: number) { return this.api.send({ operation: 'Revocar beca del estudiante', method: 'DELETE', path: `/api/v1/students/${id}/scholarships/${studentScholarshipId}` }); }
  getCustomValues(id: number) { return this.api.send({ operation: 'Consultar valores personalizados', method: 'GET', path: `/api/v1/students/${id}/custom-values` }); }
  saveCustomValues(id: number, values: Record<string, unknown>) { return this.api.send({ operation: 'Guardar valores personalizados', method: 'PUT', path: `/api/v1/students/${id}/custom-values`, body: { values } }); }
}
