import 'dotenv/config';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import sql from 'mssql';

const databaseName = 'AcademiaDigitalMigrationE2E';
const previousMigration = '20260728221736_AddMissingUserProfileColumns';
const currentMigration = '20260801174825_AddStudentCareersAndAtomicity';
const admissionPreviousMigration = '20260822153414_AddAdmissionFormsAndApplications';
const admissionCurrentMigration = '20260822155958_AddAdmissionApplicationStatusHistory';
const admissionCapacityMigration = '20260822162850_AddAdmissionCapacityAndWaitlist';
const admissionCommissionMigration = '20260822164101_LinkAdmissionFormsToCommissions';
const rematriculationMigration = '20260822171401_AddStudentRematriculations';
const admissionDocumentsMigration = '20260822172937_AddAdmissionApplicationDocuments';
const admissionAgreementsMigration = '20260822175416_AddAdmissionAgreementsAndOutbox';
const teacherProfilesMigration = '20260822202331_AddTeacherProfilesAndSoftDelete';
const teacherM5Migration = '20260822214829_AddTeacherDocumentsPositionsAndAssignments';
const attendanceMigration = '20260822223239_AddAttendanceModule';
const gradesMigration = '20260822232101_AddGradebooksAndExamTables';
const certificatesMigration = '20260823000423_AddCertificateIssuanceModule';
const financeMigration = '20260824135158_AddFinanceConceptsPlansAndDebts';
const paymentsMigration = '20260824144910_AddPaymentsAndReconciliation';
const receiptsMigration = '20260824165321_AddDigitalReceipts';
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
  let result;
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    result = spawnSync('dotnet', [
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
    const output = `${result.stdout}\n${result.stderr}`;
    const transientConnectionFailure = !output.includes('requires encryption')
      && (output.includes('Error Number:20') || output.includes('likely due to a transient failure'));
    if (result.status === 0 || !transientConnectionFailure) return result;
  }
  return result;
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

async function seedAdmissionWithoutHistory() {
  const pool = await sql.connect(targetConnection);
  try {
    await pool.request().query(`
      INSERT INTO Careers (name, code, description, total_credits, duration_years, is_active, created_at, updated_at)
      VALUES ('Migración admisión', 'MIG-ADM', NULL, 100, 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      DECLARE @CareerId int = CAST(SCOPE_IDENTITY() AS int);

      INSERT INTO AdmissionForms
        (career_id, slug, title, description, terms_text, reservation_hours, is_active, created_at, updated_at)
      VALUES
        (@CareerId, 'migracion-admision', 'Ingreso migrado', NULL, 'Terms', 72, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      DECLARE @FormId int = CAST(SCOPE_IDENTITY() AS int);

      INSERT INTO AdmissionApplications
        (public_id, admission_form_id, applicant_email, applicant_dni, submitted_fields_json, status,
         terms_accepted_at, reservation_expires_at, created_at, updated_at)
      VALUES
        (NEWID(), @FormId, 'legacy.admission@migration.e2e', '99123456', '{}', 1,
         SYSUTCDATETIME(), DATEADD(hour, 72, SYSUTCDATETIME()), SYSUTCDATETIME(), SYSUTCDATETIME());
    `);
  } finally {
    await pool.close();
  }
}

async function assertAdmissionHistoryBackfill() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT COUNT(*) AS Total,
             MIN(from_status) AS FromStatus,
             MIN(to_status) AS ToStatus,
             MIN(reason) AS Reason
      FROM AdmissionApplicationStatusHistory;
    `);
    if (Number(row.Total) !== 1
      || row.FromStatus !== null
      || Number(row.ToStatus) !== 1
      || !String(row.Reason).includes('Backfilled')) {
      throw new Error(`Unexpected admission history backfill: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertAdmissionCapacityMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT f.capacity AS Capacity, a.reservation_expires_at AS ReservationExpiresAt
      FROM AdmissionForms f
      INNER JOIN AdmissionApplications a ON a.admission_form_id = f.id
      WHERE f.slug = 'migracion-admision';
    `);
    if (row.Capacity !== null || row.ReservationExpiresAt === null) {
      throw new Error(`Unexpected admission capacity upgrade: ${JSON.stringify(row)}`);
    }

    let constraintRejected = false;
    try {
      await pool.request().query("UPDATE AdmissionForms SET capacity = 0 WHERE slug = 'migracion-admision';");
    } catch (error) {
      constraintRejected = String(error).includes('CK_AdmissionForms_Capacity');
    }
    if (!constraintRejected)
      throw new Error('Admission capacity check constraint did not reject zero capacity.');
  } finally {
    await pool.close();
  }
}

async function assertAdmissionCommissionMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        f.commission_id AS CommissionId,
        i.is_unique AS IsUnique,
        i.filter_definition AS FilterDefinition,
        (SELECT COUNT(*) FROM sys.foreign_keys
         WHERE name = 'FK_AdmissionForms_Commissions_commission_id') AS ForeignKeyCount
      FROM AdmissionForms f
      CROSS JOIN sys.indexes i
      WHERE f.slug = 'migracion-admision'
        AND i.object_id = OBJECT_ID('AdmissionForms')
        AND i.name = 'IX_AdmissionForms_commission_id';
    `);
    if (row.CommissionId !== null
      || !row.IsUnique
      || !String(row.FilterDefinition).includes('commission_id')
      || Number(row.ForeignKeyCount) !== 1) {
      throw new Error(`Unexpected admission commission upgrade: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertRematriculationMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM StudentRematriculations) AS Total,
        i.is_unique AS IsUnique,
        (SELECT COUNT(*) FROM sys.foreign_keys
         WHERE parent_object_id = OBJECT_ID('StudentRematriculations')) AS ForeignKeyCount
      FROM sys.indexes i
      WHERE i.object_id = OBJECT_ID('StudentRematriculations')
        AND i.name = 'IX_StudentRematriculations_student_career_id_academic_year';
    `);
    if (Number(row.Total) !== 0 || !row.IsUnique || Number(row.ForeignKeyCount) !== 6) {
      throw new Error(`Unexpected rematriculation schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertAdmissionDocumentsMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM AdmissionApplicationDocuments) AS Total,
        (SELECT COUNT(*) FROM sys.foreign_keys
         WHERE parent_object_id = OBJECT_ID('AdmissionApplicationDocuments')) AS ForeignKeyCount,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('AdmissionApplicationDocuments')
           AND name = 'IX_AdmissionApplicationDocuments_admission_application_id_document_requirement_id_submitted_at') AS LifecycleIndexCount;
    `);
    if (Number(row.Total) !== 0
      || Number(row.ForeignKeyCount) !== 3
      || Number(row.LifecycleIndexCount) !== 1) {
      throw new Error(`Unexpected admission documents schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertAdmissionAgreementsMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM AdmissionAgreements) AS AgreementTotal,
        (SELECT COUNT(*) FROM OutboxMessages) AS OutboxTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys
         WHERE parent_object_id = OBJECT_ID('AdmissionAgreements')) AS AgreementForeignKeys,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('AdmissionAgreements')
           AND is_unique = 1
           AND name IN ('IX_AdmissionAgreements_admission_application_id', 'IX_AdmissionAgreements_agreement_number')) AS AgreementUniqueIndexes,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('OutboxMessages')
           AND is_unique = 1
           AND name = 'IX_OutboxMessages_deduplication_key') AS OutboxDeduplicationIndex;
    `);
    if (Number(row.AgreementTotal) !== 0
      || Number(row.OutboxTotal) !== 0
      || Number(row.AgreementForeignKeys) !== 1
      || Number(row.AgreementUniqueIndexes) !== 2
      || Number(row.OutboxDeduplicationIndex) !== 1) {
      throw new Error(`Unexpected admission agreement/outbox schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertTeacherProfilesMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM sys.columns
         WHERE object_id = OBJECT_ID('Teachers')
           AND name IN (
             'address_line', 'city', 'province', 'postal_code',
             'emergency_contact_name', 'emergency_contact_relationship', 'emergency_contact_phone',
             'deactivated_at', 'deactivated_by_user_id', 'deactivation_reason')) AS ProfileColumnCount,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('Teachers')
           AND name = 'IX_Teachers_user_id'
           AND is_unique = 1) AS UniqueUserIndexCount;
    `);
    if (Number(row.ProfileColumnCount) !== 10 || Number(row.UniqueUserIndexCount) !== 1) {
      throw new Error(`Unexpected teacher profile schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function seedLegacyTeachingPosition() {
  const pool = await sql.connect(targetConnection);
  try {
    await pool.request().query(`
      DECLARE @CareerId int = (SELECT TOP (1) id FROM Careers ORDER BY id);
      DECLARE @TeacherUserId bigint, @TeacherId bigint, @CourseId int;

      INSERT INTO Users (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
      VALUES ('LegacyTeacher', 'Migration', 'legacy.teacher@migration.e2e', 'unused', '99100003', 1, SYSUTCDATETIME(), 2, 0);
      SET @TeacherUserId = CAST(SCOPE_IDENTITY() AS bigint);

      INSERT INTO Teachers (employee_number, department, specialization_area, hire_date, is_active, PhoneNumber, user_id)
      VALUES ('MIG-DOC-001', 'Sistemas', 'Migración', '2026-03-01', 1, NULL, @TeacherUserId);
      SET @TeacherId = CAST(SCOPE_IDENTITY() AS bigint);

      INSERT INTO Courses (career_id, code, name, description, is_active, created_at, updated_at)
      VALUES (@CareerId, 'MIG-DOC-CUR', 'Curso docente migrado', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      SET @CourseId = CAST(SCOPE_IDENTITY() AS int);

      INSERT INTO TeachingPositions (academic_year, semester, position_type, max_students, is_vacant, course_id, teacher_id)
      VALUES (2026, 1, 0, 40, 1, @CourseId, @TeacherId);
    `);
  } finally {
    await pool.close();
  }
}

async function assertTeacherDocumentsMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM TeacherDocuments) AS Total,
        (SELECT COUNT(*) FROM sys.foreign_keys
         WHERE parent_object_id = OBJECT_ID('TeacherDocuments')) AS ForeignKeyCount,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('TeacherDocuments')
           AND name = 'IX_TeacherDocuments_teacher_id_document_type_version'
           AND is_unique = 1) AS VersionIndexCount,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('TeacherDocuments')
           AND name = 'IX_TeacherDocuments_teacher_id_submitted_at') AS TimelineIndexCount;
    `);
    if (Number(row.Total) !== 0
      || Number(row.ForeignKeyCount) !== 2
      || Number(row.VersionIndexCount) !== 1
      || Number(row.TimelineIndexCount) !== 1) {
      throw new Error(`Unexpected teacher documents schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertTeachingAssignmentsMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM TeacherAssignments) AS AssignmentTotal,
        (SELECT COUNT(*) FROM TeacherAssignments WHERE is_current = 1 AND assignment_reason LIKE 'Backfilled%') AS BackfilledTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('TeacherAssignments')) AS AssignmentForeignKeys,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('TeacherAssignments')
           AND name = 'IX_TeacherAssignments_teaching_position_id'
           AND is_unique = 1) AS CurrentPositionIndex,
        (SELECT COUNT(*) FROM sys.check_constraints
         WHERE parent_object_id = OBJECT_ID('TeachingPositions')
           AND name = 'CK_TeachingPositions_AssignmentState') AS StateConstraint,
        (SELECT COUNT(*) FROM TeachingPositions
         WHERE teacher_id IS NOT NULL AND is_vacant = 0
           AND created_at > '2000-01-01' AND updated_at > '2000-01-01') AS NormalizedPositions;
    `);
    if (Number(row.AssignmentTotal) !== 1
      || Number(row.BackfilledTotal) !== 1
      || Number(row.AssignmentForeignKeys) !== 4
      || Number(row.CurrentPositionIndex) !== 1
      || Number(row.StateConstraint) !== 1
      || Number(row.NormalizedPositions) !== 1) {
      throw new Error(`Unexpected teaching assignment schema/backfill: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertAttendanceMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM AttendanceSessions) AS SessionTotal,
        (SELECT COUNT(*) FROM AttendanceRecords) AS RecordTotal,
        (SELECT COUNT(*) FROM AttendanceJustifications) AS JustificationTotal,
        (SELECT COUNT(*) FROM AttendanceSessionReopenings) AS ReopeningTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys
         WHERE parent_object_id IN (
           OBJECT_ID('AttendanceSessions'), OBJECT_ID('AttendanceRecords'),
           OBJECT_ID('AttendanceJustifications'), OBJECT_ID('AttendanceSessionReopenings'))) AS ForeignKeyTotal,
        (SELECT COUNT(*) FROM sys.check_constraints
         WHERE parent_object_id = OBJECT_ID('AttendanceSessions')
           AND name IN ('CK_AttendanceSessions_Units', 'CK_AttendanceSessions_TimeRange')) AS SessionConstraints,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('AttendanceSessions')
           AND is_unique = 1
           AND name IN (
             'IX_AttendanceSessions_idempotency_key',
             'IX_AttendanceSessions_course_id_commission_id_academic_year_semester_session_date_start_time_scope')) AS SessionUniqueIndexes,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('AttendanceRecords')
           AND is_unique = 1
           AND name = 'IX_AttendanceRecords_attendance_session_id_enrollment_id') AS RecordUniqueIndex,
        (SELECT COUNT(*) FROM sys.indexes
         WHERE object_id = OBJECT_ID('AttendanceJustifications')
           AND is_unique = 1
           AND has_filter = 1) AS CurrentJustificationIndex;
    `);
    if (Number(row.SessionTotal) !== 0
      || Number(row.RecordTotal) !== 0
      || Number(row.JustificationTotal) !== 0
      || Number(row.ReopeningTotal) !== 0
      || Number(row.ForeignKeyTotal) !== 13
      || Number(row.SessionConstraints) !== 2
      || Number(row.SessionUniqueIndexes) !== 2
      || Number(row.RecordUniqueIndex) !== 1
      || Number(row.CurrentJustificationIndex) !== 1) {
      throw new Error(`Unexpected attendance schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertGradesMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM Gradebooks) AS GradebookTotal,
        (SELECT COUNT(*) FROM ExamTables) AS ExamTableTotal,
        (SELECT COUNT(*) FROM sys.tables WHERE name IN (
          'Gradebooks', 'GradebookEvaluations', 'GradeEntryRevisions', 'GradebookReopenings',
          'ExamTables', 'ExamTribunalMembers', 'ExamRegistrations', 'ExamGradeRevisions', 'ExamTableReopenings')) AS TableTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (
          OBJECT_ID('Gradebooks'), OBJECT_ID('GradebookEvaluations'), OBJECT_ID('GradeEntryRevisions'), OBJECT_ID('GradebookReopenings'),
          OBJECT_ID('ExamTables'), OBJECT_ID('ExamTribunalMembers'), OBJECT_ID('ExamRegistrations'), OBJECT_ID('ExamGradeRevisions'), OBJECT_ID('ExamTableReopenings'))) AS ForeignKeyTotal,
        (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN (
          'CK_CourseApprovalRules_FinalExamGrade', 'CK_ExamTables_CallNumber', 'CK_ExamTables_Deadline',
          'CK_ExamRegistrations_Attempt', 'CK_ExamGradeRevisions_Grade',
          'CK_GradebookEvaluations_Maximum', 'CK_GradebookEvaluations_Weight', 'CK_GradeEntryRevisions_Score')) AS ConstraintTotal,
        (SELECT COUNT(*) FROM sys.indexes WHERE is_unique = 1 AND name IN (
          'IX_Gradebooks_idempotency_key', 'IX_Gradebooks_course_id_commission_id_academic_year_semester',
          'IX_GradebookEvaluations_gradebook_id_display_order', 'IX_GradebookEvaluations_gradebook_id_name',
          'IX_GradeEntryRevisions_evaluation_id_enrollment_id', 'IX_GradeEntryRevisions_evaluation_id_enrollment_id_version',
          'IX_ExamTables_idempotency_key', 'IX_ExamTables_course_id_exam_date_utc_call_number',
          'IX_ExamTribunalMembers_exam_table_id_role', 'IX_ExamTribunalMembers_exam_table_id_teacher_id',
          'IX_ExamRegistrations_exam_table_id_enrollment_id', 'IX_ExamRegistrations_enrollment_id_attempt_number',
          'IX_ExamGradeRevisions_exam_registration_id', 'IX_ExamGradeRevisions_exam_registration_id_version')) AS UniqueIndexTotal,
        (SELECT COUNT(*) FROM CourseApprovalRules WHERE minimum_final_exam_grade <> 6) AS InvalidFinalThresholds;
    `);
    if (Number(row.GradebookTotal) !== 0
      || Number(row.ExamTableTotal) !== 0
      || Number(row.TableTotal) !== 9
      || Number(row.ForeignKeyTotal) !== 30
      || Number(row.ConstraintTotal) !== 8
      || Number(row.UniqueIndexTotal) !== 14
      || Number(row.InvalidFinalThresholds) !== 0) {
      throw new Error(`Unexpected grades/exam schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function seedLegacyCertificateRequests() {
  const pool = await sql.connect(targetConnection);
  try {
    await pool.request().query(`
      DECLARE @CareerId int, @StudentUserId bigint, @StudentId bigint;
      INSERT INTO [Careers] ([name], [code], [description], [total_credits], [duration_years], [is_active], [created_at], [updated_at])
      VALUES ('Migración certificados', 'MIG-CERT', NULL, 100, 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      SET @CareerId = CAST(SCOPE_IDENTITY() AS int);
      INSERT INTO [Users] ([username], [last_name], [email], [password], [dni], [is_active], [date_joined], [role], [failed_login_attempts])
      VALUES ('LegacyCertificate', 'Migration', 'legacy.certificate@migration.e2e', 'unused', '99100004', 1, SYSUTCDATETIME(), 1, 0);
      SET @StudentUserId = CAST(SCOPE_IDENTITY() AS bigint);
      INSERT INTO [Students] ([legajo_number], [enrollment_date], [status], [user_id], [career_id], [updated_at])
      VALUES ('MIG-CERT-001', '2025-03-01', 0, @StudentUserId, @CareerId, SYSUTCDATETIME());
      SET @StudentId = CAST(SCOPE_IDENTITY() AS bigint);
      INSERT INTO [StudentCareers] ([StudentId], [CareerId], [EnrollmentDate], [IsActive], [CreatedAt], [UpdatedAt])
      VALUES (@StudentId, @CareerId, '2025-03-01', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
      INSERT INTO [CertificateRequests] ([user_id], [certificate_type], [status], [created_at]) VALUES
        (@StudentUserId, 'Certificado de alumno regular', 0, '2026-01-01T00:00:00Z'),
        (@StudentUserId, 'Certificado de alumno regular', 1, '2026-02-01T00:00:00Z'),
        (@StudentUserId, 'Certificado de promedio', 0, '2026-03-01T00:00:00Z');
    `);
  } finally {
    await pool.close();
  }
}

async function assertCertificatesMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM [CertificateRequests]) AS RequestTotal,
        (SELECT COUNT(*) FROM [CertificateRequests] WHERE [student_career_id] IS NOT NULL) AS LinkedRequests,
        (SELECT COUNT(*) FROM [CertificateRequests] WHERE [kind] = 0) AS RegularRequests,
        (SELECT COUNT(*) FROM [CertificateRequests] WHERE [kind] = 3) AS AcademicRequests,
        (SELECT COUNT(*) FROM [CertificateRequests] WHERE [kind] = 0 AND [status] IN (0, 1, 3)) AS ActiveRegularRequests,
        (SELECT COUNT(*) FROM [CertificateRequests]
          WHERE [status] = 2 AND [rejection_reason] = 'Superseded duplicate during M8 migration.') AS SupersededRequests,
        (SELECT COUNT(*) FROM [CertificateIssuances]) AS IssuanceTotal,
        (SELECT COUNT(*) FROM [CertificateSequences] WHERE [id] = 1 AND [last_value] = 0) AS SequenceSeed,
        (SELECT COUNT(*) FROM sys.tables WHERE [name] IN ('CertificateIssuances', 'CertificateSequences')) AS TableTotal,
        (SELECT COUNT(*) FROM sys.check_constraints
          WHERE [name] IN ('CK_CertificateIssuances_Sequence', 'CK_CertificateSequences_Singleton')) AS ConstraintTotal,
        (SELECT COUNT(*) FROM sys.indexes WHERE [is_unique] = 1 AND [name] IN (
          'IX_CertificateRequests_user_id_student_career_id_kind_exam_registration_id',
          'IX_CertificateIssuances_certificate_number', 'IX_CertificateIssuances_certificate_request_id',
          'IX_CertificateIssuances_public_id', 'IX_CertificateIssuances_sequence_number')) AS UniqueIndexTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE [name] IN (
          'FK_CertificateRequests_ExamRegistrations_exam_registration_id',
          'FK_CertificateRequests_StudentCareers_student_career_id',
          'FK_CertificateRequests_Users_reviewed_by_user_id',
          'FK_CertificateRequests_Users_user_id',
          'FK_CertificateIssuances_CertificateRequests_certificate_request_id',
          'FK_CertificateIssuances_Users_issued_by_user_id')) AS ForeignKeyTotal;
    `);
    if (Number(row.RequestTotal) !== 3
      || Number(row.LinkedRequests) !== 3
      || Number(row.RegularRequests) !== 2
      || Number(row.AcademicRequests) !== 1
      || Number(row.ActiveRegularRequests) !== 1
      || Number(row.SupersededRequests) !== 1
      || Number(row.IssuanceTotal) !== 0
      || Number(row.SequenceSeed) !== 1
      || Number(row.TableTotal) !== 2
      || Number(row.ConstraintTotal) !== 2
      || Number(row.UniqueIndexTotal) !== 5
      || Number(row.ForeignKeyTotal) !== 6) {
      throw new Error(`Unexpected certificates schema/backfill: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertFinanceMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM sys.tables WHERE [name] IN (
          'FinancialConcepts', 'FinancialRates', 'FinancialBenefits', 'BillingPlans',
          'BillingPlanItems', 'DebtGenerationBatches', 'StudentDebts')) AS TableTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (
          OBJECT_ID('FinancialRates'), OBJECT_ID('FinancialBenefits'), OBJECT_ID('BillingPlans'),
          OBJECT_ID('BillingPlanItems'), OBJECT_ID('DebtGenerationBatches'), OBJECT_ID('StudentDebts'))) AS ForeignKeyTotal,
        (SELECT COUNT(*) FROM sys.check_constraints WHERE [name] IN (
          'CK_FinancialRates_Amount', 'CK_FinancialRates_Surcharge',
          'CK_FinancialBenefits_Percentage', 'CK_FinancialBenefits_Scholarship', 'CK_FinancialBenefits_Validity',
          'CK_BillingPlanItems_Installment', 'CK_DebtGenerationBatches_Count', 'CK_DebtGenerationBatches_Total',
          'CK_StudentDebts_Amounts', 'CK_StudentDebts_Currency')) AS ConstraintTotal,
        (SELECT COUNT(*) FROM sys.indexes WHERE [is_unique] = 1 AND [name] IN (
          'IX_FinancialConcepts_code', 'IX_FinancialBenefits_code',
          'IX_BillingPlans_career_id_academic_year_name',
          'IX_BillingPlanItems_billing_plan_id_financial_concept_id_installment_number',
          'IX_DebtGenerationBatches_idempotency_key', 'IX_DebtGenerationBatches_public_id',
          'IX_FinancialRates_financial_concept_id_career_id_academic_year_student_condition',
          'UX_FinancialRates_Default',
          'IX_StudentDebts_public_id',
          'IX_StudentDebts_student_career_id_billing_plan_item_id',
          'IX_StudentDebts_debt_generation_batch_id_student_career_id_billing_plan_item_id')) AS UniqueIndexTotal,
        (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('FinancialRates')
          AND [is_unique] = 1 AND [has_filter] = 1) AS FilteredRateIndexes,
        (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('StudentDebts')
          AND [name] IN ('base_amount', 'surcharge_amount', 'discount_amount', 'total_amount', 'paid_amount')
          AND [precision] = 18 AND [scale] = 2) AS DebtMoneyColumns,
        (SELECT COUNT(*) FROM FinancialConcepts) AS ConceptTotal,
        (SELECT COUNT(*) FROM StudentDebts) AS DebtTotal;
    `);
    if (Number(row.TableTotal) !== 7
      || Number(row.ForeignKeyTotal) !== 17
      || Number(row.ConstraintTotal) !== 10
      || Number(row.UniqueIndexTotal) !== 11
      || Number(row.FilteredRateIndexes) !== 2
      || Number(row.DebtMoneyColumns) !== 5
      || Number(row.ConceptTotal) !== 0
      || Number(row.DebtTotal) !== 0) {
      throw new Error(`Unexpected finance schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertPaymentsMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM sys.tables WHERE [name] IN (
          'PaymentMethods', 'Payments', 'PaymentAllocations', 'PaymentReconciliations', 'PaymentReversals')) AS TableTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (
          OBJECT_ID('Payments'), OBJECT_ID('PaymentAllocations'),
          OBJECT_ID('PaymentReconciliations'), OBJECT_ID('PaymentReversals'))) AS ForeignKeyTotal,
        (SELECT COUNT(*) FROM sys.check_constraints WHERE [name] IN (
          'CK_Payments_Amount', 'CK_Payments_Currency', 'CK_Payments_Status',
          'CK_PaymentAllocations_Amount', 'CK_PaymentReconciliations_Decision',
          'CK_PaymentReversals_Amount')) AS ConstraintTotal,
        (SELECT COUNT(*) FROM sys.indexes WHERE [is_unique] = 1 AND [name] IN (
          'IX_PaymentMethods_code', 'IX_PaymentMethods_kind',
          'IX_Payments_public_id', 'IX_Payments_confirmation_idempotency_key',
          'IX_PaymentAllocations_payment_id_student_debt_id',
          'IX_PaymentReconciliations_payment_id',
          'IX_PaymentReversals_payment_id', 'IX_PaymentReversals_public_id')) AS UniqueIndexTotal,
        (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('Payments')
          AND [name] = 'IX_Payments_confirmation_idempotency_key'
          AND [is_unique] = 1 AND [has_filter] = 1) AS FilteredIdempotencyIndex,
        (SELECT COUNT(*) FROM sys.columns WHERE
          (object_id = OBJECT_ID('Payments') AND [name] = 'amount'
           OR object_id = OBJECT_ID('PaymentAllocations') AND [name] = 'amount'
           OR object_id = OBJECT_ID('PaymentReversals') AND [name] = 'amount')
          AND [precision] = 18 AND [scale] = 2) AS MoneyColumns,
        (SELECT COUNT(*) FROM PaymentMethods WHERE [code] IN
          ('CASH', 'BANK_TRANSFER', 'DEBIT_CARD', 'CREDIT_CARD') AND [is_active] = 1) AS SeededMethods,
        (SELECT COUNT(*) FROM Payments) AS PaymentTotal,
        (SELECT COUNT(*) FROM PaymentAllocations) AS AllocationTotal,
        (SELECT COUNT(*) FROM PaymentReconciliations) AS ReconciliationTotal,
        (SELECT COUNT(*) FROM PaymentReversals) AS ReversalTotal;
    `);
    if (Number(row.TableTotal) !== 5
      || Number(row.ForeignKeyTotal) !== 11
      || Number(row.ConstraintTotal) !== 6
      || Number(row.UniqueIndexTotal) !== 8
      || Number(row.FilteredIdempotencyIndex) !== 1
      || Number(row.MoneyColumns) !== 3
      || Number(row.SeededMethods) !== 4
      || Number(row.PaymentTotal) !== 0
      || Number(row.AllocationTotal) !== 0
      || Number(row.ReconciliationTotal) !== 0
      || Number(row.ReversalTotal) !== 0) {
      throw new Error(`Unexpected payments schema: ${JSON.stringify(row)}`);
    }
  } finally {
    await pool.close();
  }
}

async function assertReceiptsMigration() {
  const pool = await sql.connect(targetConnection);
  try {
    const { recordset: [row] } = await pool.request().query(`
      SELECT
        (SELECT COUNT(*) FROM sys.tables WHERE [name] IN ('Receipts', 'ReceiptSequences')) AS TableTotal,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('Receipts')) AS ForeignKeyTotal,
        (SELECT COUNT(*) FROM sys.check_constraints WHERE [name] IN (
          'CK_Receipts_Sequence', 'CK_Receipts_Status', 'CK_ReceiptSequences_Singleton')) AS ConstraintTotal,
        (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('Receipts')
          AND [is_unique] = 1 AND [name] IN (
            'IX_Receipts_payment_id', 'IX_Receipts_public_id',
            'IX_Receipts_receipt_number', 'IX_Receipts_sequence_number')) AS UniqueIndexTotal,
        (SELECT COUNT(*) FROM ReceiptSequences WHERE [id] = 1 AND [last_value] = 0) AS SequenceSeed,
        (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('Receipts')
          AND [name] IN ('fiscal_cae', 'fiscal_qr_data') AND [is_nullable] = 1) AS FutureFiscalColumns,
        (SELECT COUNT(*) FROM Receipts) AS ReceiptTotal;
    `);
    if (Number(row.TableTotal) !== 2
      || Number(row.ForeignKeyTotal) !== 2
      || Number(row.ConstraintTotal) !== 3
      || Number(row.UniqueIndexTotal) !== 4
      || Number(row.SequenceSeed) !== 1
      || Number(row.FutureFiscalColumns) !== 2
      || Number(row.ReceiptTotal) !== 0) {
      throw new Error(`Unexpected receipts schema: ${JSON.stringify(row)}`);
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

  await recreateDatabase();
  assertMigrationSucceeded(migrate(admissionPreviousMigration), 'Creating the admission schema without history');
  await seedAdmissionWithoutHistory();
  assertMigrationSucceeded(migrate(admissionCurrentMigration), 'Applying the admission history migration');
  await assertAdmissionHistoryBackfill();
  assertMigrationSucceeded(migrate(admissionCapacityMigration), 'Applying the admission capacity migration');
  await assertAdmissionCapacityMigration();
  assertMigrationSucceeded(migrate(admissionCommissionMigration), 'Applying the admission commission migration');
  await assertAdmissionCommissionMigration();
  assertMigrationSucceeded(migrate(rematriculationMigration), 'Applying the student rematriculation migration');
  await assertRematriculationMigration();
  assertMigrationSucceeded(migrate(admissionDocumentsMigration), 'Applying the admission documents migration');
  await assertAdmissionDocumentsMigration();
  assertMigrationSucceeded(migrate(admissionAgreementsMigration), 'Applying the admission agreement/outbox migration');
  await assertAdmissionAgreementsMigration();
  assertMigrationSucceeded(migrate(teacherProfilesMigration), 'Applying the teacher profile migration');
  await assertTeacherProfilesMigration();
  await seedLegacyTeachingPosition();
  assertMigrationSucceeded(migrate(teacherM5Migration), 'Applying the teacher M5 documents/positions/assignments migration');
  await assertTeacherDocumentsMigration();
  await assertTeachingAssignmentsMigration();
  assertMigrationSucceeded(migrate(attendanceMigration), 'Applying the M6 attendance migration');
  await assertAttendanceMigration();
  assertMigrationSucceeded(migrate(gradesMigration), 'Applying the M7 gradebooks/exam tables migration');
  await assertGradesMigration();
  await seedLegacyCertificateRequests();
  assertMigrationSucceeded(migrate(certificatesMigration), 'Applying the M8 certificates migration');
  await assertCertificatesMigration();
  assertMigrationSucceeded(migrate(financeMigration), 'Applying the M9 finance migration');
  await assertFinanceMigration();
  assertMigrationSucceeded(migrate(paymentsMigration), 'Applying the M10 payments migration');
  await assertPaymentsMigration();
  assertMigrationSucceeded(migrate(receiptsMigration), 'Applying the M11 receipts migration');
  await assertReceiptsMigration();

  console.log('Migration regression passed: multi-career links, admissions, rematriculation, teacher M5 backfill, M6 attendance, M7 grades/exams, M8 certificates, M9 finance, M10 payments and M11 receipts verified.');
} finally {
  await dropDatabase();
}
