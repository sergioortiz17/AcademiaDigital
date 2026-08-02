import { test, expect } from '../../src/fixtures/api.fixture';
import { registeredStudentData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';

test('un alumno consulta su recurso pero no el de otro @critical @authorization', async ({ admin, anonymous, authenticatedClients, db }) => {
  let careerId = 0;
  const userIds: number[] = [];
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const firstData = registeredStudentData(careerId);
    const secondData = registeredStudentData(careerId);
    const firstRegistration = await anonymous.auth.register(firstData);
    const secondRegistration = await anonymous.auth.register(secondData);
    expect(firstRegistration.response.status()).toBe(201);
    expect(secondRegistration.response.status()).toBe(201);
    userIds.push((firstRegistration.body as { userID: number }).userID, (secondRegistration.body as { userID: number }).userID);
    const firstStudentId = await db.findStudentIdByUserId(userIds[0]);
    const secondStudentId = await db.findStudentIdByUserId(userIds[1]);
    if (!firstStudentId || !secondStudentId) throw new Error('Registered students were not persisted.');

    const firstSession = await authenticatedClients(firstData.email, firstData.password);
    const own = await firstSession.api.students.get(firstStudentId);
    expect(own.response.status()).toBe(200);
    const other = await firstSession.api.students.get(secondStudentId);
    expect(other.response.status()).toBe(403);
  } finally {
    await cleanupScenario(admin, db, careerId, userIds);
  }
});
