import 'dotenv/config';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import pg from 'pg';

const firstMigration = '20260905221718_InitialCreate';
const targetDatabase = 'academia_migration_e2e';
const sourceUrl = process.env.E2E_DATABASE_URL;

if (!sourceUrl) throw new Error('E2E_DATABASE_URL is required.');
const source = new URL(sourceUrl);
if (!new Set(['localhost', '127.0.0.1', '::1']).has(source.hostname)) {
  throw new Error('Migration regression is restricted to a local PostgreSQL server.');
}

const adminUrl = new URL(source);
adminUrl.pathname = '/postgres';
const targetUrl = new URL(source);
targetUrl.pathname = `/${targetDatabase}`;
const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const backendRoot = path.resolve(scriptDir, '..', '..', '..');

function npgsqlConnection(url) {
  const quote = value => `"${decodeURIComponent(value).replaceAll('"', '""')}"`;
  return [
    `Host=${url.hostname}`,
    `Port=${url.port || '5432'}`,
    `Database=${quote(url.pathname.replace(/^\//, ''))}`,
    `Username=${quote(url.username)}`,
    `Password=${quote(url.password)}`
  ].join(';');
}

async function recreateDatabase() {
  const client = new pg.Client({ connectionString: adminUrl.toString() });
  await client.connect();
  try {
    await client.query('SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $1 AND pid <> pg_backend_pid();', [targetDatabase]);
    await client.query(`DROP DATABASE IF EXISTS "${targetDatabase}";`);
    await client.query(`CREATE DATABASE "${targetDatabase}";`);
  } finally {
    await client.end();
  }
}

async function dropDatabase() {
  const client = new pg.Client({ connectionString: adminUrl.toString() });
  await client.connect();
  try {
    await client.query('SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $1 AND pid <> pg_backend_pid();', [targetDatabase]);
    await client.query(`DROP DATABASE IF EXISTS "${targetDatabase}";`);
  } finally {
    await client.end();
  }
}

function runDotnet(args) {
  return spawnSync('dotnet', args, {
    cwd: backendRoot,
    env: process.env,
    encoding: 'utf8'
  });
}

function assertSucceeded(result, label) {
  if (result.status !== 0) throw new Error(`${label} failed:\n${result.stdout}\n${result.stderr}`);
}

function migrate(target) {
  const args = [
    'ef', 'database', 'update',
    ...(target ? [target] : []),
    '--project', 'src/AcademiaDigital.Infrastructure/AcademiaDigital.Infrastructure.csproj',
    '--startup-project', 'src/AcademiaDigital.API/AcademiaDigital.API.csproj',
    '--configuration', 'Release',
    '--no-build',
    '--connection', npgsqlConnection(targetUrl)
  ];
  return runDotnet(args);
}

async function seedSnapshot() {
  const client = new pg.Client({ connectionString: targetUrl.toString() });
  await client.connect();
  try {
    await client.query(`
      INSERT INTO "Careers"
        ("name", "code", "description", "total_credits", "duration_years", "is_active", "created_at", "updated_at")
      VALUES ('Carrera upgrade E2E', 'UPGRADE-E2E', 'Snapshot previo', 100, 3, true, NOW(), NOW());
    `);
  } finally {
    await client.end();
  }
}

async function verifyUpgrade() {
  const client = new pg.Client({ connectionString: targetUrl.toString() });
  await client.connect();
  try {
    const career = await client.query('SELECT COUNT(*)::int AS total FROM "Careers" WHERE "code" = $1;', ['UPGRADE-E2E']);
    if (career.rows[0].total !== 1) throw new Error('Upgrade did not preserve the seeded career.');

    const migrations = await client.query('SELECT COUNT(*)::int AS total FROM "__EFMigrationsHistory";');
    if (migrations.rows[0].total < 2) throw new Error('Expected more than one applied migration.');

    const modality = await client.query(`
      SELECT COUNT(*)::int AS total
      FROM information_schema.columns
      WHERE table_schema = 'public' AND table_name = 'AcademicEvents' AND column_name = 'Modality';
    `);
    if (modality.rows[0].total !== 1) throw new Error('Latest AcademicEvents.modality column is missing.');
  } finally {
    await client.end();
  }
}

await recreateDatabase();
try {
  assertSucceeded(runDotnet(['build', 'AcademiaDigital.sln', '-c', 'Release', '--no-restore']), 'Backend build');
  assertSucceeded(migrate(firstMigration), 'Migration to initial snapshot');
  await seedSnapshot();
  assertSucceeded(migrate(), 'Upgrade to latest migration');
  await verifyUpgrade();
  console.log('PostgreSQL migration regression passed: prior snapshot -> latest, data preserved.');
} finally {
  await dropDatabase();
}
