import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('acceso propio y cruzado a documentos, becas y campos @p1 @critical @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Autorización P1');
  await allure.severity('critical');

  let careerId = 0;
  const userIds: number[] = [];
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const owner = await createRegisteredStudent(anonymous, db, careerId);
    const other = await createRegisteredStudent(anonymous, db, careerId);
    userIds.push(owner.userId, other.userId);
    const session = await authenticatedClients(owner.registration.email, owner.registration.password);

    expect((await session.api.students.listDocuments(owner.studentId)).response.status()).toBe(200);
    expect((await session.api.students.pendingDocuments(owner.studentId)).response.status()).toBe(200);
    expect((await session.api.students.listScholarships(owner.studentId)).response.status()).toBe(200);
    expect((await session.api.students.getCustomValues(owner.studentId)).response.status()).toBe(200);

    expect((await session.api.students.listDocuments(other.studentId)).response.status()).toBe(403);
    expect((await session.api.students.pendingDocuments(other.studentId)).response.status()).toBe(403);
    expect((await session.api.students.listScholarships(other.studentId)).response.status()).toBe(403);
    expect((await session.api.students.getCustomValues(other.studentId)).response.status()).toBe(403);

    expect((await session.api.students.addDocument(owner.studentId, {
      documentRequirementId: 2_147_483_647,
      fileUrl: 'https://files.example.edu/forbidden.pdf',
      originalFileName: 'forbidden.pdf',
      contentType: 'application/pdf',
      fileSizeBytes: 1
    })).response.status()).toBe(403);
    expect((await session.api.students.addScholarship(owner.studentId, {
      scholarshipId: 2_147_483_647,
      academicYear: 2026,
      status: 'Requested',
      validFrom: null,
      validTo: null,
      notes: null
    })).response.status()).toBe(403);
    expect((await session.api.students.saveCustomValues(owner.studentId, {})).response.status()).toBe(403);
    expect((await session.api.studentCatalogs.listDocumentRequirements()).response.status()).toBe(403);
    expect((await session.api.studentCatalogs.listScholarships()).response.status()).toBe(403);
    expect((await session.api.studentCatalogs.listCustomFields()).response.status()).toBe(403);

    expect((await anonymous.students.listDocuments(owner.studentId)).response.status()).toBe(401);
    expect((await anonymous.studentCatalogs.listDocumentRequirements()).response.status()).toBe(401);
  } finally {
    await cleanupScenario(admin, db, careerId, userIds);
  }
});
