import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { runToken } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('M12-M13 morosidad y egresos: filtros, alertas, exportación, adjunto y reverso @m12 @m13 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M12 Morosidad y M13 Egresos');
  await allure.story('Ledger financiero de Tesorería');
  await allure.severity('critical');

  let careerId = 0;
  const userIds: number[] = [];
  let treasurySession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const student = await createRegisteredStudent(anonymous, db, careerId);
    const treasury = await db.createTreasuryUser();
    userIds.push(student.userId, treasury.id);
    treasurySession = await authenticatedClients(treasury.email, treasury.password);
    studentSession = await authenticatedClients(student.registration.email, student.registration.password);

    expect((await anonymous.finance.delinquency()).response.status()).toBe(401);
    expect((await studentSession.api.finance.delinquency()).response.status()).toBe(403);

    const suffix = runToken();
    const concept = (await admin.finance.createConcept({ code: `MORA_${suffix}`, name: 'Cuota vencida M12' })).body as { id: number };
    expect(concept.id).toBeGreaterThan(0);
    expect((await admin.finance.createRate({
      conceptId: concept.id, careerId, academicYear: 2026, studentCondition: null,
      amount: 1500, surchargePercentage: 0, isActive: true
    })).response.status()).toBe(201);
    const plan = (await admin.finance.createPlan({
      name: `Plan mora ${suffix}`, careerId, academicYear: 2026,
      items: [{ conceptId: concept.id, installmentNumber: 1, dueDate: '2026-01-10' }]
    })).body as { id: number };
    const target = await db.getFinanceStudentTarget(student.studentId, 2026);
    expect((await admin.finance.generate(plan.id, target, `m12-${suffix}`)).response.status()).toBe(200);

    const sync = await treasurySession.api.finance.syncDirectory([{
      studentId: student.studentId, studentCareerId: target.studentCareerId,
      studentName: target.studentName, dni: target.dni, legajoNumber: target.legajoNumber,
      careerId, careerName: target.careerName, commissionId: scenario.commission.id,
      commissionName: scenario.commission.name, academicYear: 2026, isActive: true
    }]);
    expect(sync.response.status()).toBe(200);
    expect((sync.body as { synchronized: number }).synchronized).toBe(1);

    expect((await treasurySession.api.finance.createDelinquencyRule({
      name: `Mora 30 días ${suffix}`, minimumOutstandingAmount: 1000,
      minimumDaysOverdue: 30, isActive: true
    })).response.status()).toBe(201);
    const dashboardCall = await treasurySession.api.finance.delinquency({
      careerId, academicYear: 2026, asOf: '2026-11-14'
    });
    expect(dashboardCall.response.status()).toBe(200);
    const dashboard = dashboardCall.body as { studentCount: number; overdueTotal: number; students: Array<{ studentId: number; overdueInstallments: number; maximumDaysOverdue: number; triggeredAlerts: string[] }> };
    expect(dashboard.studentCount).toBe(1);
    expect(dashboard.overdueTotal).toBe(1500);
    expect(dashboard.students[0]).toMatchObject({ studentId: student.studentId, overdueInstallments: 1 });
    expect(dashboard.students[0].maximumDaysOverdue).toBeGreaterThan(30);
    expect(dashboard.students[0].triggeredAlerts).toContain(`Mora 30 días ${suffix}`);
    expect((await treasurySession.api.finance.delinquencyStudent(student.studentId)).response.status()).toBe(200);

    const excel = await treasurySession.api.finance.exportDelinquency('excel');
    expect(excel.response.status()).toBe(200);
    expect(excel.response.headers()['content-type']).toContain('application/vnd.ms-excel');
    const pdf = await treasurySession.api.finance.exportDelinquency('pdf');
    expect(pdf.response.status()).toBe(200);
    expect(pdf.rawBody.subarray(0, 5).toString()).toBe('%PDF-');

    expect((await studentSession.api.finance.createExpenseCategory({ code: `NO_${suffix}`, name: 'No permitido' })).response.status()).toBe(403);
    const categoryCall = await treasurySession.api.finance.createExpenseCategory({ code: `SERV_${suffix}`, name: 'Servicios' });
    expect(categoryCall.response.status()).toBe(201);
    const category = categoryCall.body as { id: number };
    const attachment = Buffer.from('%PDF-1.4\nM13 comprobante\n%%EOF');
    const expenseCall = await treasurySession.api.finance.registerExpense({
      categoryId: category.id, supplierName: 'Proveedor E2E', expenseDate: '2026-11-14', amount: 2750.50,
      purchaseNote: `OC-${suffix}`, observation: 'Egreso inmutable de prueba',
      attachment: { name: 'factura.pdf', mimeType: 'application/pdf', buffer: attachment }
    });
    expect(expenseCall.response.status()).toBe(201);
    const expense = expenseCall.body as { publicId: string; amount: number; hasAttachment: boolean; attachmentSha256: string };
    expect(expense).toMatchObject({ amount: 2750.5, hasAttachment: true });
    expect(expense.attachmentSha256).toMatch(/^[a-f0-9]{64}$/);
    const download = await treasurySession.api.finance.downloadExpenseAttachment(expense.publicId);
    expect(download.response.status()).toBe(200);
    expect(download.rawBody.equals(attachment)).toBe(true);

    const reversal = await treasurySession.api.finance.reverseExpense(expense.publicId, 'Corrección compensatoria E2E');
    expect(reversal.response.status()).toBe(200);
    expect((reversal.body as { kind: string }).kind).toBe('Reversal');
    expect((await treasurySession.api.finance.reverseExpense(expense.publicId, 'Duplicado')).response.status()).toBe(409);
    const ledger = (await treasurySession.api.finance.expenses({ categoryId: category.id })).body as {
      expenseTotal: number; reversalTotal: number; netTotal: number; entries: unknown[]
    };
    expect(ledger).toMatchObject({ expenseTotal: 2750.5, reversalTotal: 2750.5, netTotal: 0 });
    expect(ledger.entries).toHaveLength(2);
  } finally {
    await treasurySession?.dispose();
    await studentSession?.dispose();
    await cleanupScenario(admin, db, careerId, userIds);
  }
});
