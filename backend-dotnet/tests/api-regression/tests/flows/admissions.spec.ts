import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  admissionApplicationDetailSchema,
  admissionApplicationDocumentSchema,
  admissionAgreementSchema,
  admissionApplicationPageSchema,
  admissionApplicationSchema,
  admissionExpirationResultSchema,
  admissionFormSchema,
  admissionOutboxResultSchema,
  careerSchema,
  commissionSchema,
  documentRequirementSchema,
  problemSchema
} from '../../src/contracts/schemas';
import { academicData, documentRequirementData, runToken, studentDocumentData } from '../../src/factories/data.factory';

test('formulario público y solicitud de admisión @m4 @admissions @critical @regression', async ({ anonymous, db }) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M4 Admisiones');
  await allure.story('Formulario público y preinscripción');
  await allure.severity('critical');

  const seeded = await db.seedAdmissionForm();
  try {
    const formCall = await anonymous.admissions.getForm(seeded.slug);
    expect(formCall.response.status()).toBe(200);
    const form = admissionFormSchema.parse((formCall.body as { data: unknown }).data);
    expect(form).toMatchObject({
      slug: seeded.slug,
      reservationHours: 72,
      fields: [
        expect.objectContaining({ key: 'email', isRequired: true }),
        expect.objectContaining({ key: 'dni', isRequired: true }),
        expect.objectContaining({ key: 'firstName', isRequired: true }),
        expect.objectContaining({ key: 'phone', isRequired: false })
      ]
    });

    const suffix = runToken();
    const body = {
      formSlug: seeded.slug,
      acceptedTerms: true,
      fields: {
        email: `admission.${suffix.toLowerCase()}@e2e.local`,
        dni: String(Number.parseInt(suffix, 36) % 90_000_000 + 10_000_000),
        firstName: 'Ada',
        phone: '3515550110'
      }
    };

    const missingChallenge = await anonymous.admissions.createApplication(body, null);
    expect(missingChallenge.response.status()).toBe(403);
    expect(problemSchema.parse(missingChallenge.body).msg).toContain('verificación del desafío');

    const invalidChallenge = await anonymous.admissions.createApplication(body, 'invalid-challenge');
    expect(invalidChallenge.response.status()).toBe(403);
    expect(problemSchema.parse(invalidChallenge.body).msg).toContain('verificación del desafío');
    expect(await db.countAdmissionApplications(seeded.formId)).toBe(0);

    const createdCall = await anonymous.admissions.createApplication(body);
    expect(createdCall.response.status()).toBe(201);
    const created = admissionApplicationSchema.parse((createdCall.body as { data: unknown }).data);
    expect(created.status).toBe('PreEnrolled');
    if (!created.reservationExpiresAt) throw new Error('PreEnrolled application must have a reservation expiration.');
    expect(new Date(created.reservationExpiresAt).getTime() - new Date(created.createdAt).getTime())
      .toBe(72 * 60 * 60 * 1000);
    expect(await db.countAdmissionApplications(seeded.formId)).toBe(1);

    const duplicate = await anonymous.admissions.createApplication(body);
    expect(duplicate.response.status()).toBe(409);
    expect(problemSchema.parse(duplicate.body).msg).toContain('Ya existe una solicitud de admisión');
    expect(await db.countAdmissionApplications(seeded.formId)).toBe(1);

    const missingTerms = await anonymous.admissions.createApplication({ ...body, acceptedTerms: false });
    expect(missingTerms.response.status()).toBe(400);

    const burst = await Promise.all(
      Array.from({ length: 16 }, () => anonymous.admissions.createApplication({}))
    );
    const rateLimited = burst.filter(result => result.response.status() === 429);
    expect(rateLimited.length).toBeGreaterThanOrEqual(6);
    expect(rateLimited[0].response.headers()['retry-after']).toBeDefined();
    expect(problemSchema.parse(rateLimited[0].body).msg).toContain('Too many admission attempts');

    // Allow the one-second E2E limiter window to reset before the next scenario.
    await new Promise(resolve => setTimeout(resolve, 1_100));
  } finally {
    await db.cleanupAdmissionForm(seeded.formId, seeded.careerId);
  }
});

test('cupo aislado por comision y turno @m4 @admissions @critical @regression', async ({
  admin, anonymous, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M4 Admisiones');
  await allure.story('Destino academico y cupo por comision');
  await allure.severity('critical');

  const data = academicData();
  const careerCall = await admin.academic.createCareer(data.career);
  expect(careerCall.response.status()).toBe(201);
  const career = careerSchema.parse(careerCall.body);
  const suffix = data.suffix.toLowerCase();
  const formIds: number[] = [];

  try {
    const morningCall = await admin.academic.createCommission(career.id, data.commission);
    const eveningCall = await admin.academic.createCommission(career.id, {
      ...data.commission,
      code: `${data.commission.code}E`.slice(0, 30),
      name: `${data.commission.name} Noche`,
      shift: 'Evening'
    });
    expect(morningCall.response.status()).toBe(201);
    expect(eveningCall.response.status()).toBe(201);
    const morning = commissionSchema.parse(morningCall.body);
    const evening = commissionSchema.parse(eveningCall.body);

    const formBody = (commissionId: number, slug: string, title: string) => ({
      careerId: career.id,
      commissionId,
      slug,
      title,
      description: 'Formulario E2E dirigido a una comision',
      termsText: 'Acepto los terminos de admision.',
      reservationHours: 24,
      capacity: 1,
      fields: [
        { key: 'email', label: 'Correo', type: 1, isRequired: true, sortOrder: 1 },
        { key: 'dni', label: 'DNI', type: 0, isRequired: true, sortOrder: 2 }
      ]
    });
    const morningFormCall = await admin.admissions.createForm(
      formBody(morning.id, `morning-${suffix}`, `Ingreso manana ${suffix}`));
    expect(morningFormCall.response.status()).toBe(201);
    const morningForm = admissionFormSchema.parse((morningFormCall.body as { data: unknown }).data);
    formIds.push(morningForm.id);
    expect(morningForm).toMatchObject({
      commissionId: morning.id,
      commissionCode: morning.code,
      commissionName: morning.name,
      academicYear: morning.academicYear,
      yearNumber: morning.yearNumber,
      shift: 'Morning',
      capacity: 1
    });
    const unlimitedTarget = await admin.admissions.setFormCapacity(morningForm.id, null);
    expect(unlimitedTarget.response.status()).toBe(400);

    const duplicateTarget = await admin.admissions.createForm(
      formBody(morning.id, `morning-duplicate-${suffix}`, `Ingreso duplicado ${suffix}`));
    expect(duplicateTarget.response.status()).toBe(409);
    expect(problemSchema.parse(duplicateTarget.body).msg).toContain('ya tiene un formulario de admisión');

    const eveningFormCall = await admin.admissions.createForm(
      formBody(evening.id, `evening-${suffix}`, `Ingreso noche ${suffix}`));
    expect(eveningFormCall.response.status()).toBe(201);
    const eveningForm = admissionFormSchema.parse((eveningFormCall.body as { data: unknown }).data);
    formIds.push(eveningForm.id);
    expect(eveningForm.shift).toBe('Evening');

    const submit = (formSlug: string, marker: string, dni: string) => anonymous.admissions.createApplication({
      formSlug,
      acceptedTerms: true,
      fields: { email: `${marker}.${suffix}@e2e.local`, dni }
    });
    const morningReserved = admissionApplicationSchema.parse(
      ((await submit(morningForm.slug, 'morning-1', '93000001')).body as { data: unknown }).data);
    const morningWaiting = admissionApplicationSchema.parse(
      ((await submit(morningForm.slug, 'morning-2', '93000002')).body as { data: unknown }).data);
    const eveningReserved = admissionApplicationSchema.parse(
      ((await submit(eveningForm.slug, 'evening-1', '93000003')).body as { data: unknown }).data);

    expect(morningReserved.status).toBe('PreEnrolled');
    expect(morningWaiting.status).toBe('Waitlisted');
    expect(eveningReserved.status).toBe('PreEnrolled');
  } finally {
    for (const formId of formIds.reverse()) await db.cleanupAdmissionForm(formId, career.id);
    await admin.academic.deleteCareer(career.id);
  }
});

test('cupo serializable, expiración y promoción FIFO @m4 @admissions @critical @regression', async ({
  admin, anonymous, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M4 Admisiones');
  await allure.story('Cupo, expiración y lista de espera FIFO');
  await allure.severity('critical');

  const careerCall = await admin.academic.createCareer(academicData().career);
  expect(careerCall.response.status()).toBe(201);
  const career = careerSchema.parse(careerCall.body);
  let formId: number | undefined;

  try {
    const suffix = runToken().toLowerCase();
    const formCall = await admin.admissions.createForm({
      careerId: career.id,
      slug: `capacity-${suffix}`,
      title: `Ingreso con cupo ${suffix}`,
      description: 'Formulario E2E con una vacante',
      termsText: 'Acepto los términos de admisión.',
      reservationHours: 24,
      capacity: 1,
      fields: [
        { key: 'email', label: 'Correo', type: 1, isRequired: true, sortOrder: 1 },
        { key: 'dni', label: 'DNI', type: 0, isRequired: true, sortOrder: 2 }
      ]
    });
    expect(formCall.response.status()).toBe(201);
    const form = admissionFormSchema.parse((formCall.body as { data: unknown }).data);
    formId = form.id;
    expect(form.capacity).toBe(1);

    const applicationBody = (position: number) => ({
      formSlug: form.slug,
      acceptedTerms: true,
      fields: {
        email: `capacity.${suffix}.${position}@e2e.local`,
        dni: String(80_000_000 + position)
      }
    });
    const concurrentCalls = await Promise.all([
      anonymous.admissions.createApplication(applicationBody(1)),
      anonymous.admissions.createApplication(applicationBody(2))
    ]);
    expect(concurrentCalls.map((call) => call.response.status())).toEqual([201, 201]);
    const concurrent = concurrentCalls.map((call) =>
      admissionApplicationSchema.parse((call.body as { data: unknown }).data));
    expect(concurrent.filter((application) => application.status === 'PreEnrolled')).toHaveLength(1);
    expect(concurrent.filter((application) => application.status === 'Waitlisted')).toHaveLength(1);

    const reserved = concurrent.find((application) => application.status === 'PreEnrolled')!;
    const firstWaiting = concurrent.find((application) => application.status === 'Waitlisted')!;
    expect(firstWaiting.reservationExpiresAt).toBeNull();

    const thirdCall = await anonymous.admissions.createApplication(applicationBody(3));
    expect(thirdCall.response.status()).toBe(201);
    const third = admissionApplicationSchema.parse((thirdCall.body as { data: unknown }).data);
    expect(third.status).toBe('Waitlisted');

    await db.expireAdmissionReservation(reserved.publicId);
    const processCall = await admin.admissions.processExpirations(form.id);
    expect(processCall.response.status()).toBe(200);
    const processed = admissionExpirationResultSchema.parse((processCall.body as { data: unknown }).data);
    expect(processed).toEqual({ formsProcessed: 1, expired: 1, promoted: 1 });

    const expiredDetail = admissionApplicationDetailSchema.parse(
      ((await admin.admissions.getApplication(reserved.publicId)).body as { data: unknown }).data
    );
    const promotedDetail = admissionApplicationDetailSchema.parse(
      ((await admin.admissions.getApplication(firstWaiting.publicId)).body as { data: unknown }).data
    );
    const waitingDetail = admissionApplicationDetailSchema.parse(
      ((await admin.admissions.getApplication(third.publicId)).body as { data: unknown }).data
    );
    expect(expiredDetail.application.status).toBe('Expired');
    expect(promotedDetail.application.status).toBe('PreEnrolled');
    expect(promotedDetail.history.at(-1)?.reason).toContain('FIFO');
    expect(waitingDetail.application.status).toBe('Waitlisted');

    const increaseCall = await admin.admissions.setFormCapacity(form.id, 2);
    expect(increaseCall.response.status()).toBe(200);
    expect(admissionFormSchema.parse((increaseCall.body as { data: unknown }).data).capacity).toBe(2);
    const secondPromotion = admissionApplicationDetailSchema.parse(
      ((await admin.admissions.getApplication(third.publicId)).body as { data: unknown }).data
    );
    expect(secondPromotion.application.status).toBe('PreEnrolled');

    const invalidReduction = await admin.admissions.setFormCapacity(form.id, 1);
    expect(invalidReduction.response.status()).toBe(409);
  } finally {
    if (formId) await db.cleanupAdmissionForm(formId, career.id);
    await admin.academic.deleteCareer(career.id);
  }
});

test('administración y transición auditada de admisiones @m4 @admissions @authorization @critical @regression', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M4 Admisiones');
  await allure.story('Administración y auditoría de solicitudes');
  await allure.severity('critical');

  const careerCall = await admin.academic.createCareer(academicData().career);
  expect(careerCall.response.status()).toBe(201);
  const career = careerSchema.parse(careerCall.body);
  const user = await db.createUnlinkedStudentUser();
  const student = await authenticatedClients(user.email, user.password);
  let formId: number | undefined;
  const requirementIds: number[] = [];

  try {
    expect((await anonymous.admissions.getForms()).response.status()).toBe(401);
    expect((await student.api.admissions.getForms()).response.status()).toBe(403);

    const suffix = runToken().toLowerCase();
    const formBody = {
      careerId: career.id,
      slug: `admin-${suffix}`,
      title: `Ingreso administrado ${suffix}`,
      description: 'Formulario creado por la API administrativa',
      termsText: 'Acepto los términos de admisión.',
      reservationHours: 48,
      fields: [
        { key: 'email', label: 'Correo', type: 1, isRequired: true, sortOrder: 1 },
        { key: 'dni', label: 'DNI', type: 0, isRequired: true, sortOrder: 2 },
        { key: 'firstName', label: 'Nombre', type: 0, isRequired: true, sortOrder: 3 }
      ]
    };
    const createFormCall = await admin.admissions.createForm(formBody);
    expect(createFormCall.response.status()).toBe(201);
    const form = admissionFormSchema.parse((createFormCall.body as { data: unknown }).data);
    formId = form.id;
    expect(form).toMatchObject({ slug: formBody.slug, isActive: true, reservationHours: 48 });

    const formsCall = await admin.admissions.getForms();
    expect(formsCall.response.status()).toBe(200);
    const forms = admissionFormSchema.array().parse((formsCall.body as { data: unknown }).data);
    expect(forms.some((candidate) => candidate.id === form.id)).toBe(true);

    const publicForm = await anonymous.admissions.getForm(form.slug);
    expect(publicForm.response.status()).toBe(200);

    const email = `admission.admin.${suffix}@e2e.local`;
    const applicationCall = await anonymous.admissions.createApplication({
      formSlug: form.slug,
      acceptedTerms: true,
      fields: { email, dni: user.dni, firstName: 'Ada' }
    });
    expect(applicationCall.response.status()).toBe(201);
    const application = admissionApplicationSchema.parse((applicationCall.body as { data: unknown }).data);

    const pageCall = await admin.admissions.getApplications(
      `?admissionFormId=${form.id}&status=0&search=${encodeURIComponent(email)}&page=1&pageSize=10`
    );
    expect(pageCall.response.status()).toBe(200);
    const page = admissionApplicationPageSchema.parse((pageCall.body as { data: unknown }).data);
    expect(page.total).toBe(1);
    expect(page.items[0].publicId).toBe(application.publicId);

    const detailCall = await admin.admissions.getApplication(application.publicId);
    expect(detailCall.response.status()).toBe(200);
    const initial = admissionApplicationDetailSchema.parse((detailCall.body as { data: unknown }).data);
    expect(initial.history).toHaveLength(1);
    expect(initial.history[0]).toMatchObject({ fromStatus: null, toStatus: 'PreEnrolled' });

    const enrolledCall = await admin.admissions.changeApplicationStatus(
      application.publicId, 1, 'Documentación verificada'
    );
    expect(enrolledCall.response.status()).toBe(200);
    const enrolled = admissionApplicationDetailSchema.parse((enrolledCall.body as { data: unknown }).data);
    expect(enrolled.application.status).toBe('Enrolled');
    expect(enrolled.history).toHaveLength(2);
    expect(enrolled.history[1].changedByUserId).not.toBeNull();

    const requirementCall = await admin.studentCatalogs.createDocumentRequirement(
      documentRequirementData(career.id)
    );
    expect(requirementCall.response.status()).toBe(201);
    const requirement = documentRequirementSchema.parse(requirementCall.body);
    requirementIds.push(requirement.id);

    const blockedConfirmation = await admin.admissions.changeApplicationStatus(application.publicId, 2);
    expect(blockedConfirmation.response.status()).toBe(409);
    expect(problemSchema.parse(blockedConfirmation.body).msg).toContain(requirement.code);

    expect((await anonymous.admissions.getApplicationDocuments(application.publicId)).response.status()).toBe(401);
    expect((await student.api.admissions.getApplicationDocuments(application.publicId)).response.status()).toBe(403);

    const applicableRequirements = documentRequirementSchema.array().parse(
      (await admin.studentCatalogs.listDocumentRequirements(career.id)).body
    ).filter((item) => item.isRequired);
    const documents = [];
    for (const applicableRequirement of applicableRequirements) {
      const documentCall = await admin.admissions.submitApplicationDocument(
        application.publicId,
        studentDocumentData(applicableRequirement.id)
      );
      expect(documentCall.response.status()).toBe(201);
      documents.push(admissionApplicationDocumentSchema.parse(
        (documentCall.body as { data: unknown }).data
      ));
    }
    const document = documents.find((item) => item.documentRequirementId === requirement.id)!;
    expect(document.status).toBe('Submitted');

    const stillBlocked = await admin.admissions.changeApplicationStatus(application.publicId, 2);
    expect(stillBlocked.response.status()).toBe(409);

    const reviewedDocuments = [];
    for (const candidate of documents) {
      const reviewedCall = await admin.admissions.reviewApplicationDocument(
        application.publicId,
        candidate.id,
        { status: 'Approved', observation: 'Documento verificado por E2E' }
      );
      expect(reviewedCall.response.status()).toBe(200);
      reviewedDocuments.push(admissionApplicationDocumentSchema.parse(
        (reviewedCall.body as { data: unknown }).data
      ));
    }
    const reviewed = reviewedDocuments.find((item) => item.id === document.id)!;
    expect(reviewed).toMatchObject({ status: 'Approved', reviewedByUserId: expect.any(Number) });

    const listedDocuments = admissionApplicationDocumentSchema.array().parse(
      ((await admin.admissions.getApplicationDocuments(application.publicId)).body as { data: unknown }).data
    );
    expect(listedDocuments.map((item) => item.id)).toContain(document.id);

    const confirmedCall = await admin.admissions.changeApplicationStatus(application.publicId, 2);
    expect(confirmedCall.response.status()).toBe(200);
    const confirmed = admissionApplicationDetailSchema.parse((confirmedCall.body as { data: unknown }).data);
    expect(confirmed.application.status).toBe('Confirmed');
    expect(confirmed.history).toHaveLength(3);

    expect((await anonymous.admissions.getAgreement(application.publicId)).response.status()).toBe(401);
    expect((await student.api.admissions.getAgreement(application.publicId)).response.status()).toBe(403);
    expect((await anonymous.admissions.processOutbox()).response.status()).toBe(401);
    expect((await student.api.admissions.processOutbox()).response.status()).toBe(403);

    const pendingAgreement = admissionAgreementSchema.parse(
      ((await admin.admissions.getAgreement(application.publicId)).body as { data: unknown }).data
    );
    expect(pendingAgreement).toMatchObject({ status: 'Pending', sha256: null, downloadPath: null });
    expect((await admin.admissions.downloadAgreement(application.publicId)).response.status()).toBe(409);

    const outboxCall = await admin.admissions.processOutbox();
    expect(outboxCall.response.status()).toBe(200);
    const outbox = admissionOutboxResultSchema.parse((outboxCall.body as { data: unknown }).data);
    expect(outbox.processed).toBeGreaterThanOrEqual(1);
    expect(outbox.failed).toBe(0);

    const readyAgreement = admissionAgreementSchema.parse(
      ((await admin.admissions.getAgreement(application.publicId)).body as { data: unknown }).data
    );
    expect(readyAgreement).toMatchObject({
      status: 'Ready',
      sha256: expect.stringMatching(/^[A-F0-9]{64}$/),
      downloadPath: expect.stringContaining(application.publicId)
    });
    const download = await admin.admissions.downloadAgreement(application.publicId);
    expect(download.response.status()).toBe(200);
    expect(download.response.headers()['content-type']).toContain('application/pdf');
    expect(download.rawBody.subarray(0, 8).toString('ascii')).toBe('%PDF-1.4');

    const invalidTransition = await admin.admissions.changeApplicationStatus(application.publicId, 1);
    expect(invalidTransition.response.status()).toBe(409);
    expect(problemSchema.parse(invalidTransition.body).msg).toContain('no puede transicionar');

    const deactivateCall = await admin.admissions.setFormActive(form.id, false);
    expect(deactivateCall.response.status()).toBe(200);
    const deactivated = admissionFormSchema.parse((deactivateCall.body as { data: unknown }).data);
    expect(deactivated.isActive).toBe(false);
    expect((await anonymous.admissions.getForm(form.slug)).response.status()).toBe(404);
  } finally {
    await student.dispose();
    if (formId) await db.cleanupAdmissionForm(formId, career.id);
    await db.cleanupP1Artifacts({ documentRequirementIds: requirementIds });
    await admin.academic.deleteCareer(career.id);
    await db.deleteUser(user.id);
  }
});
