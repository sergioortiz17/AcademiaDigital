import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { problemSchema } from '../../src/contracts/schemas';
import { env } from '../../src/config/environment';

test('sesión JWT válida y revocación por logout @smoke @critical @authorization', async ({ admin }) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Autenticación');
  await allure.severity('critical');

  const session = await admin.auth.checkSession();
  expect(session.response.status()).toBe(200);
  expect(session.body).toMatchObject({ success: true });

  const profile = await admin.auth.profile();
  expect(profile.response.status()).toBe(200);
  expect(profile.body).toMatchObject({ success: true, email: env.adminEmail, role: 3 });

  const logout = await admin.auth.logout();
  expect(logout.response.status()).toBe(200);

  const revoked = await admin.auth.profile();
  expect(revoked.response.status()).toBe(401);
});

test('rechaza credenciales y tokens inválidos @negative @authorization', async ({ anonymous, request }) => {
  const invalidLogin = await anonymous.auth.login('nobody.playwright@e2e.local', 'wrong-password');
  expect(invalidLogin.response.status()).toBe(401);
  problemSchema.parse(invalidLogin.body);

  const withoutToken = await anonymous.students.list();
  expect(withoutToken.response.status()).toBe(401);

  const invalidTokenResponse = await request.get('/api/v1/students', {
    headers: { Authorization: ['Bearer', 'invalid.jwt.value'].join(' ') },
    failOnStatusCode: false
  });
  expect(invalidTokenResponse.status()).toBe(401);
});

test('rutas académicas sensibles requieren autenticación @critical @authorization', async ({ anonymous }) => {
  // Known backend defect: StudentAcademicController currently has no guard and returns 404 instead of 401.
  const response = await anonymous.students.eligibleCourses(999_999_999);
  expect(response.response.status()).toBe(401);
});
