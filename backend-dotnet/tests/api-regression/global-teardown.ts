import { E2eDatabase } from './src/db/e2e-database';

export default async function globalTeardown(): Promise<void> {
  const db = new E2eDatabase();
  await db.connect();
  try {
    const admin = await db.seedAdmin();
    await db.revokeSessions(admin.id);
  } finally {
    await db.close();
  }
}
