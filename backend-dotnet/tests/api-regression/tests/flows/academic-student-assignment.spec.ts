import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  academicAssignmentSchema,
  commissionSchema,
  pagedStudentsSchema,
  studentSummarySchema
} from '../../src/contracts/schemas';
import { env } from '../../src/config/environment';
import { studentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('flujo crítico de catálogo, estudiante, plan y comisión @smoke @critical @regression', async ({ admin, db }) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Gestión académica');
  await allure.story('Alta y asignación integral de estudiante');
  await allure.severity('critical');
  await allure.owner('QA Automation');

  let careerId = 0;
  const user = await db.createUnlinkedStudentUser();
  try {
    const scenario = await test.step('Preparar catálogo académico', () => createAcademicScenario(admin));
    careerId = scenario.career.id;

    await test.step('Validar consultas del catálogo', async () => {
      const careers = await admin.academic.listCareers();
      expect(careers.response.status()).toBe(200);
      expect(careers.response.headers()['content-type']).toContain('application/json');
      expect(careers.durationMs).toBeLessThan(env.responseTimeMs);
      expect(careers.body).toEqual(expect.arrayContaining([expect.objectContaining({ id: scenario.career.id })]));

      const courses = await admin.academic.listCourses(scenario.career.id);
      expect(courses.response.status()).toBe(200);
      expect(courses.body).toHaveLength(2);

      const planCourses = await admin.academic.listStudyPlanCourses(scenario.studyPlan.id);
      expect(planCourses.response.status()).toBe(200);
      expect(planCourses.body).toHaveLength(2);

      const grouped = await admin.academic.groupedStudyPlan(scenario.career.id, scenario.studyPlan.id);
      expect(grouped.response.status()).toBe(200);
      expect(grouped.body).toMatchObject({ careerId: scenario.career.id, studyPlanId: scenario.studyPlan.id });

      const plans = await admin.academic.listStudyPlans(scenario.career.id);
      expect(plans.response.status()).toBe(200);
      expect(plans.body).toEqual(expect.arrayContaining([expect.objectContaining({ id: scenario.studyPlan.id, status: 'Active' })]));

      const commissions = await admin.academic.listCommissions(scenario.career.id, 2026);
      expect(commissions.response.status()).toBe(200);
      expect(commissionSchema.array().parse(commissions.body)).toEqual(expect.arrayContaining([scenario.commission]));
    });

    const student = await test.step('Crear estudiante con datos administrativos mínimos', async () => {
      const call = await admin.students.create(studentData(user.id, scenario.career.id));
      expect(call.response.status()).toBe(201);
      return studentSummarySchema.parse(call.body);
    });

    await test.step('Asignar plan de estudio', async () => {
      const call = await admin.students.assignStudyPlan(student.id, scenario.studyPlan.id, 'Asignación Playwright');
      expect(call.response.status()).toBe(204);
      const refreshed = await admin.students.get(student.id);
      expect(refreshed.response.status()).toBe(200);
      expect(studentSummarySchema.parse(refreshed.body).currentStudyPlanId).toBe(scenario.studyPlan.id);
    });

    const firstAssignment = await test.step('Asignar comisión académica', async () => {
      const call = await admin.students.assignAcademic(student.id, {
        careerId: scenario.career.id,
        studyPlanId: scenario.studyPlan.id,
        commissionId: scenario.commission.id,
        academicYear: 2026,
        yearNumber: 1,
        reason: 'Asignación automatizada'
      });
      expect(call.response.status()).toBe(201);
      return academicAssignmentSchema.parse(call.body);
    });
    expect(firstAssignment.isCurrent).toBe(true);

    await test.step('Reasignar y preservar historial', async () => {
      const secondCommissionCall = await admin.academic.createCommission(scenario.career.id, {
        ...scenario.data.commission,
        code: `${scenario.data.commission.code}B`.slice(0, 30),
        name: `${scenario.data.commission.name} B`
      });
      expect(secondCommissionCall.response.status()).toBe(201);
      const secondCommission = commissionSchema.parse(secondCommissionCall.body);
      const second = await admin.students.assignAcademic(student.id, {
        careerId: scenario.career.id,
        studyPlanId: scenario.studyPlan.id,
        commissionId: secondCommission.id,
        academicYear: 2026,
        yearNumber: 1,
        reason: 'Reasignación automatizada'
      });
      expect(second.response.status()).toBe(201);

      const history = await admin.students.assignments(student.id, 2026);
      expect(history.response.status()).toBe(200);
      const assignments = academicAssignmentSchema.array().parse(history.body);
      expect(assignments).toHaveLength(2);
      expect(assignments.find((item) => item.id === firstAssignment.id)).toMatchObject({ isCurrent: false });
      expect(assignments.find((item) => item.id === firstAssignment.id)?.endedAt).not.toBeNull();

      const filtered = await admin.students.list({ commissionId: secondCommission.id, page: 1, pageSize: 20 });
      expect(filtered.response.status()).toBe(200);
      expect(pagedStudentsSchema.parse(filtered.body).items).toEqual(
        expect.arrayContaining([expect.objectContaining({ id: student.id, commissionId: secondCommission.id })])
      );
    });

    await test.step('Consultar legajo, elegibilidad y progreso', async () => {
      const record = await admin.students.record(student.id);
      expect(record.response.status()).toBe(200);
      expect(record.body).toMatchObject({ student: expect.objectContaining({ id: student.id }), currentAcademicAssignment: expect.any(Object) });

      const eligible = await admin.students.eligibleCourses(student.id);
      expect(eligible.response.status()).toBe(200);
      expect(eligible.body).toHaveLength(2);

      const progress = await admin.students.academicProgress(student.id);
      expect(progress.response.status()).toBe(200);
      expect(progress.body).toMatchObject({ studentId: student.id, studyPlanId: scenario.studyPlan.id, totalCourses: 2 });
    });
  } finally {
    await cleanupScenario(admin, db, careerId, [user.id]);
  }
});
