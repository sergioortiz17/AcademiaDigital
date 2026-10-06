import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  billingPlanSchema,
  debtGenerationResultSchema,
  financialBenefitSchema,
  financialConceptSchema,
  financialRateSchema,
  scholarshipSchema,
  studentDebtSchema
} from '../../src/contracts/schemas';
import { runToken, scholarshipData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('M9 finanzas: tarifas, beneficios, generación idempotente y deuda aislada @m9 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M9 Cobros y conceptos');
  await allure.story('Generación masiva de deuda con snapshot');
  await allure.severity('critical');

  let careerId = 0;
  const userIds: number[] = [];
  const scholarshipIds: number[] = [];
  let scholarshipStudentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let regularStudentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  const uniqueSuffix = runToken();
  const conceptCode = `CUOTA_${uniqueSuffix}`;
  const generalBenefitCode = `GENERAL_${uniqueSuffix}`;
  const scholarshipBenefitCode = `BECA_${uniqueSuffix}`;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const scholarshipStudent = await createRegisteredStudent(anonymous, db, careerId);
    const regularStudent = await createRegisteredStudent(anonymous, db, careerId);
    userIds.push(scholarshipStudent.userId, regularStudent.userId);
    scholarshipStudentSession = await authenticatedClients(scholarshipStudent.registration.email, scholarshipStudent.registration.password);
    regularStudentSession = await authenticatedClients(regularStudent.registration.email, regularStudent.registration.password);

    expect((await anonymous.finance.concepts()).response.status()).toBe(401);
    expect((await scholarshipStudentSession.api.finance.createConcept({ code: 'NO', name: 'No autorizado' })).response.status()).toBe(403);

    const scholarshipCall = await admin.studentCatalogs.createScholarship(scholarshipData());
    expect(scholarshipCall.response.status()).toBe(201);
    const scholarship = scholarshipSchema.parse(scholarshipCall.body);
    scholarshipIds.push(scholarship.id);
    expect((await admin.students.addScholarship(scholarshipStudent.studentId, {
      scholarshipId: scholarship.id,
      academicYear: 2026,
      status: 'Granted',
      validFrom: '2026-01-01',
      validTo: '2026-12-31',
      notes: 'Beneficio financiero M9'
    })).response.status()).toBe(201);

    const conceptCall = await admin.finance.createConcept({
      code: ` ${conceptCode.toLowerCase()} `, name: 'Cuota mensual M9', description: 'Concepto de regresión'
    });
    expect(conceptCall.response.status()).toBe(201);
    let concept = financialConceptSchema.parse(conceptCall.body);
    expect(concept.code).toBe(conceptCode);
    expect((await admin.finance.createConcept({ code: conceptCode, name: 'Duplicado' })).response.status()).toBe(409);
    const updatedConceptCall = await admin.finance.updateConcept(concept.id, {
      code: concept.code, name: 'Cuota mensual actualizada', description: concept.description, isActive: true
    });
    expect(updatedConceptCall.response.status()).toBe(200);
    concept = financialConceptSchema.parse(updatedConceptCall.body);

    const rateBody = {
      conceptId: concept.id,
      careerId,
      academicYear: 2026,
      studentCondition: null,
      amount: 1000,
      surchargePercentage: 10,
      isActive: true
    };
    const rateCall = await admin.finance.createRate(rateBody);
    expect(rateCall.response.status()).toBe(201);
    const rate = financialRateSchema.parse(rateCall.body);
    expect((await admin.finance.createRate(rateBody)).response.status()).toBe(409);

    expect((await admin.finance.createBenefit({
      code: 'INVALID_SCHOLARSHIP', name: 'Inválido', kind: 'Scholarship', scholarshipId: null,
      careerId, studentCondition: null, percentage: 20, validFrom: null, validTo: null
    })).response.status()).toBe(400);
    const generalBenefit = financialBenefitSchema.parse((await admin.finance.createBenefit({
      code: generalBenefitCode, name: 'Descuento general 10%', kind: 'Discount', scholarshipId: null,
      careerId, studentCondition: null, percentage: 10, validFrom: '2026-01-01', validTo: '2026-12-31'
    })).body);
    const scholarshipBenefit = financialBenefitSchema.parse((await admin.finance.createBenefit({
      code: scholarshipBenefitCode, name: 'Beca 30%', kind: 'Scholarship', scholarshipId: scholarship.id,
      careerId, studentCondition: null, percentage: 30, validFrom: '2026-01-01', validTo: '2026-12-31'
    })).body);
    expect([generalBenefit.kind, scholarshipBenefit.kind]).toEqual(['Discount', 'Scholarship']);

    const planCall = await admin.finance.createPlan({
      name: 'Plan financiero M9', careerId, academicYear: 2026,
      items: [{ conceptId: concept.id, installmentNumber: 1, dueDate: '2026-01-10' }]
    });
    expect(planCall.response.status()).toBe(201);
    const plan = billingPlanSchema.parse(planCall.body);
    expect(plan.currency).toBe('ARS');
    expect((await admin.finance.generate(plan.id)).response.status()).toBe(400);
    expect((await scholarshipStudentSession.api.finance.generate(plan.id, 'finance-m9-forbidden')).response.status()).toBe(403);

    const idempotencyKey = `finance-m9-${Date.now()}`;
    const concurrent = await Promise.all([
      admin.finance.generate(plan.id, idempotencyKey),
      admin.finance.generate(plan.id, idempotencyKey)
    ]);
    expect(concurrent.map(call => call.response.status())).toEqual([200, 200]);
    const first = debtGenerationResultSchema.parse(concurrent[0].body);
    const retry = debtGenerationResultSchema.parse(concurrent[1].body);
    expect(retry.batchPublicId).toBe(first.batchPublicId);
    expect(first.generatedDebtCount).toBe(2);
    expect(first.debts.map(debt => debt.totalAmount).sort((a, b) => a - b)).toEqual([770, 990]);
    expect(first.generatedTotal).toBe(1760);
    expect((await admin.finance.generate(plan.id, `${idempotencyKey}-different`)).response.status()).toBe(409);

    const scholarshipOwn = studentDebtSchema.array().parse((await scholarshipStudentSession.api.finance.myDebts()).body);
    const regularOwn = studentDebtSchema.array().parse((await regularStudentSession.api.finance.myDebts()).body);
    expect(scholarshipOwn).toHaveLength(1);
    expect(regularOwn).toHaveLength(1);
    expect(scholarshipOwn[0]).toMatchObject({
      studentId: scholarshipStudent.studentId,
      baseAmount: 1000,
      surchargeAmount: 100,
      discountAmount: 330,
      totalAmount: 770,
      outstandingAmount: 770,
      appliedBenefitCode: scholarshipBenefitCode,
      status: 'Pending'
    });
    expect(regularOwn[0]).toMatchObject({
      studentId: regularStudent.studentId,
      discountAmount: 110,
      totalAmount: 990,
      appliedBenefitCode: generalBenefitCode
    });
    expect(studentDebtSchema.array().parse((await admin.finance.studentDebts(scholarshipStudent.studentId)).body))
      .toEqual(scholarshipOwn);
    expect((await regularStudentSession.api.finance.studentDebts(scholarshipStudent.studentId)).response.status()).toBe(403);

    const rateUpdate = await admin.finance.updateRate(rate.id, { ...rateBody, amount: 2000, surchargePercentage: 20 });
    expect(financialRateSchema.parse(rateUpdate.body)).toMatchObject({ amount: 2000, surchargePercentage: 20 });
    const immutableDebt = studentDebtSchema.array().parse((await admin.finance.studentDebts(scholarshipStudent.studentId)).body);
    expect(immutableDebt[0]).toMatchObject({ baseAmount: 1000, surchargeAmount: 100, totalAmount: 770 });

    expect(financialConceptSchema.array().parse((await admin.finance.concepts()).body).map(item => item.id)).toContain(concept.id);
    expect(financialRateSchema.array().parse((await admin.finance.rates({ careerId, academicYear: 2026 })).body)).toHaveLength(1);
    expect(financialBenefitSchema.array().parse((await admin.finance.benefits()).body).map(item => item.id))
      .toEqual(expect.arrayContaining([generalBenefit.id, scholarshipBenefit.id]));
    expect(billingPlanSchema.array().parse((await admin.finance.plans({ careerId, academicYear: 2026 })).body).map(item => item.id)).toContain(plan.id);
  } finally {
    await scholarshipStudentSession?.dispose();
    await regularStudentSession?.dispose();
    await cleanupScenario(admin, db, careerId, userIds);
    await db.cleanupP1Artifacts({ scholarshipIds });
  }
});
