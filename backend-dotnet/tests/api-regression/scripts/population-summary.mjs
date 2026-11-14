import 'dotenv/config';
import pg from 'pg';

const databaseUrl = process.env.E2E_DATABASE_URL;
if (!databaseUrl) throw new Error('E2E_DATABASE_URL is required.');

const client = new pg.Client({ connectionString: databaseUrl });
await client.connect();
try {
  const tables = ['Users', 'Students', 'Careers', 'Courses', 'Enrollments', 'AdmissionApplications'];
  for (const table of tables) {
    const exists = await client.query('SELECT to_regclass($1) IS NOT NULL AS exists;', [`public."${table}"`]);
    if (!exists.rows[0].exists) continue;
    const result = await client.query(`SELECT COUNT(*)::int AS total FROM "${table}";`);
    console.log(`${table}: ${result.rows[0].total}`);
  }
} finally {
  await client.end();
}
