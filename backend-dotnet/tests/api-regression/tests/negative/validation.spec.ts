import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { academicData, registeredStudentData, studentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('validaciones de DTO y JSON de estudiantes @negative @validation @regression', async ({ admin }) => {
  await allure.feature('Validaciones');
  const empty = await admin.students.create({});
  expect(empty.response.status()).toBe(400);

  const malformed = await admin.students.createMalformed('{"userId": 1,');
  expect(malformed.response.status()).toBe(400);

  const invalidPagination = await admin.students.list({ page: 0, pageSize: 101 });
  expect(invalidPagination.response.status()).toBe(400);
});

test('rechaza duplicados y relaciones académicas incompatibles @critical @negative @validation', async ({ admin, db }) => {
  const users = [await db.createUnlinkedStudentUser()];
  let firstCareerId = 0;
  let secondCareerId = 0;
  try {
    const first = await createAcademicScenario(admin);
    const second = await createAcademicScenario(admin);
    firstCareerId = first.career.id;
    secondCareerId = second.career.id;

    const duplicateCareer = await admin.academic.createCareer(first.data.career);
    expect(duplicateCareer.response.status()).toBe(409);

    const crossCourse = await admin.academic.addStudyPlanCourse(first.studyPlan.id, {
      courseId: second.introCourse.id,
      yearNumber: 1,
      semester: 1,
      isMandatory: true,
      sortOrder: 9
    });
    expect(crossCourse.response.status()).toBe(409);

    const studentCall = await admin.students.create(studentData(users[0].id, first.career.id));
    expect(studentCall.response.status()).toBe(201);
    const studentId = (studentCall.body as { id: number }).id;
    const incompatible = await admin.students.assignAcademic(studentId, {
      careerId: first.career.id,
      studyPlanId: first.studyPlan.id,
      commissionId: second.commission.id,
      academicYear: 2026,
      yearNumber: 1,
      reason: 'Debe fallar'
    });
    expect(incompatible.response.status()).toBe(409);

    const invalidRange = await admin.academic.createCommission(first.career.id, {
      code: 'INVALID-RANGE', name: 'Invalid', academicYear: 1999, yearNumber: 0, shift: 'Morning'
    });
    expect(invalidRange.response.status()).toBe(400);
  } finally {
    await cleanupScenario(admin, db, firstCareerId, users.map((user) => user.id));
    if (secondCareerId) await cleanupScenario(admin, db, secondCareerId, []);
  }
});

test('alta de estudiante es atómica ante comisión incompatible @critical @negative @regression', async ({ admin, db }) => {
  const user = await db.createUnlinkedStudentUser();
  let firstCareerId = 0;
  let secondCareerId = 0;
  try {
    const first = await createAcademicScenario(admin);
    const second = await createAcademicScenario(admin);
    firstCareerId = first.career.id;
    secondCareerId = second.career.id;

    const response = await admin.students.create({
      ...studentData(user.id, first.career.id),
      studyPlanId: first.studyPlan.id,
      commissionId: second.commission.id,
      academicYear: 2026,
      yearNumber: 1
    });
    expect(response.response.status()).toBe(409);

    expect(await db.findStudentIdByUserId(user.id)).toBeNull();

    const incomplete = await admin.students.create({
      ...studentData(user.id, first.career.id),
      studyPlanId: first.studyPlan.id
    });
    expect(incomplete.response.status()).toBe(400);
    expect(await db.findStudentIdByUserId(user.id)).toBeNull();

    const missingCommission = await admin.students.create({
      ...studentData(user.id, first.career.id),
      studyPlanId: first.studyPlan.id,
      commissionId: 2_147_483_647,
      academicYear: 2026,
      yearNumber: 1
    });
    expect(missingCommission.response.status()).toBe(404);
    expect(await db.findStudentIdByUserId(user.id)).toBeNull();
  } finally {
    await cleanupScenario(admin, db, firstCareerId, [user.id]);
    if (secondCareerId) await cleanupScenario(admin, db, secondCareerId, []);
  }
});

test('registro con carrera inexistente no deja User huérfano @critical @negative @regression', async ({ anonymous, db }) => {
  const registration = registeredStudentData(2_147_483_647);
  const response = await anonymous.auth.register(registration);
  expect(response.response.status()).toBe(404);
  expect(await db.findUserIdByEmail(registration.email)).toBeNull();
});

test('alta combinada crea un único plan y asignación @critical @regression', async ({ admin, db }) => {
  const user = await db.createUnlinkedStudentUser();
  let careerId = 0;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const response = await admin.students.create({
      ...studentData(user.id, careerId),
      studyPlanId: scenario.studyPlan.id,
      commissionId: scenario.commission.id,
      academicYear: 2026,
      yearNumber: 1
    });
    expect(response.response.status()).toBe(201);
    const studentId = (response.body as { id: number }).id;
    expect(await db.countStudentCareers(studentId)).toBe(1);
    expect(await db.countStudentStudyPlans(studentId)).toBe(1);
    const assignments = await admin.students.assignments(studentId, 2026);
    expect(assignments.response.status()).toBe(200);
    expect(assignments.body).toMatchObject([expect.objectContaining({ isCurrent: true, careerId })]);
  } finally {
    await cleanupScenario(admin, db, careerId, [user.id]);
  }
});

test('valida límites de carrera @negative @validation', async ({ admin }) => {
  const invalid = academicData();
  const response = await admin.academic.createCareer({ ...invalid.career, code: 'X'.repeat(21), durationYears: 0, totalCredits: -1 });
  expect(response.response.status()).toBe(400);
});

test('dos altas concurrentes crean un solo Student por User @critical @negative @regression', async ({ admin, db }) => {
  const user = await db.createUnlinkedStudentUser();
  let careerId = 0;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const [first, second] = await Promise.all([
      admin.students.create(studentData(user.id, careerId)),
      admin.students.create(studentData(user.id, careerId))
    ]);

    expect([first.response.status(), second.response.status()].sort()).toEqual([201, 409]);
    const studentId = await db.findStudentIdByUserId(user.id);
    expect(studentId).not.toBeNull();
    expect(await db.countStudentCareers(studentId!)).toBe(1);
  } finally {
    await cleanupScenario(admin, db, careerId, [user.id]);
  }
});
