import 'dotenv/config';

function required(name: string, fallback?: string): string {
  const value = process.env[name] ?? fallback;
  if (!value) throw new Error(`Missing required environment variable: ${name}`);
  return value;
}

function parseBoolean(value: string | undefined): boolean {
  return value?.toLowerCase() === 'true';
}

export const env = {
  apiBaseUrl: required('API_BASE_URL', 'http://localhost:8010').replace(/\/$/, ''),
  responseTimeMs: Number(process.env.API_RESPONSE_TIME_MS ?? 3000),
  adminEmail: required('E2E_ADMIN_EMAIL', 'admin.playwright@e2e.local'),
  adminPassword: required('E2E_ADMIN_PASSWORD'),
  sqlConnectionString: required('E2E_SQL_CONNECTION_STRING'),
  allowDbCleanup: parseBoolean(process.env.E2E_ALLOW_DB_CLEANUP ?? 'true'),
  allowDevelopmentDatabase: parseBoolean(process.env.E2E_ALLOW_DEVELOPMENT_DATABASE),
  dockerManagedDatabase: parseBoolean(process.env.E2E_DOCKER_MANAGED_DATABASE),
  preserveData: parseBoolean(process.env.E2E_PRESERVE_DATA),
  dataPrefix: required('E2E_DATA_PREFIX', 'PWAPI')
} as const;

function databaseName(connectionString: string): string | undefined {
  return /(?:^|;)\s*(?:Database|Initial Catalog)\s*=\s*([^;]+)/i.exec(connectionString)?.[1]?.trim();
}

export function assertSafeE2eEnvironment(): void {
  const api = new URL(env.apiBaseUrl);
  const localHosts = new Set(['localhost', '127.0.0.1', '::1']);
  if (!localHosts.has(api.hostname)) {
    throw new Error(`Refusing E2E execution against non-local API host: ${api.hostname}`);
  }
  const database = databaseName(env.sqlConnectionString);
  const isolatedE2eDatabase = database?.toLowerCase() === 'academiadigitale2e';
  const authorizedDevelopmentDatabase = database?.toLowerCase() === 'academiadigital'
    && env.allowDevelopmentDatabase
    && env.dockerManagedDatabase;
  if (!isolatedE2eDatabase && !authorizedDevelopmentDatabase) {
    throw new Error(
      'Refusing database setup. Use AcademiaDigitalE2E, or explicitly authorize the Docker-managed AcademiaDigital database ' +
      'with E2E_ALLOW_DEVELOPMENT_DATABASE=true and E2E_DOCKER_MANAGED_DATABASE=true.'
    );
  }
  if (!env.preserveData && !env.allowDbCleanup) {
    throw new Error('E2E_ALLOW_DB_CLEANUP=true is required for the disposable E2E environment.');
  }
}
