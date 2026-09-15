import { randomUUID } from 'node:crypto';
import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  enrollmentPeriodSchema,
  examRegistrationSchema,
  examTableDetailSchema,
  examTableSchema,
  gradebookDetailSchema,
  gradebookSchema,
  publishedGradebookSchema,
  studentExamTableSchema,
  teacherSchema,
  teachingPositionSchema
} from '../../src/contracts/schemas';
import { registeredStudentData, runToken } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('M7 calificaciones y mesas: workflow, historial, publicaciÃ³n y acta final @m7 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M7 Calificaciones y notas');
  await allure.story('Planilla de cursada y mesa de examen completas');
  await allure.severity('critical');

  const professorUser = await db.createUnlinkedTeacherUser();
  const vocalUser = await db.createUnlinkedTeacherUser();
  let careerId = 0;
  let studentUserId = 0;
  let professorId = 0;
  let vocalId = 0;
  let periodId = 0;
  let professorSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let vocalSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const registrationData = registeredStudentData(careerId);
    const registerUser = await anonymous.auth.register(registrationData);
    expect(registerUser.response.status()).toBe(201);
    studentUserId = (registerUser.body as { userID: number }).userID;
    const studentId = await db.findStudentIdByUserId(studentUserId);
    if (!studentId) throw new Error('Student profile was not created.');
    expect((await admin.students.assignStudyPlan(studentId, scenario.studyPlan.id, 'Grades E2E')).response.status()).toBe(204);
    expect((await admin.students.assignAcademic(studentId, {
      careerId,
      studyPlanId: scenario.studyPlan.id,
      commissionId: scenario.commission.id,
      academicYear: 2026,
      yearNumber: 1,
      reason: 'Grades E2E'
    })).response.status()).toBe(201);
    const periodCall = await admin.enrollments.createPeriod({
      careerId,
      studyPlanId: scenario.studyPlan.id,
      academicYear: 2026,
      semester: 1,
      quotasMorning: 10,
      quotasAfternoon: 10,
      quotasEvening: 10
    });
    expect(periodCall.response.status()).toBe(201);
    periodId = enrollmentPeriodSchema.parse((periodCall.body as { data: unknown }).data).id;
    studentSession = await authenticatedClients(registrationData.email, registrationData.password);
    expect((await studentSession.api.enrollments.enroll({
      enrollmentPeriodId: periodId,
      shift: 'Tarde',
      studyPlanCourseIds: [scenario.introStudyPlanCourse.id]
    })).response.status()).toBe(201);

    const professorCall = await admin.teachers.create({
      userId: professorUser.id,
      employeeNumber: `grade-${runToken()}`,
      department: 'AcadÃ©mica',
      specializationArea: 'Calificaciones',
      hireDate: '2026-03-01T00:00:00Z'
    });
    expect(professorCall.response.status()).toBe(201);
    professorId = teacherSchema.parse(professorCall.body).id;
    const vocalCall = await admin.teachers.create({
      userId: vocalUser.id,
      employeeNumber: `vocal-${runToken()}`,
      department: 'AcadÃ©mica',
      specializationArea: 'Tribunal',
      hireDate: '2026-03-01T00:00:00Z'
    });
    expect(vocalCall.response.status()).toBe(201);
    vocalId = teacherSchema.parse(vocalCall.body).id;
    const positionCall = await admin.teachingPositions.create({
      courseId: scenario.introCourse.id,
      commissionId: scenario.commission.id,
      academicYear: 2026,
      semester: 1,
      positionType: 'Titular',
      maxStudents: 40
    });
    expect(positionCall.response.status()).toBe(201);
    const position = teachingPositionSchema.parse(positionCall.body);
    expect((await admin.teachers.assign(professorId, {
      teachingPositionId: position.id,
      startedOn: '2026-03-01',
      reason: 'Calificaciones M7 E2E'
    })).response.status()).toBe(201);

    professorSession = await authenticatedClients(professorUser.email, professorUser.password);
    vocalSession = await authenticatedClients(vocalUser.email, vocalUser.password);
    expect((await anonymous.grades.gradebooks()).response.status()).toBe(401);
    expect((await studentSession.api.grades.gradebooks()).response.status()).toBe(403);
    expect((await studentSession.api.grades.myGrades()).body).toEqual([]);
    expect((await vocalSession.api.grades.gradebooks()).body).toEqual([]);

    const gradebookBody = {
      teachingPositionId: position.id,
      evaluations: [
        { name: 'Parcial', weightPercentage: 40, maximumScore: 10 },
        { name: 'Proyecto', weightPercentage: 60, maximumScore: 10 }
      ]
    };
    expect((await vocalSession.api.grades.createGradebook(randomUUID(), gradebookBody)).response.status()).toBe(403);
    expect((await professorSession.api.grades.createGradebook(randomUUID(), {
      ...gradebookBody,
      evaluations: [
        { name: 'Parcial', weightPercentage: 40, maximumScore: 10 },
        { name: 'Proyecto', weightPercentage: 50, maximumScore: 10 }
      ]
    })).response.status()).toBe(400);
    const gradebookKey = randomUUID();
    const createGradebook = await professorSession.api.grades.createGradebook(gradebookKey, gradebookBody);
    expect(createGradebook.response.status()).toBe(201);
    const gradebook = gradebookSchema.parse(createGradebook.body);
    const retryGradebook = gradebookSchema.parse(
      (await professorSession.api.grades.createGradebook(gradebookKey, gradebookBody)).body);
    expect(retryGradebook.id).toBe(gradebook.id);
    expect((await vocalSession.api.grades.gradebook(gradebook.id)).response.status()).toBe(403);

    let detail = gradebookDetailSchema.parse((await professorSession.api.grades.gradebook(gradebook.id)).body);
    expect(detail.students).toHaveLength(1);
    expect(detail.evaluations).toHaveLength(2);
    const enrollmentId = detail.students[0].enrollmentId;
    const partialId = detail.evaluations[0].id;
    const projectId = detail.evaluations[1].id;
    expect((await professorSession.api.grades.saveGrades(gradebook.id, {
      grades: [{ evaluationId: partialId, enrollmentId, score: 5, notes: 'Primera versiÃ³n' }]
    })).response.status()).toBe(200);
    expect((await professorSession.api.grades.submitGradebook(gradebook.id)).response.status()).toBe(409);
    detail = gradebookDetailSchema.parse((await professorSession.api.grades.saveGrades(gradebook.id, {
      grades: [
        { evaluationId: partialId, enrollmentId, score: 6, notes: 'CorrecciÃ³n docente' },
        { evaluationId: projectId, enrollmentId, score: 6 }
      ]
    })).body);
    expect(detail.students[0]).toMatchObject({ average: 6, resultStatus: 'Regularized' });
    expect(detail.students[0].grades.find(item => item.evaluationId === partialId)?.version).toBe(2);

    expect((await professorSession.api.grades.submitGradebook(gradebook.id)).response.status()).toBe(200);
    expect((await professorSession.api.grades.saveGrades(gradebook.id, {
      grades: [{ evaluationId: partialId, enrollmentId, score: 7 }]
    })).response.status()).toBe(409);
    expect((await professorSession.api.grades.approveGradebook(gradebook.id)).response.status()).toBe(403);
    expect((await admin.grades.approveGradebook(gradebook.id)).response.status()).toBe(200);
    expect((await studentSession.api.grades.myGrades()).body).toEqual([]);
    expect((await admin.grades.publishGradebook(gradebook.id)).response.status()).toBe(200);
    let published = publishedGradebookSchema.array().parse((await studentSession.api.grades.myGrades()).body);
    expect(published[0]).toMatchObject({ average: 6, resultStatus: 'Regularized', status: 'Published' });
    expect((await admin.grades.closeGradebook(gradebook.id)).response.status()).toBe(200);
    expect(await db.getEnrollmentState(enrollmentId)).toEqual({ status: 1, finalGrade: 6 });

    expect((await professorSession.api.grades.reopenGradebook(gradebook.id, 'Intento docente')).response.status()).toBe(403);
    const reopenedGradebook = gradebookSchema.parse(
      (await admin.grades.reopenGradebook(gradebook.id, 'CorrecciÃ³n autorizada por SecretarÃ­a')).body);
    expect(reopenedGradebook).toMatchObject({ status: 'Draft', reopeningCount: 1 });
    expect((await professorSession.api.grades.saveGrades(gradebook.id, {
      grades: [{ evaluationId: partialId, enrollmentId, score: 6, notes: 'VersiÃ³n reabierta' }]
    })).response.status()).toBe(200);
    expect((await professorSession.api.grades.submitGradebook(gradebook.id)).response.status()).toBe(200);
    expect((await admin.grades.approveGradebook(gradebook.id)).response.status()).toBe(200);
    expect((await admin.grades.publishGradebook(gradebook.id)).response.status()).toBe(200);
    expect((await admin.grades.closeGradebook(gradebook.id)).response.status()).toBe(200);

    const examBody = {
      courseId: scenario.introCourse.id,
      academicYear: 2026,
      callNumber: 1,
      examDateUtc: '2026-12-10T18:00:00Z',
      registrationDeadlineUtc: '2026-12-01T23:59:00Z',
      location: 'Aula Magna',
      tribunal: [
        { teacherId: professorId, role: 'President' },
        { teacherId: vocalId, role: 'Vocal' }
      ]
    };
    expect((await anonymous.grades.createExamTable(randomUUID(), examBody)).response.status()).toBe(401);
    expect((await professorSession.api.grades.createExamTable(randomUUID(), examBody)).response.status()).toBe(403);
    const examKey = randomUUID();
    const createExam = await admin.grades.createExamTable(examKey, examBody);
    expect(createExam.response.status()).toBe(201);
    const examTable = examTableSchema.parse(createExam.body);
    expect(examTableSchema.parse((await admin.grades.createExamTable(examKey, examBody)).body).id).toBe(examTable.id);
    expect(examTableSchema.array().parse((await professorSession.api.grades.examTables()).body)).toHaveLength(1);
    expect(examTableSchema.array().parse((await vocalSession.api.grades.examTables()).body)).toHaveLength(1);
    expect(examTableDetailSchema.parse((await professorSession.api.grades.examTable(examTable.id)).body).tribunal).toHaveLength(2);
    expect((await studentSession.api.grades.examTable(examTable.id)).response.status()).toBe(403);
    expect(studentExamTableSchema.array().parse((await studentSession.api.grades.myExamTables()).body)[0])
      .toMatchObject({ canRegister: true, registrationId: null, result: null });

    const examRegistrationCall = await studentSession.api.grades.registerForExam(examTable.id, enrollmentId);
    expect(examRegistrationCall.response.status()).toBe(201);
    const examRegistration = examRegistrationSchema.parse(examRegistrationCall.body);
    expect(examRegistration.attemptNumber).toBe(1);
    expect(examRegistrationSchema.parse(
      (await studentSession.api.grades.registerForExam(examTable.id, enrollmentId)).body).attemptNumber).toBe(1);
    expect((await professorSession.api.grades.registerForExam(examTable.id, enrollmentId)).response.status()).toBe(403);
    expect((await admin.grades.startExamGrading(examTable.id)).response.status()).toBe(200);
    expect((await studentSession.api.grades.saveExamResults(examTable.id, {
      results: [{ registrationId: examRegistration.id, outcome: 'Passed', grade: 8 }]
    })).response.status()).toBe(403);
    const savedResult = await professorSession.api.grades.saveExamResults(examTable.id, {
      results: [{ registrationId: examRegistration.id, outcome: 'Passed', grade: 8, notes: 'Acta inicial' }]
    });
    expect(savedResult.response.status()).toBe(200);
    expect(examTableDetailSchema.parse(savedResult.body).registrations[0].result)
      .toMatchObject({ version: 1, outcome: 'Passed', grade: 8 });
    expect(studentExamTableSchema.array().parse((await studentSession.api.grades.myExamTables()).body)[0].result).toBeNull();
    expect((await admin.grades.publishExamTable(examTable.id)).response.status()).toBe(200);
    expect(await db.getEnrollmentState(enrollmentId)).toEqual({ status: 2, finalGrade: 8 });
    expect(studentExamTableSchema.array().parse((await studentSession.api.grades.myExamTables()).body)[0].result)
      .toMatchObject({ outcome: 'Passed', grade: 8 });

    expect((await professorSession.api.grades.reopenExamTable(examTable.id, 'Intento docente')).response.status()).toBe(403);
    expect(examTableSchema.parse(
      (await admin.grades.reopenExamTable(examTable.id, 'RectificaciÃ³n de acta autorizada')).body))
      .toMatchObject({ status: 'Grading', reopeningCount: 1 });
    const correctedResult = examTableDetailSchema.parse((await vocalSession.api.grades.saveExamResults(examTable.id, {
      results: [{ registrationId: examRegistration.id, outcome: 'Failed', grade: 5, notes: 'Acta rectificada' }]
    })).body);
    expect(correctedResult.registrations[0].result).toMatchObject({ version: 2, outcome: 'Failed', grade: 5 });
    expect((await admin.grades.publishExamTable(examTable.id)).response.status()).toBe(200);
    expect(await db.getEnrollmentState(enrollmentId)).toEqual({ status: 1, finalGrade: 6 });
    expect(studentExamTableSchema.array().parse((await studentSession.api.grades.myExamTables()).body)[0].result)
      .toMatchObject({ version: 2, outcome: 'Failed', grade: 5 });
  } finally {
    await professorSession?.dispose();
    await vocalSession?.dispose();
    await studentSession?.dispose();
    if (careerId) await cleanupScenario(admin, db, careerId, studentUserId ? [studentUserId] : [], periodId ? [periodId] : []);
    else if (studentUserId) await db.deleteUser(studentUserId);
    if (professorId) await db.cleanupTeacher(professorId, professorUser.id);
    else await db.deleteUser(professorUser.id);
    if (vocalId) await db.cleanupTeacher(vocalId, vocalUser.id);
    else await db.deleteUser(vocalUser.id);
  }
});
