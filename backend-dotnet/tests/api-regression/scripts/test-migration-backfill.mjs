import 'dotenv/config';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import sql from 'mssql';

const databaseName = 'AcademiaDigitalMigrationE2E';
const previousMigration = '20260728221736_AddMissingUserProfileColumns';
const currentMigration = '20260801174825_AddStudentCareersAndAtomicity';
const sourceConnection = process.env.E2E_SQL_CONNECTION_STRING;

if (!sourceConnection) throw new Error('E2E_SQL_CONNECTION_STRING is required.');
if (!/(?:localhost|127\.0\.0\.1)(?:,|;)/i.test(sourceConnection)) {
  throw new Error('Migration regression is restricted to a local SQL Server.');
}

function withDatabase(connectionString, database) {
  if (!/(?:Database|Initial Catalog)\s*=/i.test(connectionString)) {
    throw new Error('The SQL connection string must declare Database or Initial Catalog.');
  }
  return connectionString.replace(/((?:Database|Initial Catalog)\s*=\s*)[^;]+/i, `$1${database}`);
}

const targetConnection = withDatabase(sourceConnection, databaseName);
const masterConnection = withDatabase(sourceConnection, 'master');
const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const backendRoot = path.resolve(scriptDir, '..', '..', '..');

async function recreateDatabase() {
  const pool = await sql.connect(masterConnection);
  try {
    await pool.request().query(`
      IF DB_ID(N'${databaseName}') IS NOT NULL
      BEGIN
        ALTER DATABASE [${databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [${databaseName}];
      END;
      CREATE DATABASE [${databaseName}];
    `);
  } finally {
    await pool.close();
  }
}

async function dropDatabase() {
  const pool = await sql.connect(masterConnection);
  try {
    await pool.request().query(`
      IF DB_ID(N'${databaseName}') IS NOT NULL
      BEGIN
        ALTER DATABASE [${databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [${databaseName}];
      END;
    `);
  } finally {
    await pool.close();
  }
}

function migrate(target) {
  return spawnSync('dotnet', [
    'ef', 'database', 'update', target,
    '--project', 'src/AcademiaDigital.Infrastructure/AcademiaDigital.Infrastructure.csproj',
    '--startup-project', 'src/AcademiaDigital.API/AcademiaDigital.API.csproj',
    '--configuration', 'Release',
    '--no-build',
    '--connection', targetConnection
  ], {
    cwd: backendRoot,
    env: process.env,
    encoding: 'utf8'
  });
}

function buildBackend() {
  return spawnSync('dotnet', ['build', 'AcademiaDigital.sln', '-c', 'Release', '--no-restore'], {
    cwd: backendRoot,
    env: process.env,
    encoding: 'utf8'
  });
}

function assertMigrationSucceeded(result, label) {
  if (result.status !== 0) throw new Error(`${label} failed:\n${result.stdout}\n${result.stderr}`);
}

async function seedLegacyData({ duplicateStudent = false } = {}) {
  const pool = await sql.connect(targetConnection);
  try {
    await pool.request().query(`
      DECLARE @PrimaryCareerId int, @SecondaryCareerId int, @PrimaryPlanId int, @SecondaryPlanId int;
      DECLARE @StudentUserId bigint, @AdminUserId bigint, @StudentId bigint;
      DECLARE @SecondaryCommissionId int;

      INSERT INTO Careers (name, code, description, total_credits, duration_years, is_active, created_at, updated_at)
      VALUES ('Migración principal', 'MIG-PRI', NULL, 100, 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      SET @PrimaryCareerId = CAST(SCOPE_IDENTITY() AS int);
      INSERT INTO Careers (name, code, description, total_credits, duration_years, is_active, created_at, updated_at)
      VALUES ('Migración secundaria', 'MIG-SEC', NULL, 100, 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      SET @SecondaryCareerId = CAST(SCOPE_IDENTITY() AS int);

      INSERT INTO Users (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
      VALUES ('LegacyAdmin', 'Migration', 'legacy.admin@migration.e2e', 'unused', '99100001', 1, SYSUTCDATETIME(), 3, 0);
      SET @AdminUserId = CAST(SCOPE_IDENTITY() AS bigint);
      INSERT INTO Users (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
      VALUES ('LegacyStudent', 'Migration', 'legacy.student@migration.e2e', 'unused', '99100002', 1, SYSUTCDATETIME(), 1, 0);
      SET @StudentUserId = CAST(SCOPE_IDENTITY() AS bigint);

      INSERT INTO Students (legajo_number, enrollment_date, status, user_id, career_id)
      VALUES ('MIG-LEG-001', '2025-03-01', 0, @StudentUserId, @PrimaryCareerId);
      SET @StudentId = CAST(SCOPE_IDENTITY() AS bigint);

      INSERT INTO StudyPlans (career_id, code, name, version_number, status, effective_from, effective_to, is_active, created_at, updated_at)
      VALUES (@PrimaryCareerId, 'MIG-PLAN-PRI', 'Plan principal', 1, 'Active', '2025-01-01', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      SET @PrimaryPlanId = CAST(SCOPE_IDENTITY() AS int);
      INSERT INTO StudyPlans (career_id, code, name, version_number, status, effective_from, effective_to, is_active, created_at, updated_at)
      VALUES (@SecondaryCareerId, 'MIG-PLAN-SEC', 'Plan secundario', 1, 'Active', '2025-01-01', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      SET @SecondaryPlanId = CAST(SCOPE_IDENTITY() AS int);

      INSERT INTO StudentStudyPlans (student_id, study_plan_id, is_current, assigned_at, ended_at, migration_reason)
      VALUES (@StudentId, @PrimaryPlanId, 0, '2025-03-01', '2025-12-31', 'Plan histórico');
      INSERT INTO StudentStudyPlans (student_id, study_plan_id, is_current, assigned_at, ended_at, migration_reason)
      VALUES (@StudentId, @SecondaryPlanId, 1, '2026-03-01', NULL, 'Segunda carrera previa');

      INSERT INTO Commissions (CareerId, Code, Name, AcademicYear, YearNumber, Shift, IsActive, CreatedAt)
      VALUES (@SecondaryCareerId, 'MIG-COM-SEC', 'Comisión secundaria', 2026, 1, 'Morning', 1, SYSUTCDATETIME());
      SET @SecondaryCommissionId = CAST(SCOPE_IDENTITY() AS int);
      INSERT INTO StudentAcademicAssignments
        (StudentId, CareerId, StudyPlanId, CommissionId, AcademicYear, YearNumber, StartedAt, EndedAt, IsCurrent, Reason, AssignedByUserId)
      VALUES (@StudentId, @SecondaryCareerId, @SecondaryPlanId, @SecondaryCommissionId, 2026, 1,
        '2026-03-01', NULL, 1, 'Asignación previa', @AdminUserId);

      ${duplicateStudent ? `
      INSERT INTO Students (legajo_number, enrollment_date, status, user_id, career_id)
      VALUES ('MIG-LEG-002', '2025-03-02', 0, @StudentUserId, @PrimaryCareerId);
      ` : ''}
    `);
  } finally {
    await pool.close();
  }
}

async function assertBackfill() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM StudentCareers) AS MembershipCount,
        (SELECT COUNT(*) FROM StudentStudyPlans sp
         INNER JOIN StudentCareers sc ON sc.Id = sp.student_career_id
         INNER JOIN StudyPlans p ON p.id = sp.study_plan_id
         WHERE sc.StudentId = sp.student_id AND sc.CareerId = p.career_id) AS LinkedPlans,
        (SELECT COUNT(*) FROM StudentStudyPlans) AS TotalPlans,
        (SELECT COUNT(*) FROM StudentAcademicAssignments a
         INNER JOIN StudentCareers sc ON sc.Id = a.StudentCareerId
         WHERE sc.StudentId = a.StudentId AND sc.CareerId = a.CareerId) AS LinkedAssignments,
        (SELECT COUNT(*) FROM StudentAcademicAssignments) AS TotalAssignments;
    `);
    if (Number(row.MembershipCount) !== 2
      || Number(row.LinkedPlans) !== Number(row.TotalPlans)
      || Number(row.LinkedAssignments) !== Number(row.TotalAssignments)) {
      throw new Error(`Unexpected backfill result: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

try {
  assertMigrationSucceeded(buildBackend(), 'Building the backend');
  await recreateDatabase();
  assertMigrationSucceeded(migrate(previousMigration), 'Creating the legacy schema');
  await seedLegacyData();
  assertMigrationSucceeded(migrate(currentMigration), 'Applying the multi-career migration');
  await assertBackfill();

  await recreateDatabase();
  assertMigrationSucceeded(migrate(previousMigration), 'Recreating the legacy schema');
  await seedLegacyData({ duplicateStudent: true });
  const rejected = migrate(currentMigration);
  const output = `${rejected.stdout}\n${rejected.stderr}`;
  if (rejected.status === 0 || !output.includes('more than one Student exists for the same User')) {
    throw new Error('The migration did not reject duplicate Students for one User with the expected diagnostic.');
  }
  console.log('Migration backfill regression passed: links migrated and duplicate Students rejected.');
} finally {
  await dropDatabase();
}
