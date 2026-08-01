import { test, expect } from '../../src/fixtures/api.fixture';
import { careerSchema, pagedStudentsSchema } from '../../src/contracts/schemas';
import { env } from '../../src/config/environment';

test('contratos básicos de listados y recursos inexistentes @smoke @regression', async ({ admin }) => {
  const careers = await admin.academic.listCareers();
  expect(careers.response.status()).toBe(200);
  expect(careers.response.headers()['content-type']).toContain('application/json');
  expect(careers.durationMs).toBeLessThan(env.responseTimeMs);
  careerSchema.array().parse(careers.body);

  const students = await admin.students.list({ page: 1, pageSize: 20 });
  expect(students.response.status()).toBe(200);
  pagedStudentsSchema.parse(students.body);

  const missingCareer = await admin.academic.getCareer(2_147_483_647);
  expect(missingCareer.response.status()).toBe(404);

  const malformedStudentId = await admin.students.get('not-a-number');
  expect(malformedStudentId.response.status()).toBe(404);
});
