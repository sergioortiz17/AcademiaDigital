import { expect } from '@playwright/test';
import { ApiClients } from '../../src/fixtures/api.fixture';
import { E2eDatabase } from '../../src/db/e2e-database';
import { registeredStudentData } from '../../src/factories/data.factory';

export async function createRegisteredStudent(api: ApiClients, db: E2eDatabase, careerId: number) {
  const registration = registeredStudentData(careerId);
  const response = await api.auth.register(registration);
  expect(response.response.status()).toBe(201);
  const userId = Number((response.body as { userID: number }).userID);
  const studentId = await db.findStudentIdByUserId(userId);
  if (!studentId) throw new Error('The registered Student was not persisted.');
  return { registration, userId, studentId };
}
