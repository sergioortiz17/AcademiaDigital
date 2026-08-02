import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { scholarshipSchema, studentRecordSchema, studentScholarshipSchema } from '../../src/contracts/schemas';
import { scholarshipData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('catálogo, asignación y revocación de becas @p1 @critical @regression @scholarships', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Becas');
  await allure.severity('critical');

  let careerId = 0;
  let userId = 0;
  const scholarshipIds: number[] = [];
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const student = await createRegisteredStudent(anonymous, db, careerId);
    userId = student.userId;

    const data = scholarshipData();
    const create = await admin.studentCatalogs.createScholarship(data);
    expect(create.response.status()).toBe(201);
    const scholarship = scholarshipSchema.parse(create.body);
    scholarshipIds.push(scholarship.id);

    expect((await admin.studentCatalogs.createScholarship(data)).response.status()).toBe(409);
    expect((await admin.studentCatalogs.getScholarship(2_147_483_647)).response.status()).toBe(404);
    expect((await admin.studentCatalogs.updateScholarship(2_147_483_647, scholarshipData())).response.status()).toBe(404);
    expect((await admin.studentCatalogs.deleteScholarship(2_147_483_647)).response.status()).toBe(404);
    expect(scholarshipSchema.parse((await admin.studentCatalogs.getScholarship(scholarship.id)).body).id).toBe(scholarship.id);
    expect(scholarshipSchema.array().parse((await admin.studentCatalogs.listScholarships()).body).map((item) => item.id)).toContain(scholarship.id);

    const update = await admin.studentCatalogs.updateScholarship(scholarship.id, {
      ...data, name: `${data.name} actualizada`, description: 'Descripción actualizada'
    });
    expect(update.response.status()).toBe(200);
    expect(scholarshipSchema.parse(update.body).name).toContain('actualizada');

    const assignmentBody = {
      scholarshipId: scholarship.id,
      academicYear: 2026,
      status: 'Granted',
      validFrom: '2026-03-01',
      validTo: '2026-12-31',
      notes: 'Otorgada por ciclo completo'
    };
    const assign = await admin.students.addScholarship(student.studentId, assignmentBody);
    expect(assign.response.status()).toBe(201);
    const studentScholarship = studentScholarshipSchema.parse(assign.body);
    expect(studentScholarship).toMatchObject({ scholarshipId: scholarship.id, status: 'Granted', academicYear: 2026 });
    expect(studentScholarship.grantedAt).not.toBeNull();

    expect((await admin.students.addScholarship(student.studentId, assignmentBody)).response.status()).toBe(409);
    expect((await admin.students.addScholarship(student.studentId, {
      ...assignmentBody, academicYear: 2027, validFrom: '2027-12-31', validTo: '2027-03-01'
    })).response.status()).toBe(400);
    expect((await admin.students.addScholarship(student.studentId, {
      ...assignmentBody, scholarshipId: 2_147_483_647, academicYear: 2027
    })).response.status()).toBe(404);

    const history = studentScholarshipSchema.array().parse((await admin.students.listScholarships(student.studentId)).body);
    expect(history).toEqual(expect.arrayContaining([expect.objectContaining({ id: studentScholarship.id, status: 'Granted' })]));
    const record = studentRecordSchema.parse((await admin.students.record(student.studentId)).body);
    expect(record.activeScholarships.map((item) => item.id)).toContain(studentScholarship.id);

    const updateAssignment = await admin.students.updateScholarship(student.studentId, studentScholarship.id, {
      ...assignmentBody, notes: 'Renovada y verificada'
    });
    expect(updateAssignment.response.status()).toBe(200);
    expect(studentScholarshipSchema.parse(updateAssignment.body).notes).toBe('Renovada y verificada');

    expect((await admin.students.revokeScholarship(student.studentId, studentScholarship.id)).response.status()).toBe(204);
    expect((await admin.students.revokeScholarship(student.studentId, studentScholarship.id)).response.status()).toBe(409);
    const revoked = studentScholarshipSchema.array().parse((await admin.students.listScholarships(student.studentId)).body);
    expect(revoked.find((item) => item.id === studentScholarship.id)?.status).toBe('Revoked');
    const recordAfterRevoke = studentRecordSchema.parse((await admin.students.record(student.studentId)).body);
    expect(recordAfterRevoke.activeScholarships.map((item) => item.id)).not.toContain(studentScholarship.id);

    const owner = await authenticatedClients(student.registration.email, student.registration.password);
    expect((await owner.api.students.listScholarships(student.studentId)).response.status()).toBe(200);
    expect((await owner.api.students.addScholarship(student.studentId, assignmentBody)).response.status()).toBe(403);
    expect((await anonymous.studentCatalogs.listScholarships()).response.status()).toBe(401);

    expect((await admin.studentCatalogs.deleteScholarship(scholarship.id)).response.status()).toBe(204);
    expect(scholarshipSchema.parse((await admin.studentCatalogs.getScholarship(scholarship.id)).body).isActive).toBe(false);
    expect((await admin.students.addScholarship(student.studentId, { ...assignmentBody, academicYear: 2028 })).response.status()).toBe(404);
  } finally {
    await db.cleanupP1Artifacts({ scholarshipIds });
    await cleanupScenario(admin, db, careerId, userId ? [userId] : []);
  }
});
