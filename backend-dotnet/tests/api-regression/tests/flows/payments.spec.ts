import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  billingPlanSchema,
  debtGenerationResultSchema,
  financialConceptSchema,
  paymentMethodSchema,
  paymentSchema,
  studentDebtSchema
} from '../../src/contracts/schemas';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('M10 pagos: parciales, multiconcepto, conciliación, idempotencia y reversa @m10 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M10 Pagos');
  await allure.story('Imputación y conciliación auditable');
  await allure.severity('critical');

  let careerId = 0;
  const userIds: number[] = [];
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const student = await createRegisteredStudent(anonymous, db, careerId);
    userIds.push(student.userId);
    studentSession = await authenticatedClients(student.registration.email, student.registration.password);

    expect((await anonymous.payments.methods()).response.status()).toBe(401);
    expect((await anonymous.payments.mine()).response.status()).toBe(401);
    expect((await studentSession.api.payments.create({
      studentDni: '99999999', paymentMethodId: 1, amount: 1,
      externalReference: null, notes: null,
      allocations: [{ debtPublicId: '00000000-0000-0000-0000-000000000001', amount: 1 }]
    })).response.status()).toBe(403);

    const methods = paymentMethodSchema.array().parse((await studentSession.api.payments.methods()).body);
    expect(methods.map(method => method.code).sort()).toEqual(['BANK_TRANSFER', 'CASH', 'CREDIT_CARD', 'DEBIT_CARD']);
    expect(methods.find(method => method.code === 'BANK_TRANSFER')?.requiresReconciliation).toBe(true);
    const methodId = (code: string) => {
      const method = methods.find(item => item.code === code);
      if (!method) throw new Error(`Payment method ${code} not found.`);
      return method.id;
    };

    const conceptCall = await admin.finance.createConcept({
      code: `PAY_M10_${Date.now().toString(36)}`.toUpperCase(),
      name: 'Cuota para pagos M10',
      description: 'Concepto temporal de regresión'
    });
    expect(conceptCall.response.status()).toBe(201);
    const concept = financialConceptSchema.parse(conceptCall.body);
    expect((await admin.finance.createRate({
      conceptId: concept.id, careerId, academicYear: 2026, studentCondition: null,
      amount: 100, surchargePercentage: 0, isActive: true
    })).response.status()).toBe(201);
    const planCall = await admin.finance.createPlan({
      name: 'Plan pagos M10', careerId, academicYear: 2026,
      items: [
        { conceptId: concept.id, installmentNumber: 1, dueDate: '2026-09-10' },
        { conceptId: concept.id, installmentNumber: 2, dueDate: '2026-10-10' }
      ]
    });
    expect(planCall.response.status()).toBe(201);
    const plan = billingPlanSchema.parse(planCall.body);
    const generation = debtGenerationResultSchema.parse((await admin.finance.generate(plan.id, `payments-m10-${Date.now()}`)).body);
    expect(generation.debts).toHaveLength(2);
    const debts = [...generation.debts].sort((a, b) => a.installmentNumber - b.installmentNumber);

    const cashDraftCall = await admin.payments.create({
      studentDni: student.registration.DNI,
      paymentMethodId: methodId('CASH'),
      amount: 60,
      externalReference: null,
      notes: 'Pago parcial multiconcepto',
      allocations: [
        { debtPublicId: debts[0].publicId, amount: 40 },
        { debtPublicId: debts[1].publicId, amount: 20 }
      ]
    });
    expect(cashDraftCall.response.status()).toBe(201);
    const cashDraft = paymentSchema.parse(cashDraftCall.body);
    expect(cashDraft).toMatchObject({ amount: 60, status: 'Draft', currency: 'ARS' });
    expect((await admin.payments.confirm(cashDraft.publicId)).response.status()).toBe(400);

    const cashKey = `payments-cash-${Date.now()}`;
    const cashConfirmations = await Promise.all([
      admin.payments.confirm(cashDraft.publicId, cashKey),
      admin.payments.confirm(cashDraft.publicId, cashKey)
    ]);
    expect(cashConfirmations.map(call => call.response.status())).toEqual([200, 200]);
    const cash = paymentSchema.parse(cashConfirmations[0].body);
    expect(paymentSchema.parse(cashConfirmations[1].body).publicId).toBe(cash.publicId);
    expect(cash.status).toBe('Confirmed');
    expect(cash.allocations.map(item => item.debtPaid)).toEqual([40, 20]);

    expect((await admin.payments.create({
      studentDni: student.registration.DNI,
      paymentMethodId: methodId('CASH'),
      amount: 61,
      externalReference: null,
      notes: null,
      allocations: [{ debtPublicId: debts[0].publicId, amount: 61 }]
    })).response.status()).toBe(409);

    const reversed = paymentSchema.parse((await admin.payments.reverse(cash.publicId, {
      reason: 'Anulación administrativa de prueba'
    })).body);
    expect(reversed.status).toBe('Reversed');
    expect(reversed.reversals).toHaveLength(1);
    const reversedRetry = paymentSchema.parse((await admin.payments.reverse(cash.publicId, {
      reason: 'Reintento idempotente de anulación'
    })).body);
    expect(reversedRetry.reversals).toHaveLength(1);

    const transferDraft = paymentSchema.parse((await admin.payments.create({
      studentDni: student.registration.DNI,
      paymentMethodId: methodId('BANK_TRANSFER'),
      amount: 100,
      externalReference: `TR-${Date.now()}`,
      notes: 'Transferencia a conciliar',
      allocations: [{ debtPublicId: debts[0].publicId, amount: 100 }]
    })).body);
    const transferPending = paymentSchema.parse((await admin.payments.confirm(
      transferDraft.publicId, `payments-transfer-${Date.now()}`
    )).body);
    expect(transferPending.status).toBe('PendingReconciliation');
    expect(transferPending.allocations[0].debtPaid).toBe(0);
    const transfer = paymentSchema.parse((await admin.payments.reconcile(transferDraft.publicId, {
      decision: 'Approve', note: 'Acreditación verificada en banco'
    })).body);
    expect(transfer).toMatchObject({ status: 'Confirmed', reconciliations: [{ decision: 'Approve' }] });
    expect(transfer.allocations[0]).toMatchObject({ debtPaid: 100, debtOutstanding: 0, debtStatus: 'Paid' });

    const rejectedDraft = paymentSchema.parse((await admin.payments.create({
      studentDni: student.registration.DNI,
      paymentMethodId: methodId('BANK_TRANSFER'),
      amount: 10,
      externalReference: `TR-REJECT-${Date.now()}`,
      notes: null,
      allocations: [{ debtPublicId: debts[1].publicId, amount: 10 }]
    })).body);
    await admin.payments.confirm(rejectedDraft.publicId, `payments-reject-${Date.now()}`);
    expect((await admin.payments.reconcile(rejectedDraft.publicId, { decision: 'Reject', note: null })).response.status()).toBe(400);
    const rejected = paymentSchema.parse((await admin.payments.reconcile(rejectedDraft.publicId, {
      decision: 'Reject', note: 'Transferencia no acreditada'
    })).body);
    expect(rejected).toMatchObject({ status: 'Rejected', reconciliations: [{ decision: 'Reject' }] });
    expect(rejected.allocations[0].debtPaid).toBe(0);

    const debitDraft = paymentSchema.parse((await admin.payments.create({
      studentDni: student.registration.DNI,
      paymentMethodId: methodId('DEBIT_CARD'), amount: 30, externalReference: null, notes: null,
      allocations: [{ debtPublicId: debts[1].publicId, amount: 30 }]
    })).body);
    expect((await admin.payments.confirm(debitDraft.publicId, cashKey)).response.status()).toBe(409);
    const debit = paymentSchema.parse((await admin.payments.confirm(debitDraft.publicId, `payments-debit-${Date.now()}`)).body);
    expect(debit).toMatchObject({ status: 'Confirmed', method: { code: 'DEBIT_CARD' } });

    const creditDraft = paymentSchema.parse((await admin.payments.create({
      studentDni: student.registration.DNI,
      paymentMethodId: methodId('CREDIT_CARD'), amount: 70, externalReference: null, notes: null,
      allocations: [{ debtPublicId: debts[1].publicId, amount: 70 }]
    })).body);
    const credit = paymentSchema.parse((await admin.payments.confirm(creditDraft.publicId, `payments-credit-${Date.now()}`)).body);
    expect(credit).toMatchObject({ status: 'Confirmed', method: { code: 'CREDIT_CARD' } });
    expect(credit.allocations[0]).toMatchObject({ debtPaid: 100, debtOutstanding: 0, debtStatus: 'Paid' });

    const finalDebts = studentDebtSchema.array().parse((await admin.finance.studentDebts(student.studentId)).body);
    expect(finalDebts.map(debt => debt.status)).toEqual(['Paid', 'Paid']);
    const adminHistory = paymentSchema.array().parse((await admin.payments.byStudent(student.studentId)).body);
    const ownHistory = paymentSchema.array().parse((await studentSession.api.payments.mine()).body);
    expect(ownHistory).toEqual(adminHistory);
    expect(ownHistory).toHaveLength(5);
    expect((await studentSession.api.payments.byStudent(student.studentId)).response.status()).toBe(403);
  } finally {
    await studentSession?.dispose();
    await cleanupScenario(admin, db, careerId, userIds);
  }
});
