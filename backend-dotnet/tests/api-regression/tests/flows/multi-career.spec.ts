import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { academicAssignmentSchema, enrollmentPeriodSchema, studentCareerSchema, studentSummarySchema } from '../../src/contracts/schemas';
import { studentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('un estudiante conserva su legajo y cursa dos carreras @smoke @critical @regression @multi-career', async ({
  admin, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Estudiantes multi-carrera');
  await allure.severity('critical');

  const user = await db.createUnlinkedStudentUser();
  let primaryCareerId = 0;
  let secondaryCareerId = 0;
  let secondaryPeriodId = 0;
  try {
    const primary = await createAcademicScenario(admin);
    const secondary = await createAcademicScenario(admin);
    primaryCareerId = primary.career.id;
    secondaryCareerId = secondary.career.id;

    const create = await admin.students.create(studentData(user.id, primaryCareerId));
    expect(create.response.status()).toBe(201);
    const student = studentSummarySchema.parse(create.body);

    const addSecondary = await admin.students.addCareer(student.id, secondaryCareerId, '2026-03-02T00:00:00Z');
    expect(addSecondary.response.status()).toBe(201);
    expect(studentCareerSchema.parse(addSecondary.body)).toMatchObject({ careerId: secondaryCareerId, isPrimary: false, isActive: true });

    const duplicate = await admin.students.addCareer(student.id, secondaryCareerId);
    expect(duplicate.response.status()).toBe(409);

    const primaryAssignment = await admin.students.assignAcademic(student.id, {
      careerId: primaryCareerId,
      studyPlanId: primary.studyPlan.id,
      commissionId: primary.commission.id,
      academicYear: 2026,
      yearNumber: 1,
      reason: 'Carrera principal'
    });
    expect(primaryAssignment.response.status()).toBe(201);

    const secondaryAssignment = await admin.students.assignAcademic(student.id, {
      careerId: secondaryCareerId,
      studyPlanId: secondary.studyPlan.id,
      commissionId: secondary.commission.id,
      academicYear: 2026,
      yearNumber: 1,
      reason: 'Segunda carrera'
    });
    expect(secondaryAssignment.response.status()).toBe(201);

    const assignments = await admin.students.assignments(student.id, 2026);
    expect(assignments.response.status()).toBe(200);
    const current = academicAssignmentSchema.array().parse(assignments.body).filter((item) => item.isCurrent);
    expect(current).toHaveLength(2);
    expect(current.map((item) => item.careerId)).toEqual(expect.arrayContaining([primaryCareerId, secondaryCareerId]));

    const refreshed = studentSummarySchema.parse((await admin.students.get(student.id)).body);
    expect(refreshed.legajoNumber).toBe(student.legajoNumber);
    expect(refreshed.careerId).toBe(primaryCareerId);
    expect(refreshed.currentStudyPlanId).toBe(primary.studyPlan.id);
    expect(refreshed.careers).toHaveLength(2);

    const defaultProgress = await admin.students.academicProgress(student.id);
    expect(defaultProgress.response.status()).toBe(200);
    expect(defaultProgress.body).toMatchObject({ careerId: primaryCareerId, studyPlanId: primary.studyPlan.id });
    const secondaryEligible = await admin.students.eligibleCourses(student.id, secondaryCareerId);
    expect(secondaryEligible.response.status()).toBe(200);
    expect(secondaryEligible.body).toHaveLength(2);

    const periodCall = await admin.enrollments.createPeriod({
      careerId: secondaryCareerId,
      studyPlanId: secondary.studyPlan.id,
      academicYear: 2026,
      semester: 1,
      quotasMorning: 10,
      quotasAfternoon: 10,
      quotasEvening: 10
    });
    expect(periodCall.response.status()).toBe(201);
    const period = enrollmentPeriodSchema.parse((periodCall.body as { data: unknown }).data);
    secondaryPeriodId = period.id;

    const studentSession = await authenticatedClients(user.email, user.password);
    const enrollment = await studentSession.api.enrollments.enroll({
      enrollmentPeriodId: period.id,
      shift: 'Mañana',
      studyPlanCourseIds: [secondary.introStudyPlanCourse.id]
    });
    expect(enrollment.response.status()).toBe(201);

    const secondaryProgress = await admin.students.academicProgress(student.id, secondaryCareerId);
    expect(secondaryProgress.response.status()).toBe(200);
    expect(secondaryProgress.body).toMatchObject({ careerId: secondaryCareerId, studyPlanId: secondary.studyPlan.id, inProgressCourses: 1 });

    const filtered = await admin.students.list({ careerId: secondaryCareerId, page: 1, pageSize: 20 });
    expect(filtered.response.status()).toBe(200);
    expect(filtered.body).toMatchObject({ items: expect.arrayContaining([expect.objectContaining({ id: student.id })]) });
  } finally {
    if (secondaryCareerId) await cleanupScenario(admin, db, secondaryCareerId, [user.id], secondaryPeriodId ? [secondaryPeriodId] : []);
    if (primaryCareerId) await cleanupScenario(admin, db, primaryCareerId, []);
    if (!primaryCareerId && !secondaryCareerId) await db.deleteUser(user.id);
  }
});
