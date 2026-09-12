import { APIRequestContext, request as playwrightRequest, test as base } from '@playwright/test';
import { loginSchema, LoginResponse } from '../contracts/schemas';
import { env } from '../config/environment';
import { E2eDatabase } from '../db/e2e-database';
import { AcademicClient } from '../clients/academic.client';
import { AuthClient } from '../clients/auth.client';
import { EnrollmentsClient } from '../clients/enrollments.client';
import { StudentsClient } from '../clients/students.client';
import { StudentCatalogsClient } from '../clients/student-catalogs.client';
import { ApiRequestExecutor } from '../utils/api-request';
import { AdmissionsClient } from '../clients/admissions.client';
import { TeachersClient } from '../clients/teachers.client';
import { TeachingPositionsClient } from '../clients/teaching-positions.client';
import { AttendanceClient } from '../clients/attendance.client';
import { GradesClient } from '../clients/grades.client';
import { CertificatesClient } from '../clients/certificates.client';
import { FinanceClient } from '../clients/finance.client';
import { PaymentsClient } from '../clients/payments.client';
import { ReceiptsClient } from '../clients/receipts.client';

export interface ApiClients {
  auth: AuthClient;
  academic: AcademicClient;
  students: StudentsClient;
  enrollments: EnrollmentsClient;
  studentCatalogs: StudentCatalogsClient;
  admissions: AdmissionsClient;
  teachers: TeachersClient;
  teachingPositions: TeachingPositionsClient;
  attendance: AttendanceClient;
  grades: GradesClient;
  certificates: CertificatesClient;
  finance: FinanceClient;
  payments: PaymentsClient;
  receipts: ReceiptsClient;
}

function clients(context: APIRequestContext, token?: string): ApiClients {
  const headers: Record<string, string> = token ? { Authorization: `Bearer ${token}` } : {};
  const executor = new ApiRequestExecutor(context, headers);
  return {
    auth: new AuthClient(executor),
    academic: new AcademicClient(executor),
    students: new StudentsClient(executor),
    enrollments: new EnrollmentsClient(executor),
    studentCatalogs: new StudentCatalogsClient(executor),
    admissions: new AdmissionsClient(executor, env.admissionChallengeToken),
    teachers: new TeachersClient(executor),
    teachingPositions: new TeachingPositionsClient(executor),
    attendance: new AttendanceClient(executor),
    grades: new GradesClient(executor),
    certificates: new CertificatesClient(executor),
    finance: new FinanceClient(executor),
    payments: new PaymentsClient(executor),
    receipts: new ReceiptsClient(executor)
  };
}

type Fixtures = {
  db: E2eDatabase;
  anonymous: ApiClients;
  admin: ApiClients;
  adminToken: string;
  authenticatedClients: (email: string, password: string) => Promise<{ api: ApiClients; token: string; dispose: () => Promise<void> }>;
};

export const test = base.extend<Fixtures>({
  db: async ({}, use) => {
    const db = new E2eDatabase();
    await db.connect();
    await use(db);
    await db.close();
  },
  anonymous: async ({ request }, use) => {
    await use(clients(request));
  },
  adminToken: async ({ request }, use) => {
    const result = await clients(request).auth.login(env.adminEmail, env.adminPassword);
    const login = loginSchema.parse(result.body) as LoginResponse;
    await use(login.token);
  },
  admin: async ({ request, adminToken }, use) => {
    await use(clients(request, adminToken));
  },
  authenticatedClients: async ({}, use) => {
    const contexts: APIRequestContext[] = [];
    await use(async (email, password) => {
      const context = await playwrightRequest.newContext({ baseURL: env.apiBaseUrl, extraHTTPHeaders: { Accept: 'application/json' } });
      contexts.push(context);
      const anonymousApi = clients(context);
      const login = loginSchema.parse((await anonymousApi.auth.login(email, password)).body);
      return { api: clients(context, login.token), token: login.token, dispose: () => context.dispose() };
    });
    await Promise.all(contexts.map((context) => context.dispose().catch(() => undefined)));
  }
});

export { expect } from '@playwright/test';
