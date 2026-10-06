import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  certificateIssuanceSchema,
  certificateRequestResponseSchema,
  certificateRequestSchema,
  certificateRequestsResponseSchema
} from '../../src/contracts/schemas';
import { registeredStudentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('M8 certificados: aprobación, correlativo concurrente, PDF e historial autorizado @m8 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M8 Certificados y constancias');
  await allure.story('Emisión concurrente e historial inmutable');
  await allure.severity('critical');

  let careerId = 0;
  const userIds: number[] = [];
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let otherStudentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const studentData = registeredStudentData(careerId);
    const otherData = registeredStudentData(careerId);
    const studentRegistration = await anonymous.auth.register(studentData);
    const otherRegistration = await anonymous.auth.register(otherData);
    expect(studentRegistration.response.status()).toBe(201);
    expect(otherRegistration.response.status()).toBe(201);
    const studentUserId = (studentRegistration.body as { userID: number }).userID;
    const otherUserId = (otherRegistration.body as { userID: number }).userID;
    userIds.push(studentUserId, otherUserId);
    const studentId = await db.findStudentIdByUserId(studentUserId);
    if (!studentId) throw new Error('Student profile was not created.');
    studentSession = await authenticatedClients(studentData.email, studentData.password);
    otherStudentSession = await authenticatedClients(otherData.email, otherData.password);

    expect((await anonymous.certificates.myRequests()).response.status()).toBe(401);
    expect((await admin.certificates.request({ certificateType: 'RegularStudent' })).response.status()).toBe(403);
    expect((await studentSession.api.certificates.all()).response.status()).toBe(403);
    expect((await studentSession.api.certificates.request({ certificateType: 'Certificado inexistente' })).response.status()).toBe(400);
    expect((await studentSession.api.certificates.request({ certificateType: 'Certificado de materias aprobadas' })).response.status()).toBe(409);

    const regularCall = await studentSession.api.certificates.request({ certificateType: 'Certificado de alumno regular' });
    const academicCall = await studentSession.api.certificates.request({ certificateType: 'Certificado de promedio' });
    expect(regularCall.response.status()).toBe(201);
    expect(academicCall.response.status()).toBe(201);
    const regular = certificateRequestResponseSchema.parse(regularCall.body).request;
    const academic = certificateRequestResponseSchema.parse(academicCall.body).request;
    expect(regular).toMatchObject({ kind: 'RegularStudent', status: 'Pending' });
    expect(academic).toMatchObject({ kind: 'AcademicStatus', status: 'Pending' });
    expect((await studentSession.api.certificates.request({ certificateType: 'RegularStudent' })).response.status()).toBe(409);
    expect(certificateRequestsResponseSchema.parse((await studentSession.api.certificates.myRequests()).body).requests).toHaveLength(2);

    expect((await studentSession.api.certificates.approve(regular.id)).response.status()).toBe(403);
    expect(certificateRequestSchema.parse((await admin.certificates.approve(regular.id)).body).status).toBe('Approved');
    expect(certificateRequestSchema.parse((await admin.certificates.approve(academic.id)).body).status).toBe('Approved');

    const [regularIssueCall, academicIssueCall] = await Promise.all([
      admin.certificates.issue(regular.id),
      admin.certificates.issue(academic.id)
    ]);
    expect(regularIssueCall.response.status()).toBe(200);
    expect(academicIssueCall.response.status()).toBe(200);
    const issuances = [
      certificateIssuanceSchema.parse(regularIssueCall.body),
      certificateIssuanceSchema.parse(academicIssueCall.body)
    ];
    expect(issuances.every(item => item.status === 'Ready' && item.sha256?.length === 64)).toBe(true);
    const sequenceValues = issuances.map(item => Number(item.certificateNumber.slice(5))).sort((a, b) => a - b);
    expect(sequenceValues[1] - sequenceValues[0]).toBe(1);

    const retry = certificateIssuanceSchema.parse((await admin.certificates.issue(regular.id)).body);
    expect(retry.certificateNumber).toBe(issuances[0].certificateNumber);
    const ownHistory = certificateIssuanceSchema.array().parse((await studentSession.api.certificates.myIssued()).body);
    expect(ownHistory).toHaveLength(2);
    const adminHistory = certificateIssuanceSchema.array().parse((await admin.certificates.studentHistory(studentId)).body);
    expect(adminHistory.map(item => item.certificateNumber).sort()).toEqual(ownHistory.map(item => item.certificateNumber).sort());

    const download = await studentSession.api.certificates.download(issuances[0].id);
    expect(download.response.status()).toBe(200);
    expect(download.response.headers()['content-type']).toContain('application/pdf');
    expect(download.rawBody.subarray(0, 5).toString('ascii')).toBe('%PDF-');
    expect((await otherStudentSession.api.certificates.download(issuances[0].id)).response.status()).toBe(403);
    expect((await admin.certificates.download(issuances[0].id)).response.status()).toBe(200);

    const transcriptCall = await studentSession.api.certificates.request({ certificateType: 'Certificado analítico' });
    const transcript = certificateRequestResponseSchema.parse(transcriptCall.body).request;
    expect((await studentSession.api.certificates.reject(transcript.id, 'Intento no autorizado')).response.status()).toBe(403);
    const rejected = certificateRequestSchema.parse(
      (await admin.certificates.reject(transcript.id, 'No corresponde al período solicitado')).body);
    expect(rejected).toMatchObject({ status: 'Rejected', rejectionReason: 'No corresponde al período solicitado' });

    const all = certificateRequestsResponseSchema.parse((await admin.certificates.all({ search: studentData.email })).body);
    expect(all.requests).toHaveLength(3);
  } finally {
    await studentSession?.dispose();
    await otherStudentSession?.dispose();
    await cleanupScenario(admin, db, careerId, userIds);
  }
});
