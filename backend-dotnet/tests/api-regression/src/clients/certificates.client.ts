import { ApiRequestExecutor } from '../utils/api-request';

export class CertificatesClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  myRequests() {
    return this.api.send({ operation: 'Consultar mis solicitudes de certificados', method: 'GET', path: '/api/v1/certificates/my' });
  }

  request(body: { certificateType: string; studentCareerId?: number; examRegistrationId?: number }) {
    return this.api.send({ operation: 'Solicitar certificado', method: 'POST', path: '/api/v1/certificates/request', body });
  }

  all(query: { search?: string; status?: string } = {}) {
    return this.api.send({ operation: 'Listar solicitudes de certificados', method: 'GET', path: '/api/v1/certificates/all', query });
  }

  approve(id: number) {
    return this.api.send({ operation: 'Aprobar solicitud de certificado', method: 'POST', path: `/api/v1/certificates/${id}/approve` });
  }

  reject(id: number, reason: string) {
    return this.api.send({ operation: 'Rechazar solicitud de certificado', method: 'POST', path: `/api/v1/certificates/${id}/reject`, body: { reason } });
  }

  issue(id: number) {
    return this.api.send({ operation: 'Emitir certificado', method: 'POST', path: `/api/v1/certificates/${id}/issue` });
  }

  myIssued() {
    return this.api.send({ operation: 'Consultar mis certificados emitidos', method: 'GET', path: '/api/v1/certificates/issued/me' });
  }

  studentHistory(studentId: number) {
    return this.api.send({ operation: 'Consultar historial de certificados del alumno', method: 'GET', path: `/api/v1/certificates/students/${studentId}/history` });
  }

  download(publicId: string) {
    return this.api.send({ operation: 'Descargar certificado PDF', method: 'GET', path: `/api/v1/certificates/issued/${publicId}/download` });
  }
}
