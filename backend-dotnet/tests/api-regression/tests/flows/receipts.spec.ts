import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  billingPlanSchema,
  debtGenerationResultSchema,
  financialConceptSchema,
  paymentMethodSchema,
  paymentSchema,
  receiptSchema
} from '../../src/contracts/schemas';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('M11 recibos: correlativo concurrente, emisión atómica, PDF e historial aislado @m11 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M11 Recibos digitales');
  await allure.story('Comprobante interno atómico y verificable');
  await allure.severity('critical');

  let careerId = 0;
  const userIds: number[] = [];
  let ownerSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let otherSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const owner = await createRegisteredStudent(anonymous, db, careerId);
    userIds.push(owner.userId);
    ownerSession = await authenticatedClients(owner.registration.email, owner.registration.password);

    expect((await anonymous.receipts.mine()).response.status()).toBe(401);
    expect((await ownerSession.api.receipts.byStudent(owner.studentId)).response.status()).toBe(403);

    const methods = paymentMethodSchema.array().parse((await admin.payments.methods()).body);
    const methodId = (code: string) => {
      const method = methods.find(item => item.code === code);
      if (!method) throw new Error(`Payment method ${code} not found.`);
      return method.id;
    };

    const conceptCall = await admin.finance.createConcept({
      code: `REC_M11_${Date.now().toString(36)}`.toUpperCase(),
      name: 'Cuota para recibos M11',
      description: 'Concepto temporal de comprobantes'
    });
    expect(conceptCall.response.status()).toBe(201);
    const concept = financialConceptSchema.parse(conceptCall.body);
    expect((await admin.finance.createRate({
      conceptId: concept.id, careerId, academicYear: 2026, studentCondition: null,
      amount: 100, surchargePercentage: 0, isActive: true
    })).response.status()).toBe(201);
    const planCall = await admin.finance.createPlan({
      name: 'Plan recibos M11', careerId, academicYear: 2026,
      items: [1, 2, 3].map(installmentNumber => ({
        conceptId: concept.id,
        installmentNumber,
        dueDate: `2026-${String(installmentNumber + 8).padStart(2, '0')}-10`
      }))
    });
    expect(planCall.response.status()).toBe(201);
    const plan = billingPlanSchema.parse(planCall.body);
    const generation = debtGenerationResultSchema.parse((await admin.finance.generate(
      plan.id, `receipts-m11-${Date.now()}`
    )).body);
    const debts = [...generation.debts].sort((a, b) => a.installmentNumber - b.installmentNumber);
    expect(debts).toHaveLength(3);

    const createDraft = async (debtPublicId: string, paymentMethodId: number, externalReference: string | null = null) => {
      const call = await admin.payments.create({
        studentDni: owner.registration.DNI,
        paymentMethodId,
        amount: 100,
        externalReference,
        notes: 'Pago con comprobante M11',
        allocations: [{ debtPublicId, amount: 100 }]
      });
      expect(call.response.status()).toBe(201);
      return paymentSchema.parse(call.body);
    };

    const cashDrafts = await Promise.all([
      createDraft(debts[0].publicId, methodId('CASH')),
      createDraft(debts[1].publicId, methodId('DEBIT_CARD'))
    ]);
    const confirmations = await Promise.all(cashDrafts.map((draft, index) =>
      admin.payments.confirm(draft.publicId, `receipt-confirm-${index}-${Date.now()}`)));
    expect(confirmations.map(call => call.response.status())).toEqual([200, 200]);
    const confirmedPayments = confirmations.map(call => paymentSchema.parse(call.body));
    const concurrentReceipts = confirmedPayments.map(payment => receiptSchema.parse(payment.receipt));
    expect(concurrentReceipts.every(receipt => receipt.status === 'Ready')).toBe(true);
    expect(new Set(concurrentReceipts.map(receipt => receipt.receiptNumber)).size).toBe(2);
    const concurrentNumbers = concurrentReceipts.map(receipt => Number(receipt.receiptNumber.slice(4))).sort((a, b) => a - b);
    expect(concurrentNumbers[1] - concurrentNumbers[0]).toBe(1);
    expect(concurrentReceipts[0]).toMatchObject({
      studentId: owner.studentId,
      amount: 100,
      currency: 'ARS',
      paymentStatus: 'Confirmed',
      fiscalCae: null,
      fiscalQrData: null
    });
    expect(concurrentReceipts[0].items).toEqual([
      expect.objectContaining({ conceptCode: concept.code, conceptName: concept.name, amount: 100 })
    ]);

    const transferDraft = await createDraft(
      debts[2].publicId, methodId('BANK_TRANSFER'), `M11-TR-${Date.now()}`);
    const pendingTransfer = paymentSchema.parse((await admin.payments.confirm(
      transferDraft.publicId, `receipt-transfer-${Date.now()}`
    )).body);
    expect(pendingTransfer).toMatchObject({ status: 'PendingReconciliation', receipt: null });
    expect(receiptSchema.array().parse((await ownerSession.api.receipts.mine()).body)).toHaveLength(2);

    const approvedTransfer = paymentSchema.parse((await admin.payments.reconcile(transferDraft.publicId, {
      decision: 'Approve', note: 'Transferencia acreditada para M11'
    })).body);
    const transferReceipt = receiptSchema.parse(approvedTransfer.receipt);
    expect(transferReceipt).toMatchObject({
      status: 'Ready', paymentMethodCode: 'BANK_TRANSFER', paymentStatus: 'Confirmed'
    });

    const ownerReceipts = receiptSchema.array().parse((await ownerSession.api.receipts.mine()).body);
    const adminReceipts = receiptSchema.array().parse((await admin.receipts.byStudent(owner.studentId)).body);
    expect(ownerReceipts).toEqual(adminReceipts);
    expect(ownerReceipts).toHaveLength(3);
    const allNumbers = ownerReceipts.map(receipt => Number(receipt.receiptNumber.slice(4))).sort((a, b) => a - b);
    expect(allNumbers).toEqual([allNumbers[0], allNumbers[0] + 1, allNumbers[0] + 2]);

    const detail = receiptSchema.parse((await ownerSession.api.receipts.get(concurrentReceipts[0].publicId)).body);
    expect(detail.sha256).toMatch(/^[A-F0-9]{64}$/);
    const ownerPdf = await ownerSession.api.receipts.download(detail.publicId);
    expect(ownerPdf.response.status()).toBe(200);
    expect(ownerPdf.response.headers()['content-type']).toContain('application/pdf');
    expect(ownerPdf.rawBody.subarray(0, 8).toString('ascii')).toBe('%PDF-1.4');
    const adminPdf = await admin.receipts.download(detail.publicId);
    expect(adminPdf.response.status()).toBe(200);

    const generatedRetry = receiptSchema.parse((await admin.receipts.generate(detail.publicId)).body);
    expect(generatedRetry).toMatchObject({
      publicId: detail.publicId,
      receiptNumber: detail.receiptNumber,
      sha256: detail.sha256,
      status: 'Ready'
    });

    const reversedPayment = paymentSchema.parse((await admin.payments.reverse(
      concurrentReceipts[0].paymentPublicId,
      { reason: 'Reversión posterior al recibo emitido' }
    )).body);
    expect(reversedPayment.status).toBe('Reversed');
    const immutableReceipt = receiptSchema.parse((await admin.receipts.get(detail.publicId)).body);
    expect(immutableReceipt).toMatchObject({
      paymentStatus: 'Reversed',
      receiptNumber: detail.receiptNumber,
      amount: detail.amount,
      sha256: detail.sha256
    });
    expect(await admin.receipts.download(detail.publicId).then(call => call.response.status())).toBe(200);

    const other = await createRegisteredStudent(anonymous, db, careerId);
    userIds.push(other.userId);
    otherSession = await authenticatedClients(other.registration.email, other.registration.password);
    expect(receiptSchema.array().parse((await otherSession.api.receipts.mine()).body)).toEqual([]);
    expect((await otherSession.api.receipts.get(detail.publicId)).response.status()).toBe(403);
    expect((await otherSession.api.receipts.download(detail.publicId)).response.status()).toBe(403);
    expect((await ownerSession.api.receipts.generate(detail.publicId)).response.status()).toBe(403);
  } finally {
    await ownerSession?.dispose();
    await otherSession?.dispose();
    await cleanupScenario(admin, db, careerId, userIds);
  }
});
