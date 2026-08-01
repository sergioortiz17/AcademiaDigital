import { ApiRequestExecutor } from '../utils/api-request';

export class StudentCatalogsClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  listDocumentRequirements(careerId?: number) { return this.api.send({ operation: 'Listar requisitos documentales', method: 'GET', path: '/api/v1/document-requirements', query: { careerId } }); }
  getDocumentRequirement(id: number | string) { return this.api.send({ operation: 'Consultar requisito documental', method: 'GET', path: `/api/v1/document-requirements/${id}` }); }
  createDocumentRequirement(body: unknown) { return this.api.send({ operation: 'Crear requisito documental', method: 'POST', path: '/api/v1/document-requirements', body }); }
  updateDocumentRequirement(id: number, body: unknown) { return this.api.send({ operation: 'Actualizar requisito documental', method: 'PUT', path: `/api/v1/document-requirements/${id}`, body }); }
  deleteDocumentRequirement(id: number) { return this.api.send({ operation: 'Desactivar requisito documental', method: 'DELETE', path: `/api/v1/document-requirements/${id}` }); }

  listScholarships() { return this.api.send({ operation: 'Listar becas', method: 'GET', path: '/api/v1/scholarships' }); }
  getScholarship(id: number | string) { return this.api.send({ operation: 'Consultar beca', method: 'GET', path: `/api/v1/scholarships/${id}` }); }
  createScholarship(body: unknown) { return this.api.send({ operation: 'Crear beca', method: 'POST', path: '/api/v1/scholarships', body }); }
  updateScholarship(id: number, body: unknown) { return this.api.send({ operation: 'Actualizar beca', method: 'PUT', path: `/api/v1/scholarships/${id}`, body }); }
  deleteScholarship(id: number) { return this.api.send({ operation: 'Desactivar beca', method: 'DELETE', path: `/api/v1/scholarships/${id}` }); }

  listCustomFields() { return this.api.send({ operation: 'Listar campos personalizados', method: 'GET', path: '/api/v1/student-custom-fields' }); }
  getCustomField(id: number | string) { return this.api.send({ operation: 'Consultar campo personalizado', method: 'GET', path: `/api/v1/student-custom-fields/${id}` }); }
  createCustomField(body: unknown) { return this.api.send({ operation: 'Crear campo personalizado', method: 'POST', path: '/api/v1/student-custom-fields', body }); }
  updateCustomField(id: number, body: unknown) { return this.api.send({ operation: 'Actualizar campo personalizado', method: 'PUT', path: `/api/v1/student-custom-fields/${id}`, body }); }
  deleteCustomField(id: number) { return this.api.send({ operation: 'Desactivar campo personalizado', method: 'DELETE', path: `/api/v1/student-custom-fields/${id}` }); }
}
