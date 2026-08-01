import bcrypt from 'bcryptjs';
import sql from 'mssql';
import { assertSafeE2eEnvironment, env } from '../config/environment';
import { runToken } from '../factories/data.factory';

export interface SeededUser {
  id: number;
  email: string;
  password: string;
  dni: string;
}

export class E2eDatabase {
  private pool?: sql.ConnectionPool;

  async connect(): Promise<void> {
    assertSafeE2eEnvironment();
    this.pool = await new sql.ConnectionPool(env.sqlConnectionString).connect();
  }

  async close(): Promise<void> {
    await this.pool?.close();
    this.pool = undefined;
  }

  private get db(): sql.ConnectionPool {
    if (!this.pool) throw new Error('E2E database is not connected.');
    return this.pool;
  }

  async seedAdmin(): Promise<SeededUser> {
    const passwordHash = await bcrypt.hash(env.adminPassword, 11);
    const dni = '99000001';
    const result = await this.db.request()
      .input('email', sql.NVarChar(254), env.adminEmail)
      .input('password', sql.NVarChar(128), passwordHash)
      .input('dni', sql.NVarChar(20), dni)
      .query<{ id: number }>(`
        MERGE [Users] AS target
        USING (SELECT @email AS email) AS source
          ON target.[email] = source.[email]
        WHEN MATCHED THEN UPDATE SET
          [password] = @password, [role] = 3, [is_active] = 1,
          [failed_login_attempts] = 0, [locked_until] = NULL
        WHEN NOT MATCHED THEN INSERT
          ([username], [last_name], [email], [password], [dni], [is_active], [date_joined], [role], [failed_login_attempts])
          VALUES ('Admin', 'Playwright', @email, @password, @dni, 1, SYSUTCDATETIME(), 3, 0)
        OUTPUT inserted.[id];
      `);
    return { id: Number(result.recordset[0].id), email: env.adminEmail, password: env.adminPassword, dni };
  }

  async createUnlinkedStudentUser(): Promise<SeededUser> {
    const suffix = runToken();
    const email = `${env.dataPrefix.toLowerCase()}.free.${suffix.toLowerCase()}@e2e.local`;
    const password = `Pw_${suffix}_Aa1!`;
    const numeric = Number.parseInt(suffix, 36) % 90_000_000 + 10_000_000;
    const dni = String(numeric).slice(0, 8);
    const passwordHash = await bcrypt.hash(password, 11);
    const result = await this.db.request()
      .input('username', sql.NVarChar(255), `Free${suffix}`)
      .input('email', sql.NVarChar(254), email)
      .input('password', sql.NVarChar(128), passwordHash)
      .input('dni', sql.NVarChar(20), dni)
      .query<{ id: number }>(`
        INSERT INTO [Users]
          ([username], [last_name], [email], [password], [dni], [is_active], [date_joined], [role], [failed_login_attempts])
        OUTPUT inserted.[id]
        VALUES (@username, 'Playwright', @email, @password, @dni, 1, SYSUTCDATETIME(), 1, 0);
      `);
    return { id: Number(result.recordset[0].id), email, password, dni };
  }

  async findStudentIdByUserId(userId: number): Promise<number | null> {
    const result = await this.db.request().input('userId', sql.BigInt, userId)
      .query<{ id: number }>('SELECT [id] FROM [Students] WHERE [user_id] = @userId;');
    return result.recordset[0]?.id === undefined ? null : Number(result.recordset[0].id);
  }

  async findUserIdByEmail(email: string): Promise<number | null> {
    const result = await this.db.request().input('email', sql.NVarChar(254), email)
      .query<{ id: number }>('SELECT [id] FROM [Users] WHERE [email] = @email;');
    return result.recordset[0]?.id === undefined ? null : Number(result.recordset[0].id);
  }

  async countStudentCareers(studentId: number): Promise<number> {
    const result = await this.db.request().input('studentId', sql.BigInt, studentId)
      .query<{ total: number }>('SELECT COUNT(*) AS [total] FROM [StudentCareers] WHERE [StudentId] = @studentId;');
    return Number(result.recordset[0].total);
  }

  async countStudentStudyPlans(studentId: number): Promise<number> {
    const result = await this.db.request().input('studentId', sql.BigInt, studentId)
      .query<{ total: number }>('SELECT COUNT(*) AS [total] FROM [StudentStudyPlans] WHERE [student_id] = @studentId;');
    return Number(result.recordset[0].total);
  }

  async deleteUser(userId: number): Promise<void> {
    await this.db.request()
      .input('userId', sql.BigInt, userId)
      .input('emailPattern', sql.NVarChar(254), `${env.dataPrefix.toLowerCase()}.%@e2e.local`)
      .query(`
      DELETE FROM [ActiveSessions] WHERE [user_id] = @userId;
      DELETE FROM [Users] WHERE [id] = @userId AND [email] LIKE @emailPattern;
    `);
  }

  async revokeSessions(userId: number): Promise<void> {
    await this.db.request().input('userId', sql.BigInt, userId)
      .query('DELETE FROM [ActiveSessions] WHERE [user_id] = @userId;');
  }

  async cleanupP1Artifacts(options: {
    documentRequirementIds?: number[];
    scholarshipIds?: number[];
    customFieldIds?: number[];
  }): Promise<void> {
    if (env.preserveData) return;
    const request = this.db.request();
    const parameters = (prefix: string, ids: number[]) => ids.map((id, index) => {
      const name = `${prefix}${index}`;
      request.input(name, sql.Int, id);
      return `@${name}`;
    }).join(',') || 'NULL';
    const requirementIds = parameters('requirement', options.documentRequirementIds ?? []);
    const scholarshipIds = parameters('scholarship', options.scholarshipIds ?? []);
    const customFieldIds = parameters('customField', options.customFieldIds ?? []);
    await request.query(`
      DELETE FROM [StudentDocuments] WHERE [DocumentRequirementId] IN (${requirementIds});
      DELETE FROM [StudentScholarships] WHERE [ScholarshipId] IN (${scholarshipIds});
      DELETE FROM [StudentCustomFieldValues] WHERE [CustomFieldDefinitionId] IN (${customFieldIds});
      DELETE FROM [DocumentRequirements] WHERE [Id] IN (${requirementIds});
      DELETE FROM [Scholarships] WHERE [Id] IN (${scholarshipIds});
      DELETE FROM [CustomFieldDefinitions] WHERE [Id] IN (${customFieldIds});
    `);
  }

  async cleanupScenario(careerId: number, userIds: number[]): Promise<void> {
    const request = this.db.request()
      .input('careerId', sql.Int, careerId)
      .input('emailPattern', sql.NVarChar(254), `${env.dataPrefix.toLowerCase()}.%@e2e.local`)
      .input('careerCodePattern', sql.NVarChar(20), `C${env.dataPrefix.slice(0, 5)}%`);
    const userParameters = userIds.map((id, index) => {
      request.input(`user${index}`, sql.BigInt, id);
      return `@user${index}`;
    });
    const users = userParameters.length > 0 ? userParameters.join(',') : 'NULL';
    await request.query(`
      SET XACT_ABORT ON;
      BEGIN TRANSACTION;
      DECLARE @StudentIds TABLE ([Id] bigint PRIMARY KEY);
      INSERT INTO @StudentIds SELECT [id] FROM [Students] WHERE [user_id] IN (${users});

      DELETE FROM [Enrollments]
        WHERE [student_id] IN (SELECT [Id] FROM @StudentIds)
           OR [enrollment_period_id] IN (SELECT [id] FROM [EnrollmentPeriods] WHERE [career_id] = @careerId);
      DELETE FROM [EnrollmentPeriods] WHERE [career_id] = @careerId;
      DELETE FROM [StudentDocuments] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentScholarships] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentCustomFieldValues] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentStatusHistory] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentAcademicAssignments] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentStudyPlans] WHERE [student_id] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentCareers] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [Students] WHERE [id] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [ActiveSessions] WHERE [user_id] IN (${users});
      DELETE FROM [Users] WHERE [id] IN (${users}) AND [email] LIKE @emailPattern;

      DELETE FROM [CoursePrerequisites] WHERE [study_plan_id] IN (SELECT [id] FROM [StudyPlans] WHERE [career_id] = @careerId);
      DELETE FROM [CourseApprovalRules] WHERE [study_plan_course_id] IN
        (SELECT [id] FROM [StudyPlanCourses] WHERE [study_plan_id] IN (SELECT [id] FROM [StudyPlans] WHERE [career_id] = @careerId));
      DELETE FROM [StudyPlanCourses] WHERE [study_plan_id] IN (SELECT [id] FROM [StudyPlans] WHERE [career_id] = @careerId);
      DELETE FROM [Commissions] WHERE [CareerId] = @careerId;
      DELETE FROM [StudyPlans] WHERE [career_id] = @careerId;
      DELETE FROM [Courses] WHERE [career_id] = @careerId;
      DELETE FROM [Careers] WHERE [id] = @careerId AND [code] LIKE @careerCodePattern;
      COMMIT TRANSACTION;
    `);
  }
}

export async function waitForDatabase(attempts = 30): Promise<void> {
  let lastError: unknown;
  for (let attempt = 1; attempt <= attempts; attempt++) {
    const db = new E2eDatabase();
    try {
      await db.connect();
      await db.close();
      return;
    } catch (error) {
      lastError = error;
      await db.close().catch(() => undefined);
      await new Promise((resolve) => setTimeout(resolve, 2000));
    }
  }
  throw lastError;
}
