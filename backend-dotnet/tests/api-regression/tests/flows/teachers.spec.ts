import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { problemSchema, teacherSchema } from '../../src/contracts/schemas';
import { runToken } from '../../src/factories/data.factory';

test('CRUD administrativo y baja lógica del legajo docente @m5 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M5 Docentes');
  await allure.story('Legajo docente y baja lógica');
  await allure.severity('critical');

  const professor = await db.createUnlinkedTeacherUser();
  const otherProfessor = await db.createUnlinkedTeacherUser();
  const student = await db.createUnlinkedStudentUser();
  let teacherId = 0;
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const suffix = runToken();
    const body = {
      userId: professor.id,
      employeeNumber: `doc-${suffix}`,
      department: 'Ingeniería',
      specializationArea: 'Arquitectura de software',
      hireDate: '2026-03-01T00:00:00Z',
      phoneNumber: '3415550100',
      addressLine: 'Calle Docente 123',
      city: 'Rosario',
      province: 'Santa Fe',
      postalCode: '2000',
      emergencyContactName: 'Contacto Docente',
      emergencyContactRelationship: 'Familiar',
      emergencyContactPhone: '3415550101'
    };

    expect((await anonymous.teachers.list()).response.status()).toBe(401);
    expect((await anonymous.teachers.create(body)).response.status()).toBe(401);
    studentSession = await authenticatedClients(student.email, student.password);
    expect((await studentSession.api.teachers.list()).response.status()).toBe(403);
    expect((await studentSession.api.teachers.create(body)).response.status()).toBe(403);

    const createCall = await admin.teachers.create(body);
    expect(createCall.response.status()).toBe(201);
    const created = teacherSchema.parse(createCall.body);
    teacherId = created.id;
    expect(created).toMatchObject({
      userId: professor.id,
      employeeNumber: body.employeeNumber.toUpperCase(),
      department: 'Ingeniería',
      city: 'Rosario',
      isActive: true,
      deactivatedAt: null
    });
    const detailCall = await admin.teachers.get(teacherId);
    expect(detailCall.response.status()).toBe(200);
    expect(teacherSchema.parse(detailCall.body)).toMatchObject({ id: teacherId, userId: professor.id });

    const duplicateUser = await admin.teachers.create({ ...body, employeeNumber: `${body.employeeNumber}-2` });
    expect(duplicateUser.response.status()).toBe(409);
    expect(problemSchema.parse(duplicateUser.body).msg).toContain('user');

    const duplicateEmployee = await admin.teachers.create({ ...body, userId: otherProfessor.id });
    expect(duplicateEmployee.response.status()).toBe(409);
    expect(problemSchema.parse(duplicateEmployee.body).msg).toContain('employee number');

    const updateCall = await admin.teachers.update(teacherId, { ...body, department: 'Sistemas', city: 'Funes' });
    expect(updateCall.response.status()).toBe(200);
    expect(teacherSchema.parse(updateCall.body)).toMatchObject({
      id: teacherId, department: 'Sistemas', city: 'Funes', isActive: true
    });

    const activeList = teacherSchema.array().parse((await admin.teachers.list()).body);
    expect(activeList.some((teacher) => teacher.id === teacherId)).toBe(true);

    expect((await admin.teachers.deactivate(teacherId, 'Fin de designación E2E')).response.status()).toBe(204);
    expect((await admin.teachers.deactivate(teacherId, 'Segundo intento')).response.status()).toBe(204);
    const afterDelete = teacherSchema.array().parse((await admin.teachers.list()).body);
    expect(afterDelete.some((teacher) => teacher.id === teacherId)).toBe(false);
    const withInactive = teacherSchema.array().parse((await admin.teachers.list(true)).body);
    const inactive = withInactive.find((teacher) => teacher.id === teacherId);
    expect(inactive).toMatchObject({
      isActive: false,
      deactivatedByUserId: expect.any(Number),
      deactivationReason: 'Fin de designación E2E'
    });
    expect(inactive?.deactivatedAt).not.toBeNull();
  } finally {
    await studentSession?.dispose();
    if (teacherId) await db.cleanupTeacher(teacherId, professor.id);
    else await db.deleteUser(professor.id);
    await db.deleteUser(otherProfessor.id);
    await db.deleteUser(student.id);
  }
});
