import { ApiRequestExecutor } from '../utils/api-request';

export class AcademicClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  createCareer(body: unknown) { return this.api.send({ operation: 'Crear carrera', method: 'POST', path: '/api/v1/careers', body }); }
  listCareers() { return this.api.send({ operation: 'Listar carreras', method: 'GET', path: '/api/v1/careers' }); }
  getCareer(id: number | string) { return this.api.send({ operation: 'Consultar carrera', method: 'GET', path: `/api/v1/careers/${id}` }); }
  deleteCareer(id: number) { return this.api.send({ operation: 'Eliminar carrera', method: 'DELETE', path: `/api/v1/careers/${id}` }); }

  createCourse(careerId: number, body: unknown) { return this.api.send({ operation: 'Crear curso', method: 'POST', path: `/api/v1/careers/${careerId}/courses`, body }); }
  listCourses(careerId: number) { return this.api.send({ operation: 'Listar cursos de carrera', method: 'GET', path: `/api/v1/careers/${careerId}/courses` }); }
  deleteCourse(careerId: number, id: number) { return this.api.send({ operation: 'Eliminar curso', method: 'DELETE', path: `/api/v1/careers/${careerId}/courses/${id}` }); }

  createStudyPlan(careerId: number, body: unknown) { return this.api.send({ operation: 'Crear plan de estudio', method: 'POST', path: `/api/v1/careers/${careerId}/study-plans`, body }); }
  listStudyPlans(careerId: number) { return this.api.send({ operation: 'Listar planes de estudio', method: 'GET', path: `/api/v1/careers/${careerId}/study-plans` }); }
  activateStudyPlan(careerId: number, id: number) { return this.api.send({ operation: 'Activar plan de estudio', method: 'POST', path: `/api/v1/careers/${careerId}/study-plans/${id}/activate` }); }
  groupedStudyPlan(careerId: number, id: number) { return this.api.send({ operation: 'Consultar plan agrupado', method: 'GET', path: `/api/v1/careers/${careerId}/study-plans/${id}/courses-grouped` }); }

  addStudyPlanCourse(studyPlanId: number, body: unknown) { return this.api.send({ operation: 'Asociar curso al plan', method: 'POST', path: `/api/v1/study-plans/${studyPlanId}/courses`, body }); }
  listStudyPlanCourses(studyPlanId: number) { return this.api.send({ operation: 'Listar cursos del plan', method: 'GET', path: `/api/v1/study-plans/${studyPlanId}/courses` }); }
  deleteStudyPlanCourse(studyPlanId: number, id: number) { return this.api.send({ operation: 'Eliminar curso del plan', method: 'DELETE', path: `/api/v1/study-plans/${studyPlanId}/courses/${id}` }); }
  addCoursePrerequisite(studyPlanId: number, courseId: number, body: unknown) { return this.api.send({ operation: 'Agregar correlativa al curso', method: 'POST', path: `/api/v1/study-plans/${studyPlanId}/courses/${courseId}/prerequisites`, body }); }

  createCommission(careerId: number, body: unknown) { return this.api.send({ operation: 'Crear comisión', method: 'POST', path: `/api/v1/careers/${careerId}/commissions`, body }); }
  listCommissions(careerId: number, academicYear?: number) { return this.api.send({ operation: 'Listar comisiones', method: 'GET', path: `/api/v1/careers/${careerId}/commissions`, query: { academicYear } }); }
  getCommission(careerId: number, id: number) { return this.api.send({ operation: 'Consultar comisión', method: 'GET', path: `/api/v1/careers/${careerId}/commissions/${id}` }); }
  deleteCommission(careerId: number, id: number) { return this.api.send({ operation: 'Desactivar comisión', method: 'DELETE', path: `/api/v1/careers/${careerId}/commissions/${id}` }); }
}
