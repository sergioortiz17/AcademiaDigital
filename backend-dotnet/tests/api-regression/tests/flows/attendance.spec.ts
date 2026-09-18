import { randomUUID } from 'node:crypto';
import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  attendanceJustificationSchema,
  attendanceSessionDetailSchema,
  attendanceSessionSchema,
  attendanceSummarySchema,
  enrollmentPeriodSchema,
  teacherAssignmentSchema,
  teacherSchema,
  teachingPositionSchema
} from '../../src/contracts/schemas';
import { registeredStudentData, runToken } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('M6 asistencia completa: carga, cierre, justificaciÃ³n, riesgo, reapertura y exportaciÃ³n @m6 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M6 Asistencias');
  await allure.story('Planilla operativa completa por comisiÃ³n');
  await allure.severity('critical');

  const professor = await db.createUnlinkedTeacherUser();
  const otherProfessor = await db.createUnlinkedTeacherUser();
  let careerId = 0;
  let studentUserId = 0;
  let teacherId = 0;
  let periodId = 0;
  let professorSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let otherProfessorSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const registration = registeredStudentData(careerId);
    const register = await anonymous.auth.register(registration);
    expect(register.response.status()).toBe(201);
    studentUserId = (register.body as { userID: number }).userID;
    const studentId = await db.findStudentIdByUserId(studentUserId);
    if (!studentId) throw new Error('Student profile was not created.');
    expect((await admin.students.assignStudyPlan(studentId, scenario.studyPlan.id, 'Attendance E2E')).response.status()).toBe(204);
    expect((await admin.students.assignAcademic(studentId, {
      careerId,
      studyPlanId: scenario.studyPlan.id,
      commissionId: scenario.commission.id,
      academicYear: 2026,
      yearNumber: 1,
      reason: 'Attendance E2E'
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
    studentSession = await authenticatedClients(registration.email, registration.password);
    expect((await studentSession.api.enrollments.enroll({
      enrollmentPeriodId: periodId,
      shift: 'Tarde',
      studyPlanCourseIds: [scenario.introStudyPlanCourse.id]
    })).response.status()).toBe(201);

    const teacherCall = await admin.teachers.create({
      userId: professor.id,
      employeeNumber: `att-${runToken()}`,
      department: 'AcadÃ©mica',
      specializationArea: 'Asistencias',
      hireDate: '2026-03-01T00:00:00Z'
    });
    expect(teacherCall.response.status()).toBe(201);
    teacherId = teacherSchema.parse(teacherCall.body).id;
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
    const assignmentCall = await admin.teachers.assign(teacherId, {
      teachingPositionId: position.id,
      startedOn: '2026-03-01',
      reason: 'Asistencia M6 E2E'
    });
    expect(assignmentCall.response.status()).toBe(201);
    teacherAssignmentSchema.parse(assignmentCall.body);

    professorSession = await authenticatedClients(professor.email, professor.password);
    otherProfessorSession = await authenticatedClients(otherProfessor.email, otherProfessor.password);
    const sessionBody = {
      teachingPositionId: position.id,
      sessionDate: '2026-08-22',
      startTime: '20:00:00',
      endTime: '23:00:00',
      scope: 'ClassHour',
      units: 2
    };
    expect((await anonymous.attendance.sessions()).response.status()).toBe(401);
    expect((await anonymous.attendance.createSession(randomUUID(), sessionBody)).response.status()).toBe(401);
    expect((await studentSession.api.attendance.sessions()).response.status()).toBe(403);
    expect((await studentSession.api.attendance.createSession(randomUUID(), sessionBody)).response.status()).toBe(403);
    expect((await professorSession.api.attendance.mySummary()).response.status()).toBe(403);
    expect((await otherProfessorSession.api.attendance.sessions()).body).toEqual([]);

    expect((await otherProfessorSession.api.attendance.createSession(randomUUID(), sessionBody)).response.status()).toBe(403);
    expect((await professorSession.api.attendance.createSession(randomUUID(), {
      ...sessionBody, scope: 'FullDay'
    })).response.status()).toBe(400);

    const idempotencyKey = randomUUID();
    const createCall = await professorSession.api.attendance.createSession(idempotencyKey, sessionBody);
    expect(createCall.response.status()).toBe(201);
    const attendanceSession = attendanceSessionSchema.parse(createCall.body);
    const retry = await professorSession.api.attendance.createSession(idempotencyKey, sessionBody);
    expect(retry.response.status()).toBe(201);
    expect(attendanceSessionSchema.parse(retry.body).id).toBe(attendanceSession.id);
    expect(attendanceSessionSchema.array().parse((await professorSession.api.attendance.sessions({
      courseId: scenario.introCourse.id,
      commissionId: scenario.commission.id
    })).body).filter(item => item.id === attendanceSession.id)).toHaveLength(1);
    expect((await otherProfessorSession.api.attendance.session(attendanceSession.id)).response.status()).toBe(403);

    let detail = attendanceSessionDetailSchema.parse(
      (await professorSession.api.attendance.session(attendanceSession.id)).body);
    expect(detail.records).toHaveLength(1);
    expect(detail.records[0]).toMatchObject({ studentId, status: null });
    const enrollmentId = detail.records[0].enrollmentId;
    const load = await professorSession.api.attendance.saveRecords(attendanceSession.id, {
      records: [{ enrollmentId, status: 'Late', notes: 'Ingreso demorado' }]
    });
    expect(load.response.status()).toBe(200);
    detail = attendanceSessionDetailSchema.parse(load.body);
    const record = detail.records[0];
    expect(record).toMatchObject({ status: 'Late', notes: 'Ingreso demorado' });
    if (!record.id) throw new Error('Attendance record was not persisted.');
    const idempotentReload = attendanceSessionDetailSchema.parse((await professorSession.api.attendance.saveRecords(
      attendanceSession.id,
      { records: [{ enrollmentId, status: 'Late', notes: 'Ingreso demorado' }] }
    )).body);
    expect(idempotentReload.session.recordCount).toBe(1);
    expect((await professorSession.api.attendance.saveRecords(attendanceSession.id, {
      records: [{ enrollmentId, status: 'Justified' }]
    })).response.status()).toBe(400);

    const csv = await professorSession.api.attendance.export(attendanceSession.id, 'csv');
    expect(csv.response.status()).toBe(200);
    expect(csv.response.headers()['content-type']).toContain('text/csv');
    expect(csv.rawBody.subarray(0, 3)).toEqual(Buffer.from([0xef, 0xbb, 0xbf]));
    const pdf = await professorSession.api.attendance.export(attendanceSession.id, 'pdf');
    expect(pdf.response.status()).toBe(200);
    expect(pdf.response.headers()['content-type']).toContain('application/pdf');
    expect(pdf.rawBody.subarray(0, 4).toString('ascii')).toBe('%PDF');

    expect((await professorSession.api.attendance.close(attendanceSession.id)).response.status()).toBe(200);
    expect((await professorSession.api.attendance.saveRecords(attendanceSession.id, {
      records: [{ enrollmentId, status: 'Present' }]
    })).response.status()).toBe(409);
    let summary = attendanceSummarySchema.parse((await studentSession.api.attendance.mySummary()).body);
    expect(summary.items[0]).toMatchObject({
      attendancePercentage: 50,
      minimumAttendancePercentage: 75,
      isAtRisk: true,
      lateCount: 1
    });
    expect(attendanceSummarySchema.parse(
      (await professorSession.api.attendance.studentSummary(studentId)).body).studentId).toBe(studentId);
    expect((await otherProfessorSession.api.attendance.studentSummary(studentId)).response.status()).toBe(403);

    const justificationCall = await admin.attendance.justify(record.id, {
      category: 'Certificado laboral',
      reason: 'PresentaciÃ³n validada por secretarÃ­a',
      evidenceUrl: 'storage://attendance/e2e-certificate.pdf'
    });
    expect(justificationCall.response.status()).toBe(201);
    attendanceJustificationSchema.parse(justificationCall.body);
    summary = attendanceSummarySchema.parse((await studentSession.api.attendance.mySummary()).body);
    expect(summary.items[0]).toMatchObject({
      attendancePercentage: null,
      possibleUnits: 0,
      isAtRisk: false,
      justifiedCount: 1
    });

    const expiredSessionCall = await professorSession.api.attendance.createSession(randomUUID(), {
      ...sessionBody,
      sessionDate: '2026-08-01',
      startTime: null,
      endTime: null,
      scope: 'FullDay',
      units: 1
    });
    expect(expiredSessionCall.response.status()).toBe(201);
    const expiredSession = attendanceSessionSchema.parse(expiredSessionCall.body);
    expect((await professorSession.api.attendance.saveRecords(expiredSession.id, {
      records: [{ enrollmentId, status: 'Absent' }]
    })).response.status()).toBe(409);
    expect((await professorSession.api.attendance.reopen(expiredSession.id, 'Intento docente')).response.status()).toBe(403);
    expect((await professorSession.api.attendance.close(expiredSession.id)).response.status()).toBe(200);
    const reopened = await admin.attendance.reopen(expiredSession.id, 'CorrecciÃ³n retroactiva autorizada');
    expect(reopened.response.status()).toBe(200);
    expect(attendanceSessionSchema.parse(reopened.body)).toMatchObject({
      status: 'Open',
      isAdministrativelyReopened: true,
      reopeningCount: 1
    });
    expect((await professorSession.api.attendance.saveRecords(expiredSession.id, {
      records: [{ enrollmentId, status: 'Absent', notes: 'Carga retroactiva autorizada' }]
    })).response.status()).toBe(200);
    expect((await professorSession.api.attendance.close(expiredSession.id)).response.status()).toBe(200);
  } finally {
    await professorSession?.dispose();
    await otherProfessorSession?.dispose();
    await studentSession?.dispose();
    if (careerId) await cleanupScenario(admin, db, careerId, studentUserId ? [studentUserId] : [], periodId ? [periodId] : []);
    else if (studentUserId) await db.deleteUser(studentUserId);
    if (teacherId) await db.cleanupTeacher(teacherId, professor.id);
    else await db.deleteUser(professor.id);
    await db.deleteUser(otherProfessor.id);
  }
});
