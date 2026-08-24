import { ApiRequestExecutor } from '../utils/api-request';

export class AdmissionsClient {
  constructor(
    private readonly api: ApiRequestExecutor,
    private readonly defaultChallengeToken?: string
  ) {}

  getForm(slug: string) {
    return this.api.send({
      operation: 'Consultar formulario público de admisión',
      method: 'GET',
      path: `/api/v1/admissions/forms/${encodeURIComponent(slug)}`
    });
  }

  createApplication(body: unknown, challengeToken?: string | null) {
    const resolvedToken = challengeToken === undefined
      ? this.defaultChallengeToken
      : challengeToken;
    const requestBody = resolvedToken !== null
      && typeof body === 'object'
      && body !== null
      && !Array.isArray(body)
      ? { ...body, challengeToken: resolvedToken }
      : body;
    return this.api.send({
      operation: 'Crear solicitud pública de admisión',
      method: 'POST',
      path: '/api/v1/admissions/applications',
      body: requestBody
    });
  }

  getForms() {
    return this.api.send({
      operation: 'Listar formularios de admisión como administrador',
      method: 'GET',
      path: '/api/v1/admissions/forms'
    });
  }

  createForm(body: unknown) {
    return this.api.send({
      operation: 'Crear formulario de admisión como administrador',
      method: 'POST',
      path: '/api/v1/admissions/forms',
      body
    });
  }

  setFormActive(formId: number, isActive: boolean) {
    return this.api.send({
      operation: 'Activar o desactivar formulario de admisión',
      method: 'PATCH',
      path: `/api/v1/admissions/forms/${formId}/active`,
      body: { isActive }
    });
  }

  setFormCapacity(formId: number, capacity: number | null) {
    return this.api.send({
      operation: 'Configurar capacidad del formulario de admisión',
      method: 'PATCH',
      path: `/api/v1/admissions/forms/${formId}/capacity`,
      body: { capacity }
    });
  }

  getApplications(query = '') {
    return this.api.send({
      operation: 'Listar solicitudes de admisión como administrador',
      method: 'GET',
      path: `/api/v1/admissions/applications${query}`
    });
  }

  getApplication(publicId: string) {
    return this.api.send({
      operation: 'Consultar solicitud de admisión como administrador',
      method: 'GET',
      path: `/api/v1/admissions/applications/${publicId}`
    });
  }

  changeApplicationStatus(publicId: string, status: number, reason?: string) {
    return this.api.send({
      operation: 'Cambiar estado de solicitud de admisión',
      method: 'PATCH',
      path: `/api/v1/admissions/applications/${publicId}/status`,
      body: { status, reason: reason ?? null }
    });
  }

  getApplicationDocuments(publicId: string) {
    return this.api.send({
      operation: 'Listar documentos de una solicitud de admision',
      method: 'GET',
      path: `/api/v1/admissions/applications/${publicId}/documents`
    });
  }

  submitApplicationDocument(publicId: string, body: unknown) {
    return this.api.send({
      operation: 'Registrar documento de una solicitud de admision',
      method: 'POST',
      path: `/api/v1/admissions/applications/${publicId}/documents`,
      body
    });
  }

  reviewApplicationDocument(publicId: string, documentId: number, body: unknown) {
    return this.api.send({
      operation: 'Revisar documento de una solicitud de admision',
      method: 'PATCH',
      path: `/api/v1/admissions/applications/${publicId}/documents/${documentId}/review`,
      body
    });
  }

  getAgreement(publicId: string) {
    return this.api.send({
      operation: 'Consultar acuerdo de admision',
      method: 'GET',
      path: `/api/v1/admissions/applications/${publicId}/agreement`
    });
  }

  downloadAgreement(publicId: string) {
    return this.api.send({
      operation: 'Descargar acuerdo PDF de admision',
      method: 'GET',
      path: `/api/v1/admissions/applications/${publicId}/agreement/download`
    });
  }

  processOutbox(limit = 20) {
    return this.api.send({
      operation: 'Procesar outbox local de admision',
      method: 'POST',
      path: '/api/v1/admissions/outbox/process',
      query: { limit }
    });
  }

  processExpirations(admissionFormId?: number) {
    const query = admissionFormId ? `?admissionFormId=${admissionFormId}` : '';
    return this.api.send({
      operation: 'Procesar expiraciones y lista de espera de admisión',
      method: 'POST',
      path: `/api/v1/admissions/applications/process-expirations${query}`
    });
  }
}
