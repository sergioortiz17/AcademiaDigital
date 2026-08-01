import { ApiRequestExecutor } from '../utils/api-request';

export class AuthClient {
  constructor(private readonly api: ApiRequestExecutor) {}

  login(email: string, password: string) {
    return this.api.send({ operation: 'Autenticar usuario', method: 'POST', path: '/api/v1/users/login', body: { email, password } });
  }
  register(body: unknown) {
    return this.api.send({ operation: 'Registrar alumno', method: 'POST', path: '/api/v1/users/register', body });
  }
  profile() {
    return this.api.send({ operation: 'Consultar perfil', method: 'GET', path: '/api/v1/users/profile' });
  }
  checkSession() {
    return this.api.send({ operation: 'Validar sesión', method: 'POST', path: '/api/v1/users/checkSession' });
  }
  logout() {
    return this.api.send({ operation: 'Cerrar sesión', method: 'POST', path: '/api/v1/users/logout' });
  }
}
