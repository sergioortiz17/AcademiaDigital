import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { problemSchema, teacherDocumentSchema, teacherSchema } from '../../src/contracts/schemas';
import { runToken } from '../../src/factories/data.factory';

test('versionado, revisión y autorización de documentos docentes @m5 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M5 Docentes');
  await allure.story('Documentación docente versionada');
  await allure.severity('critical');

  const professor = await db.createUnlinkedTeacherUser();
  const student = await db.createUnlinkedStudentUser();
  let teacherId = 0;
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const suffix = runToken();
    const teacherCall = await admin.teachers.create({
      userId: professor.id,
      employeeNumber: `doc-file-${suffix}`,
      department: 'Sistemas',
      specializationArea: 'Ingeniería de software',
      hireDate: '2026-03-01T00:00:00Z'
    });
    expect(teacherCall.response.status()).toBe(201);
    teacherId = teacherSchema.parse(teacherCall.body).id;

    const firstSubmission = {
      documentType: 'cv_docente',
      fileUrl: `https://files.e2e.local/teachers/${teacherId}/cv-v1.pdf`,
      originalFileName: 'cv-v1.pdf',
      contentType: 'application/pdf',
      fileSizeBytes: 15_000,
      validUntil: '2027-12-31'
    };

    expect((await anonymous.teachers.documents(teacherId)).response.status()).toBe(401);
    expect((await anonymous.teachers.submitDocument(teacherId, firstSubmission)).response.status()).toBe(401);

    studentSession = await authenticatedClients(student.email, student.password);
    expect((await studentSession.api.teachers.documents(teacherId)).response.status()).toBe(403);
    expect((await studentSession.api.teachers.submitDocument(teacherId, firstSubmission)).response.status()).toBe(403);
    expect((await studentSession.api.teachers.reviewDocument(teacherId, 1, {
      status: 'Approved'
    })).response.status()).toBe(403);

    const firstCall = await admin.teachers.submitDocument(teacherId, firstSubmission);
    expect(firstCall.response.status()).toBe(201);
    const first = teacherDocumentSchema.parse(firstCall.body);
    expect(first).toMatchObject({
      teacherId,
      documentType: 'CV_DOCENTE',
      version: 1,
      status: 'Submitted',
      reviewedAt: null,
      reviewedByUserId: null
    });

    const invalidReview = await admin.teachers.reviewDocument(teacherId, first.id, {
      status: 'Rejected'
    });
    expect(invalidReview.response.status()).toBe(400);
    expect(problemSchema.parse(invalidReview.body).msg?.toLowerCase()).toContain('observación');

    const approvedCall = await admin.teachers.reviewDocument(teacherId, first.id, {
      status: 'Approved', observation: 'Antecedentes verificados por E2E'
    });
    expect(approvedCall.response.status()).toBe(200);
    expect(teacherDocumentSchema.parse(approvedCall.body)).toMatchObject({
      id: first.id,
      status: 'Approved',
      reviewedByUserId: expect.any(Number),
      observation: 'Antecedentes verificados por E2E'
    });

    const secondCall = await admin.teachers.submitDocument(teacherId, {
      ...firstSubmission,
      fileUrl: `storage://teachers/${teacherId}/cv-v2.pdf`,
      originalFileName: 'cv-v2.pdf'
    });
    expect(secondCall.response.status()).toBe(201);
    const second = teacherDocumentSchema.parse(secondCall.body);
    expect(second).toMatchObject({
      teacherId,
      documentType: 'CV_DOCENTE',
      version: 2,
      status: 'Submitted'
    });

    const rejectedCall = await admin.teachers.reviewDocument(teacherId, second.id, {
      status: 'Rejected', observation: 'Falta constancia actualizada'
    });
    expect(rejectedCall.response.status()).toBe(200);
    expect(teacherDocumentSchema.parse(rejectedCall.body)).toMatchObject({
      id: second.id,
      status: 'Rejected',
      observation: 'Falta constancia actualizada'
    });

    const documentsCall = await admin.teachers.documents(teacherId);
    expect(documentsCall.response.status()).toBe(200);
    const documents = teacherDocumentSchema.array().parse(documentsCall.body);
    expect(documents).toHaveLength(2);
    expect(documents.find((document) => document.id === first.id)).toMatchObject({ version: 1, status: 'Expired' });
    expect(documents.find((document) => document.id === second.id)).toMatchObject({ version: 2, status: 'Rejected' });
  } finally {
    await studentSession?.dispose();
    if (teacherId) await db.cleanupTeacher(teacherId, professor.id);
    else await db.deleteUser(professor.id);
    await db.deleteUser(student.id);
  }
});
