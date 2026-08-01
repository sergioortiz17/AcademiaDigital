import { request } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import { assertSafeE2eEnvironment, env } from './src/config/environment';
import { E2eDatabase, waitForDatabase } from './src/db/e2e-database';

export default async function globalSetup(): Promise<void> {
  assertSafeE2eEnvironment();
  await waitForDatabase();
  const context = await request.newContext({ baseURL: env.apiBaseUrl });
  try {
    let response;
    for (let attempt = 1; attempt <= 30; attempt++) {
      response = await context.get('/swagger/v1/swagger.json', { failOnStatusCode: false }).catch(() => undefined);
      if (response?.ok()) break;
      await new Promise((resolve) => setTimeout(resolve, 2000));
    }
    if (!response?.ok()) throw new Error(`API did not expose Swagger at ${env.apiBaseUrl}.`);
    await mkdir('.artifacts', { recursive: true });
    await writeFile('.artifacts/swagger-academiaDigital.json', await response.text(), 'utf8');
  } finally {
    await context.dispose();
  }

  const db = new E2eDatabase();
  await db.connect();
  const verificationContext = await request.newContext({ baseURL: env.apiBaseUrl });
  try {
    const admin = await db.seedAdmin();
    const login = await verificationContext.post('/api/v1/users/login', {
      data: { email: admin.email, password: admin.password },
      failOnStatusCode: false
    });
    if (login.status() !== 200) {
      throw new Error(
        `Safety check failed: API ${env.apiBaseUrl} does not authenticate the administrator seeded in the configured test database. ` +
        'The API is probably connected to a different database.'
      );
    }
    const loginBody = await login.json() as { token?: string; user?: { _id?: number } };
    if (Number(loginBody.user?._id) !== Number(admin.id)) {
      throw new Error(`Safety check failed: API ${env.apiBaseUrl} is not using the configured test database.`);
    }
    if (loginBody.token) {
      await verificationContext.post('/api/v1/users/logout', {
        headers: { Authorization: `Bearer ${loginBody.token}` },
        failOnStatusCode: false
      });
    }
  }
  finally {
    await verificationContext.dispose();
    await db.close();
  }
}
