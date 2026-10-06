import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import {
  problemSchema,
  teacherAssignmentSchema,
  teacherSchema,
  teachingPositionSchema
} from '../../src/contracts/schemas';
import { runToken } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('cargos, vacantes e historial aislado de asignaciones docentes @m5 @critical @regression @authorization', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('M5 Docentes');
  await allure.story('Cargos y asignaciones históricas');
  await allure.severity('critical');

  const professor = await db.createUnlinkedTeacherUser();
  const otherProfessor = await db.createUnlinkedTeacherUser();
  const student = await db.createUnlinkedStudentUser();
  let careerId = 0;
  let teacherId = 0;
  let otherTeacherId = 0;
  let professorSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let otherProfessorSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  let studentSession: Awaited<ReturnType<typeof authenticatedClients>> | undefined;
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const suffix = runToken();
    const createTeacher = async (userId: number, employeeNumber: string) => {
      const call = await admin.teachers.create({
        userId,
        employeeNumber,
        department: 'Sistemas',
        specializationArea: 'Arquitectura',
        hireDate: '2026-03-01T00:00:00Z'
      });
      expect(call.response.status()).toBe(201);
      return teacherSchema.parse(call.body).id;
    };
    teacherId = await createTeacher(professor.id, `asg-${suffix}`);
    otherTeacherId = await createTeacher(otherProfessor.id, `asg2-${suffix}`);

    const positionBody = {
      courseId: scenario.introCourse.id,
      commissionId: scenario.commission.id,
      academicYear: 2026,
      semester: 1,
      positionType: 'Titular',
      maxStudents: 40
    };

    expect((await anonymous.teachingPositions.list()).response.status()).toBe(401);
    expect((await anonymous.teachingPositions.create(positionBody)).response.status()).toBe(401);
    expect((await anonymous.teachers.myAssignments()).response.status()).toBe(401);

    studentSession = await authenticatedClients(student.email, student.password);
    expect((await studentSession.api.teachingPositions.list()).response.status()).toBe(403);
    expect((await studentSession.api.teachers.assignments(teacherId)).response.status()).toBe(403);
    expect((await studentSession.api.teachers.myAssignments()).response.status()).toBe(403);

    professorSession = await authenticatedClients(professor.email, professor.password);
    otherProfessorSession = await authenticatedClients(otherProfessor.email, otherProfessor.password);
    expect((await professorSession.api.teachingPositions.list()).response.status()).toBe(403);
    expect((await professorSession.api.teachers.assign(teacherId, {
      teachingPositionId: 1, startedOn: '2026-03-01'
    })).response.status()).toBe(403);
    expect(teacherAssignmentSchema.array().parse(
      (await professorSession.api.teachers.myAssignments()).body)).toHaveLength(0);

    const invalidPosition = await admin.teachingPositions.create({ ...positionBody, academicYear: 2027 });
    expect(invalidPosition.response.status()).toBe(400);
    expect(problemSchema.parse(invalidPosition.body).msg).toContain('año académico');

    const createPositionCall = await admin.teachingPositions.create(positionBody);
    expect(createPositionCall.response.status()).toBe(201);
    let position = teachingPositionSchema.parse(createPositionCall.body);
    expect(position).toMatchObject({ isVacant: true, isActive: true, teacherId: null });

    const updatePositionCall = await admin.teachingPositions.update(position.id, {
      ...positionBody, maxStudents: 45
    });
    expect(updatePositionCall.response.status()).toBe(200);
    position = teachingPositionSchema.parse(updatePositionCall.body);
    expect(position.maxStudents).toBe(45);

    const assignCall = await admin.teachers.assign(teacherId, {
      teachingPositionId: position.id,
      startedOn: '2026-03-01',
      reason: 'Designación titular E2E'
    });
    expect(assignCall.response.status()).toBe(201);
    const assignment = teacherAssignmentSchema.parse(assignCall.body);
    expect(assignment).toMatchObject({
      teacherId,
      teachingPositionId: position.id,
      isCurrent: true,
      assignmentReason: 'Designación titular E2E'
    });

    const occupied = teachingPositionSchema.parse((await admin.teachingPositions.get(position.id)).body);
    expect(occupied).toMatchObject({ isVacant: false, teacherId });
    expect((await admin.teachers.assign(otherTeacherId, {
      teachingPositionId: position.id, startedOn: '2026-03-01'
    })).response.status()).toBe(409);
    expect((await admin.teachingPositions.update(position.id, positionBody)).response.status()).toBe(409);

    const ownAssignments = teacherAssignmentSchema.array().parse(
      (await professorSession.api.teachers.myAssignments()).body);
    expect(ownAssignments).toHaveLength(1);
    expect(ownAssignments[0].id).toBe(assignment.id);
    expect(teacherAssignmentSchema.array().parse(
      (await otherProfessorSession.api.teachers.myAssignments()).body)).toHaveLength(0);

    const endCall = await admin.teachers.endAssignment(teacherId, assignment.id, {
      endedOn: '2026-07-15', reason: 'Fin de designación E2E'
    });
    expect(endCall.response.status()).toBe(200);
    expect(teacherAssignmentSchema.parse(endCall.body)).toMatchObject({
      isCurrent: false, endedOn: '2026-07-15', endReason: 'Fin de designación E2E'
    });
    expect(teachingPositionSchema.parse((await admin.teachingPositions.get(position.id)).body))
      .toMatchObject({ isVacant: true, teacherId: null });
    expect(teacherAssignmentSchema.array().parse(
      (await professorSession.api.teachers.myAssignments()).body)).toHaveLength(0);
    expect(teacherAssignmentSchema.array().parse(
      (await professorSession.api.teachers.myAssignments(true)).body)).toHaveLength(1);

    expect((await admin.teachingPositions.deactivate(position.id, 'Cargo cerrado E2E')).response.status()).toBe(204);
    const activePositions = teachingPositionSchema.array().parse((await admin.teachingPositions.list()).body);
    expect(activePositions.some((item) => item.id === position.id)).toBe(false);
    const historicalPositions = teachingPositionSchema.array().parse(
      (await admin.teachingPositions.list({ includeInactive: true })).body);
    expect(historicalPositions.find((item) => item.id === position.id)).toMatchObject({
      isActive: false, deactivationReason: 'Cargo cerrado E2E'
    });
  } finally {
    await professorSession?.dispose();
    await otherProfessorSession?.dispose();
    await studentSession?.dispose();
    if (teacherId) await db.cleanupTeacher(teacherId, professor.id);
    else await db.deleteUser(professor.id);
    if (otherTeacherId) await db.cleanupTeacher(otherTeacherId, otherProfessor.id);
    else await db.deleteUser(otherProfessor.id);
    if (careerId) await cleanupScenario(admin, db, careerId, [student.id]);
    else await db.deleteUser(student.id);
  }
});
