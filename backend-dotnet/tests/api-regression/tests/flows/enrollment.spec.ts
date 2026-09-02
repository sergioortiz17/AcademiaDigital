import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { enrollmentPeriodSchema } from '../../src/contracts/schemas';
import { registeredStudentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('inscripción del estudiante a StudyPlanCourses @m4 @critical @regression @enrollment', async ({ admin, anonymous, authenticatedClients, db }) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Inscripciones');
  await allure.story('Inscripción académica a materias del plan');
  await allure.severity('critical');

  let careerId = 0;
  const userIds: number[] = [];
  let periodId = 0;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const registrations = [
      registeredStudentData(careerId),
      registeredStudentData(careerId),
      registeredStudentData(careerId)
    ];
    const students: Array<{
      studentId: number;
      session: Awaited<ReturnType<typeof authenticatedClients>>;
    }> = [];
    for (const registration of registrations) {
      const register = await anonymous.auth.register(registration);
      expect(register.response.status()).toBe(201);
      const userId = (register.body as { userID: number }).userID;
      userIds.push(userId);
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
      students.push({
        studentId,
        session: await authenticatedClients(registration.email, registration.password)
      });
    }
    const [primary, contenderA, contenderB] = students;
    const periodCall = await admin.enrollments.createPeriod({
      careerId,
      studyPlanId: scenario.studyPlan.id,
      academicYear: 2026,
      semester: 1,
      quotasMorning: 10,
      quotasAfternoon: 1,
      quotasEvening: 10
    });
    expect(periodCall.response.status()).toBe(201);
    const period = enrollmentPeriodSchema.parse((periodCall.body as { data: unknown }).data);
    periodId = period.id;

    const prerequisite = await admin.academic.addCoursePrerequisite(
      scenario.studyPlan.id,
      scenario.advancedCourse.id,
      {
        prerequisiteCourseId: scenario.introCourse.id,
        prerequisiteType: 'Strict',
        minimumRequiredStatus: 'Approved'
      }
    );
    expect(prerequisite.response.status()).toBe(201);

    const blockedByPrerequisite = await primary.session.api.enrollments.enroll({
      enrollmentPeriodId: period.id,
      shift: 'Tarde',
      studyPlanCourseIds: [scenario.advancedStudyPlanCourse.id]
    });
    expect(blockedByPrerequisite.response.status()).toBe(409);

    const enrollmentBody = {
      enrollmentPeriodId: period.id,
      shift: 'Tarde',
      studyPlanCourseIds: [scenario.introStudyPlanCourse.id]
    };
    const enroll = await primary.session.api.enrollments.enroll(enrollmentBody);
    expect(enroll.response.status()).toBe(201);

    const mine = await primary.session.api.enrollments.myEnrollments();
    expect(mine.response.status()).toBe(200);
    expect(mine.body).toMatchObject({ success: true, data: expect.any(Array) });

    const invalidReduction = await admin.enrollments.updateQuotas(period.id, {
      quotasMorning: 10,
      quotasAfternoon: 0,
      quotasEvening: 10
    });
    expect(invalidReduction.response.status()).toBe(409);

    const increase = await admin.enrollments.updateQuotas(period.id, {
      quotasMorning: 10,
      quotasAfternoon: 2,
      quotasEvening: 10
    });
    expect(increase.response.status()).toBe(200);
    expect(increase.body).toMatchObject({ data: { quotasAfternoon: 2, enrolledAfternoon: 1 } });

    const contenderBody = {
      enrollmentPeriodId: period.id,
      shift: 'Tarde',
      studyPlanCourseIds: [scenario.introStudyPlanCourse.id]
    };
    const concurrent = await Promise.all([
      contenderA.session.api.enrollments.enroll(contenderBody),
      contenderB.session.api.enrollments.enroll(contenderBody)
    ]);
    expect(concurrent.map(result => result.response.status()).sort()).toEqual([201, 409]);
    const admittedContender = concurrent[0].response.status() === 201 ? contenderA : contenderB;
    const rejectedContender = concurrent[0].response.status() === 409 ? contenderA : contenderB;

    const periodStudents = await admin.enrollments.periodStudents(period.id);
    expect(periodStudents.response.status()).toBe(200);
    expect(periodStudents.body).toMatchObject({ success: true, total: 2 });
    expect((periodStudents.body as { data: Array<{ studentId: number }> }).data.map(item => item.studentId))
      .toEqual(expect.arrayContaining([primary.studentId, admittedContender.studentId]));

    const periodList = await admin.enrollments.listPeriods();
    expect(periodList.response.status()).toBe(200);
    expect(periodList.body).toMatchObject({
      data: expect.arrayContaining([
        expect.objectContaining({ id: period.id, quotasAfternoon: 2, enrolledAfternoon: 2 })
      ])
    });

    const history = await admin.students.academicHistory(primary.studentId, 2026);
    expect(history.response.status()).toBe(200);
    expect(history.body).toMatchObject({ total: 1, items: [expect.objectContaining({ courseId: scenario.introCourse.id })] });

    const duplicate = await primary.session.api.enrollments.enroll(enrollmentBody);
    expect(duplicate.response.status()).toBe(409);

    const close = await admin.enrollments.closePeriod(period.id);
    expect(close.response.status()).toBe(200);
    const closedAttempt = await rejectedContender.session.api.enrollments.enroll(contenderBody);
    expect(closedAttempt.response.status()).toBe(409);
  } finally {
    await cleanupScenario(admin, db, careerId, userIds, periodId ? [periodId] : []);
  }
});
