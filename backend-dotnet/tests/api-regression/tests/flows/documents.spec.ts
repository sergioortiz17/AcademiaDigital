import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { careerSchema, documentRequirementSchema, studentDocumentSchema } from '../../src/contracts/schemas';
import { academicData, documentRequirementData, studentDocumentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('requisitos y ciclo de documentos del estudiante @p1 @critical @regression @documents', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Documentos del estudiante');
  await allure.severity('critical');

  let primaryCareerId = 0;
  let secondaryCareerId = 0;
  let userId = 0;
  const requirementIds: number[] = [];
  try {
    const primary = await createAcademicScenario(admin);
    primaryCareerId = primary.career.id;
    const secondaryCall = await admin.academic.createCareer(academicData().career);
    expect(secondaryCall.response.status()).toBe(201);
    secondaryCareerId = careerSchema.parse(secondaryCall.body).id;

    const student = await createRegisteredStudent(anonymous, db, primaryCareerId);
    userId = student.userId;
    expect((await admin.students.addCareer(student.studentId, secondaryCareerId)).response.status()).toBe(201);

    const generalCall = await admin.studentCatalogs.createDocumentRequirement(documentRequirementData(null));
    const primaryCall = await admin.studentCatalogs.createDocumentRequirement(documentRequirementData(primaryCareerId));
    const secondaryCallRequirement = await admin.studentCatalogs.createDocumentRequirement(documentRequirementData(secondaryCareerId));
    expect(generalCall.response.status()).toBe(201);
    expect(primaryCall.response.status()).toBe(201);
    expect(secondaryCallRequirement.response.status()).toBe(201);
    const general = documentRequirementSchema.parse(generalCall.body);
    const primaryRequirement = documentRequirementSchema.parse(primaryCall.body);
    const secondaryRequirement = documentRequirementSchema.parse(secondaryCallRequirement.body);
    requirementIds.push(general.id, primaryRequirement.id, secondaryRequirement.id);

    expect((await admin.studentCatalogs.createDocumentRequirement({
      code: general.code,
      name: general.name,
      description: general.description,
      careerId: general.careerId,
      isRequired: general.isRequired,
      validFrom: general.validFrom,
      validTo: general.validTo
    })).response.status()).toBe(409);
    expect((await admin.studentCatalogs.createDocumentRequirement(documentRequirementData(2_147_483_647))).response.status()).toBe(404);
    expect((await admin.studentCatalogs.createDocumentRequirement({
      ...documentRequirementData(null), validFrom: '2026-12-31', validTo: '2026-01-01'
    })).response.status()).toBe(400);
    expect((await admin.studentCatalogs.getDocumentRequirement(2_147_483_647)).response.status()).toBe(404);

    const filtered = documentRequirementSchema.array().parse((await admin.studentCatalogs.listDocumentRequirements(primaryCareerId)).body);
    expect(filtered.map((item) => item.id)).toEqual(expect.arrayContaining([general.id, primaryRequirement.id]));
    expect(filtered.map((item) => item.id)).not.toContain(secondaryRequirement.id);
    expect(documentRequirementSchema.parse((await admin.studentCatalogs.getDocumentRequirement(primaryRequirement.id)).body).careerId)
      .toBe(primaryCareerId);

    const updatedRequirement = await admin.studentCatalogs.updateDocumentRequirement(primaryRequirement.id, {
      ...documentRequirementData(primaryCareerId),
      code: primaryRequirement.code,
      name: `${primaryRequirement.name} actualizado`
    });
    expect(updatedRequirement.response.status()).toBe(200);
    expect(documentRequirementSchema.parse(updatedRequirement.body).name).toContain('actualizado');

    const pending = documentRequirementSchema.array().parse((await admin.students.pendingDocuments(student.studentId)).body);
    expect(pending.map((item) => item.id)).toEqual(expect.arrayContaining(requirementIds));

    const unsupported = await admin.students.addDocument(student.studentId, {
      ...studentDocumentData(general.id), contentType: 'text/plain'
    });
    expect(unsupported.response.status()).toBe(400);
    const empty = await admin.students.addDocument(student.studentId, {
      ...studentDocumentData(general.id), fileSizeBytes: 0
    });
    expect(empty.response.status()).toBe(400);
    expect((await admin.students.addDocument(student.studentId, studentDocumentData(2_147_483_647))).response.status()).toBe(404);

    const firstCall = await admin.students.addDocument(student.studentId, studentDocumentData(general.id));
    expect(firstCall.response.status()).toBe(201);
    const first = studentDocumentSchema.parse(firstCall.body);
    expect(first.status).toBe('Submitted');

    const secondCall = await admin.students.addDocument(student.studentId, studentDocumentData(general.id));
    expect(secondCall.response.status()).toBe(201);
    const second = studentDocumentSchema.parse(secondCall.body);
    const versions = studentDocumentSchema.array().parse((await admin.students.listDocuments(student.studentId)).body);
    expect(versions.find((item) => item.id === first.id)?.status).toBe('Expired');
    expect(versions.find((item) => item.id === second.id)?.status).toBe('Submitted');
    expect(studentDocumentSchema.parse((await admin.students.getDocument(student.studentId, second.id)).body).id).toBe(second.id);

    expect((await admin.students.reviewDocument(student.studentId, second.id, { status: 'Rejected', observation: '' })).response.status()).toBe(400);
    const rejected = await admin.students.reviewDocument(student.studentId, second.id, { status: 'Rejected', observation: 'Archivo ilegible' });
    expect(rejected.response.status()).toBe(200);
    expect(studentDocumentSchema.parse(rejected.body)).toMatchObject({ status: 'Rejected', observation: 'Archivo ilegible' });
    expect((await admin.students.deleteDocument(student.studentId, second.id)).response.status()).toBe(204);
    expect(studentDocumentSchema.parse((await admin.students.getDocument(student.studentId, second.id)).body).status).toBe('Expired');

    const approvedCall = await admin.students.addDocument(student.studentId, studentDocumentData(general.id));
    const approvedCandidate = studentDocumentSchema.parse(approvedCall.body);
    const approvedCallResult = await admin.students.reviewDocument(student.studentId, approvedCandidate.id, { status: 'Approved', observation: 'Documento legible' });
    expect(approvedCallResult.response.status()).toBe(200);
    const approved = studentDocumentSchema.parse(approvedCallResult.body);
    expect(approved.status).toBe('Approved');
    expect(approved.reviewedAt).not.toBeNull();
    expect((await admin.students.deleteDocument(student.studentId, approved.id)).response.status()).toBe(409);

    const pendingAfterApproval = documentRequirementSchema.array().parse((await admin.students.pendingDocuments(student.studentId)).body);
    expect(pendingAfterApproval.map((item) => item.id)).not.toContain(general.id);
    expect(pendingAfterApproval.map((item) => item.id)).toEqual(expect.arrayContaining([primaryRequirement.id, secondaryRequirement.id]));

    expect((await admin.studentCatalogs.deleteDocumentRequirement(secondaryRequirement.id)).response.status()).toBe(204);
    expect((await admin.studentCatalogs.getDocumentRequirement(secondaryRequirement.id)).response.status()).toBe(404);
    const pendingAfterDisable = documentRequirementSchema.array().parse((await admin.students.pendingDocuments(student.studentId)).body);
    expect(pendingAfterDisable.map((item) => item.id)).not.toContain(secondaryRequirement.id);

    const owner = await authenticatedClients(student.registration.email, student.registration.password);
    expect((await owner.api.students.listDocuments(student.studentId)).response.status()).toBe(200);
    expect((await owner.api.students.pendingDocuments(student.studentId)).response.status()).toBe(200);
    expect((await owner.api.students.addDocument(student.studentId, studentDocumentData(primaryRequirement.id))).response.status()).toBe(403);
    expect((await anonymous.students.pendingDocuments(student.studentId)).response.status()).toBe(401);
  } finally {
    await db.cleanupP1Artifacts({ documentRequirementIds: requirementIds });
    if (secondaryCareerId) await cleanupScenario(admin, db, secondaryCareerId, userId ? [userId] : []);
    if (primaryCareerId) await cleanupScenario(admin, db, primaryCareerId, secondaryCareerId ? [] : userId ? [userId] : []);
  }
});
