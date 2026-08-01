import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { enrollmentPeriodSchema } from '../../src/contracts/schemas';
import { registeredStudentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('inscripción del estudiante a StudyPlanCourses @critical @regression @enrollment', async ({ admin, anonymous, authenticatedClients, db }) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Inscripciones');
  await allure.story('Inscripción académica a materias del plan');
  await allure.severity('critical');

  let careerId = 0;
  let userId = 0;
  let periodId = 0;
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
    expect(await db.countStudentCareers(studentId)).toBe(1);

    await admin.students.assignStudyPlan(studentId, scenario.studyPlan.id, 'Enrollment E2E');
    const assignment = await admin.students.assignAcademic(studentId, {
      careerId,
      studyPlanId: scenario.studyPlan.id,
      commissionId: scenario.commission.id,
      academicYear: 2026,
      yearNumber: 1,
      reason: 'Enrollment E2E'
    });
    expect(assignment.response.status()).toBe(201);

    const studentSession = await authenticatedClients(registration.email, registration.password);
    const periodCall = await admin.enrollments.createPeriod({
      careerId,
      studyPlanId: scenario.studyPlan.id,
      academicYear: 2026,
      semester: 1,
      quotasMorning: 10,
      quotasAfternoon: 10,
      quotasEvening: 10
    });
    expect(periodCall.response.status()).toBe(201);
    const period = enrollmentPeriodSchema.parse((periodCall.body as { data: unknown }).data);
    periodId = period.id;

    const enrollmentBody = {
      enrollmentPeriodId: period.id,
      shift: 'Mañana',
      studyPlanCourseIds: [scenario.introStudyPlanCourse.id]
    };
    const enroll = await studentSession.api.enrollments.enroll(enrollmentBody);
    expect(enroll.response.status()).toBe(201);

    const mine = await studentSession.api.enrollments.myEnrollments();
    expect(mine.response.status()).toBe(200);
    expect(mine.body).toMatchObject({ success: true, data: expect.any(Array) });

    const students = await admin.enrollments.periodStudents(period.id);
    expect(students.response.status()).toBe(200);
    expect(students.body).toMatchObject({ success: true, total: 1, data: [expect.objectContaining({ studentId })] });

    const history = await admin.students.academicHistory(studentId, 2026);
    expect(history.response.status()).toBe(200);
    expect(history.body).toMatchObject({ total: 1, items: [expect.objectContaining({ courseId: scenario.introCourse.id })] });

    const duplicate = await studentSession.api.enrollments.enroll(enrollmentBody);
    expect(duplicate.response.status()).toBe(409);

    const close = await admin.enrollments.closePeriod(period.id);
    expect(close.response.status()).toBe(200);
    const closedAttempt = await studentSession.api.enrollments.enroll(enrollmentBody);
    expect(closedAttempt.response.status()).toBe(409);
  } finally {
    await cleanupScenario(admin, db, careerId, userId ? [userId] : [], periodId ? [periodId] : []);
  }
});
