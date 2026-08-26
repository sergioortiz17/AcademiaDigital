import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  commissionSchema,
  problemSchema,
  studentRematriculationSchema,
  studentSummarySchema
} from '../../src/contracts/schemas';
import { studentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('rematriculacion al siguiente ciclo lectivo @m4 @rematriculation @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M4 Admisiones');
  await allure.story('Rematriculacion al siguiente ciclo lectivo');
  await allure.severity('critical');

  let careerId = 0;
  const user = await db.createUnlinkedStudentUser();
  let studentClient: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const studentCall = await admin.students.create(studentData(user.id, careerId));
    expect(studentCall.response.status()).toBe(201);
    const student = studentSummarySchema.parse(studentCall.body);
    expect((await admin.students.assignStudyPlan(student.id, scenario.studyPlan.id)).response.status()).toBe(204);
    expect((await admin.students.assignAcademic(student.id, {
      careerId,
      studyPlanId: scenario.studyPlan.id,
      commissionId: scenario.commission.id,
      academicYear: 2026,
      yearNumber: 1,
      reason: 'Initial academic assignment'
    })).response.status()).toBe(201);

    const nextCommissionCall = await admin.academic.createCommission(careerId, {
      ...scenario.data.commission,
      code: `${scenario.data.commission.code}27`.slice(0, 30),
      name: `${scenario.data.commission.name} 2027`,
      academicYear: 2027,
      yearNumber: 2,
      shift: 'Evening'
    });
    expect(nextCommissionCall.response.status()).toBe(201);
    const nextCommission = commissionSchema.parse(nextCommissionCall.body);
    const body = {
      careerId,
      studyPlanId: scenario.studyPlan.id,
      commissionId: nextCommission.id,
      academicYear: 2027,
      yearNumber: 2,
      notes: 'Rematriculacion E2E'
    };

    expect((await anonymous.students.rematriculate(student.id, body)).response.status()).toBe(401);
    studentClient = await authenticatedClients(user.email, user.password);
    expect((await studentClient.api.students.rematriculate(student.id, body)).response.status()).toBe(403);

    const concurrent = await Promise.all([
      admin.students.rematriculate(student.id, body),
      admin.students.rematriculate(student.id, body)
    ]);
    expect(concurrent.map((call) => call.response.status()).sort()).toEqual([201, 409]);
    const createdCall = concurrent.find((call) => call.response.status() === 201)!;
    const conflictCall = concurrent.find((call) => call.response.status() === 409)!;
    const created = studentRematriculationSchema.parse(createdCall.body);
    expect(created).toMatchObject({
      studentId: student.id,
      careerId,
      studyPlanId: scenario.studyPlan.id,
      commissionId: nextCommission.id,
      academicYear: 2027,
      yearNumber: 2,
      shift: 'Evening',
      notes: 'Rematriculacion E2E'
    });
    expect(problemSchema.parse(conflictCall.body).msg).toContain('already has a rematriculation');
    expect(await db.countStudentRematriculations(student.id)).toBe(1);
    expect(await db.getCurrentAcademicAssignment(student.id)).toEqual({
      academicYear: 2027,
      yearNumber: 2,
      commissionId: nextCommission.id
    });
  } finally {
    await studentClient?.dispose();
    await cleanupScenario(admin, db, careerId, [user.id]);
  }
});
