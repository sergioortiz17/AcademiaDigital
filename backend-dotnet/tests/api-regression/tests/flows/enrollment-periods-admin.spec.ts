import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { registeredStudentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('administración de períodos exige Admin @m4 @critical @regression @authorization', async ({ admin, anonymous, authenticatedClients, db }) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Períodos de inscripción');
  await allure.story('Autorización administrativa');
  await allure.severity('critical');

  let careerId = 0;
  let userId = 0;
  let unexpectedPeriodId = 0;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const registration = registeredStudentData(careerId);
    const register = await anonymous.auth.register(registration);
    expect(register.response.status()).toBe(201);
    userId = (register.body as { userID: number }).userID;
    const studentId = await db.findStudentIdByUserId(userId);
    expect(studentId).not.toBeNull();
    if (!studentId) throw new Error('Register created no Student row.');

    const studentSession = await authenticatedClients(registration.email, registration.password);
    const periodBody = {
      careerId,
      studyPlanId: scenario.studyPlan.id,
      academicYear: 2026,
      semester: 1,
      quotasMorning: 1,
      quotasAfternoon: 1,
      quotasEvening: 1
    };
    const quotaBody = { quotasMorning: 1, quotasAfternoon: 1, quotasEvening: 1 };

    const anonymousCalls = [
      () => anonymous.enrollments.listPeriods(),
      () => anonymous.enrollments.createPeriod(periodBody),
      () => anonymous.enrollments.periodStudents(999_999),
      () => anonymous.enrollments.updateQuotas(999_999, quotaBody),
      () => anonymous.enrollments.closePeriod(999_999),
      () => anonymous.enrollments.activatePeriod(999_999),
      () => anonymous.enrollments.deletePeriod(999_999),
      () => anonymous.enrollments.periodReport(999_999),
      () => anonymous.enrollments.removeStudent(999_999, studentId)
    ];
    for (const call of anonymousCalls)
      expect((await call()).response.status()).toBe(401);

    const studentCalls = [
      () => studentSession.api.enrollments.listPeriods(),
      () => studentSession.api.enrollments.createPeriod(periodBody),
      () => studentSession.api.enrollments.periodStudents(999_999),
      () => studentSession.api.enrollments.updateQuotas(999_999, quotaBody),
      () => studentSession.api.enrollments.closePeriod(999_999),
      () => studentSession.api.enrollments.activatePeriod(999_999),
      () => studentSession.api.enrollments.deletePeriod(999_999),
      () => studentSession.api.enrollments.periodReport(999_999),
      () => studentSession.api.enrollments.removeStudent(999_999, studentId)
    ];
    for (const call of studentCalls) {
      const response = await call();
      if (response.response.status() === 201)
        unexpectedPeriodId = (response.body as { data?: { id?: number } }).data?.id ?? 0;
      expect(response.response.status()).toBe(403);
    }

    expect((await anonymous.enrollments.activePeriod(careerId)).response.status()).toBe(401);
    const activeForStudent = await studentSession.api.enrollments.activePeriod(careerId);
    expect(activeForStudent.response.status()).toBe(200);
    expect(activeForStudent.body).toMatchObject({ success: true, data: null });
  } finally {
    await cleanupScenario(
      admin,
      db,
      careerId,
      userId ? [userId] : [],
      unexpectedPeriodId ? [unexpectedPeriodId] : []);
  }
});
