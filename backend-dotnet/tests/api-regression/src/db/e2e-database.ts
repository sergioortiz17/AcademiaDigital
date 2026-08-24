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

export interface SeededAdmissionForm {
  careerId: number;
  formId: number;
  slug: string;
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

  async createUnlinkedTeacherUser(): Promise<SeededUser> {
    const suffix = runToken();
    const email = `${env.dataPrefix.toLowerCase()}.teacher.${suffix.toLowerCase()}@e2e.local`;
    const password = `Pw_${suffix}_Aa1!`;
    const numeric = Number.parseInt(suffix, 36) % 90_000_000 + 10_000_000;
    const dni = String(numeric).slice(0, 8);
    const passwordHash = await bcrypt.hash(password, 11);
    const result = await this.db.request()
      .input('username', sql.NVarChar(255), `Teacher${suffix}`)
      .input('email', sql.NVarChar(254), email)
      .input('password', sql.NVarChar(128), passwordHash)
      .input('dni', sql.NVarChar(20), dni)
      .query<{ id: number }>(`
        INSERT INTO [Users]
          ([username], [last_name], [email], [password], [dni], [is_active], [date_joined], [role], [failed_login_attempts])
        OUTPUT inserted.[id]
        VALUES (@username, 'Playwright', @email, @password, @dni, 1, SYSUTCDATETIME(), 2, 0);
      `);
    return { id: Number(result.recordset[0].id), email, password, dni };
  }

  async cleanupTeacher(teacherId: number, userId: number): Promise<void> {
    if (env.preserveData) return;
    await this.db.request()
      .input('teacherId', sql.BigInt, teacherId)
      .input('userId', sql.BigInt, userId)
      .input('emailPattern', sql.NVarChar(254), `${env.dataPrefix.toLowerCase()}.teacher.%@e2e.local`)
      .query(`
        DECLARE @ExamTableIds TABLE ([Id] bigint PRIMARY KEY);
        INSERT INTO @ExamTableIds
          SELECT DISTINCT [exam_table_id] FROM [ExamTribunalMembers] WHERE [teacher_id] = @teacherId;
        DELETE FROM [ExamGradeRevisions] WHERE [exam_registration_id] IN
          (SELECT [id] FROM [ExamRegistrations] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds));
        DELETE FROM [ExamTableReopenings] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds);
        DELETE FROM [ExamRegistrations] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds);
        DELETE FROM [ExamTribunalMembers] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds);
        DELETE FROM [ExamTables] WHERE [id] IN (SELECT [Id] FROM @ExamTableIds);
        DECLARE @GradebookIds TABLE ([Id] bigint PRIMARY KEY);
        INSERT INTO @GradebookIds
          SELECT [id] FROM [Gradebooks] WHERE [teaching_position_id] IN
            (SELECT [teaching_position_id] FROM [TeacherAssignments] WHERE [teacher_id] = @teacherId);
        DELETE FROM [GradeEntryRevisions] WHERE [gradebook_id] IN (SELECT [Id] FROM @GradebookIds);
        DELETE FROM [GradebookReopenings] WHERE [gradebook_id] IN (SELECT [Id] FROM @GradebookIds);
        DELETE FROM [GradebookEvaluations] WHERE [gradebook_id] IN (SELECT [Id] FROM @GradebookIds);
        DELETE FROM [Gradebooks] WHERE [id] IN (SELECT [Id] FROM @GradebookIds);
        DELETE FROM [AttendanceJustifications] WHERE [attendance_record_id] IN
          (SELECT [id] FROM [AttendanceRecords] WHERE [attendance_session_id] IN
            (SELECT [id] FROM [AttendanceSessions] WHERE [teaching_position_id] IN
              (SELECT [teaching_position_id] FROM [TeacherAssignments] WHERE [teacher_id] = @teacherId)
              OR [created_by_user_id] = @userId));
        DELETE FROM [AttendanceSessionReopenings] WHERE [attendance_session_id] IN
          (SELECT [id] FROM [AttendanceSessions] WHERE [teaching_position_id] IN
            (SELECT [teaching_position_id] FROM [TeacherAssignments] WHERE [teacher_id] = @teacherId)
            OR [created_by_user_id] = @userId);
        DELETE FROM [AttendanceRecords] WHERE [attendance_session_id] IN
          (SELECT [id] FROM [AttendanceSessions] WHERE [teaching_position_id] IN
            (SELECT [teaching_position_id] FROM [TeacherAssignments] WHERE [teacher_id] = @teacherId)
            OR [created_by_user_id] = @userId);
        DELETE FROM [AttendanceSessions] WHERE [teaching_position_id] IN
          (SELECT [teaching_position_id] FROM [TeacherAssignments] WHERE [teacher_id] = @teacherId)
          OR [created_by_user_id] = @userId;
        DELETE FROM [TeacherDocuments] WHERE [teacher_id] = @teacherId;
        DELETE FROM [TeacherAssignments] WHERE [teacher_id] = @teacherId;
        UPDATE [TeachingPositions] SET [teacher_id] = NULL, [is_vacant] = 1 WHERE [teacher_id] = @teacherId;
        DELETE FROM [Teachers] WHERE [id] = @teacherId;
        DELETE FROM [ActiveSessions] WHERE [user_id] = @userId;
        DELETE FROM [Users] WHERE [id] = @userId AND [email] LIKE @emailPattern;
      `);
  }

  async seedAdmissionForm(): Promise<SeededAdmissionForm> {
    const suffix = runToken();
    const slug = `ingreso-${suffix.toLowerCase()}`;
    const careerCode = `ADM${suffix}`.slice(0, 20);
    const result = await this.db.request()
      .input('careerCode', sql.NVarChar(20), careerCode)
      .input('careerName', sql.NVarChar(200), `${env.dataPrefix} Admisión ${suffix}`)
      .input('slug', sql.NVarChar(100), slug)
      .query<{ careerId: number; formId: number }>(`
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        INSERT INTO [Careers]
          ([name], [code], [description], [total_credits], [duration_years], [is_active], [created_at], [updated_at])
        VALUES
          (@careerName, @careerCode, 'Carrera temporal de admisión E2E', 160, 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        DECLARE @CareerId int = SCOPE_IDENTITY();

        INSERT INTO [AdmissionForms]
          ([career_id], [slug], [title], [description], [terms_text], [reservation_hours], [is_active], [created_at], [updated_at])
        VALUES
          (@CareerId, @slug, 'Ingreso E2E', 'Formulario público E2E', 'Acepto los términos de admisión.', 72, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        DECLARE @FormId int = SCOPE_IDENTITY();

        INSERT INTO [AdmissionFormFields]
          ([admission_form_id], [key], [label], [type], [is_required], [sort_order])
        VALUES
          (@FormId, 'email', 'Correo electrónico', 1, 1, 1),
          (@FormId, 'dni', 'DNI', 0, 1, 2),
          (@FormId, 'firstName', 'Nombre', 0, 1, 3),
          (@FormId, 'phone', 'Teléfono', 2, 0, 4);

        COMMIT TRANSACTION;
        SELECT @CareerId AS [careerId], @FormId AS [formId];
      `);
    return {
      careerId: Number(result.recordset[0].careerId),
      formId: Number(result.recordset[0].formId),
      slug
    };
  }

  async countAdmissionApplications(formId: number): Promise<number> {
    const result = await this.db.request().input('formId', sql.Int, formId)
      .query<{ total: number }>('SELECT COUNT(*) AS [total] FROM [AdmissionApplications] WHERE [admission_form_id] = @formId;');
    return Number(result.recordset[0].total);
  }

  async expireAdmissionReservation(publicId: string): Promise<void> {
    await this.db.request()
      .input('publicId', sql.UniqueIdentifier, publicId)
      .query(`
        UPDATE [AdmissionApplications]
        SET [reservation_expires_at] = DATEADD(hour, -1, SYSUTCDATETIME())
        WHERE [public_id] = @publicId AND [status] IN (0, 1);
      `);
  }

  async countStudentRematriculations(studentId: number): Promise<number> {
    const result = await this.db.request().input('studentId', sql.BigInt, studentId)
      .query<{ total: number }>('SELECT COUNT(*) AS [total] FROM [StudentRematriculations] WHERE [student_id] = @studentId;');
    return Number(result.recordset[0].total);
  }

  async getEnrollmentState(enrollmentId: number): Promise<{ status: number; finalGrade: number | null }> {
    const result = await this.db.request().input('enrollmentId', sql.BigInt, enrollmentId)
      .query<{ status: number; finalGrade: number | null }>(`
        SELECT [status], [final_grade] AS [finalGrade]
        FROM [Enrollments]
        WHERE [id] = @enrollmentId;
      `);
    if (result.recordset.length !== 1) throw new Error(`Enrollment ${enrollmentId} not found.`);
    return {
      status: Number(result.recordset[0].status),
      finalGrade: result.recordset[0].finalGrade === null ? null : Number(result.recordset[0].finalGrade)
    };
  }

  async getCurrentAcademicAssignment(studentId: number): Promise<{
    academicYear: number;
    yearNumber: number;
    commissionId: number;
  }> {
    const result = await this.db.request().input('studentId', sql.BigInt, studentId)
      .query<{ academicYear: number; yearNumber: number; commissionId: number }>(`
        SELECT [AcademicYear] AS [academicYear], [YearNumber] AS [yearNumber], [CommissionId] AS [commissionId]
        FROM [StudentAcademicAssignments]
        WHERE [StudentId] = @studentId AND [IsCurrent] = 1;
      `);
    if (result.recordset.length !== 1)
      throw new Error(`Expected one current academic assignment for student ${studentId}.`);
    return result.recordset[0];
  }

  async cleanupAdmissionForm(formId: number, careerId: number): Promise<void> {
    if (env.preserveData) return;
    await this.db.request()
      .input('formId', sql.Int, formId)
      .input('careerId', sql.Int, careerId)
      .input('careerNamePattern', sql.NVarChar(200), `${env.dataPrefix} Admisión %`)
      .query(`
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;
        DECLARE @CommissionId int = (SELECT [commission_id] FROM [AdmissionForms] WHERE [id] = @formId);
        DELETE o FROM [OutboxMessages] o
          INNER JOIN [AdmissionApplications] a ON o.[aggregate_id] = CONVERT(nvarchar(36), a.[public_id])
          WHERE a.[admission_form_id] = @formId;
        DELETE FROM [AdmissionApplications] WHERE [admission_form_id] = @formId;
        DELETE FROM [AdmissionFormFields] WHERE [admission_form_id] = @formId;
        DELETE FROM [AdmissionForms] WHERE [id] = @formId;
        DELETE FROM [Commissions] WHERE [Id] = @CommissionId;
        DELETE FROM [Careers] WHERE [id] = @careerId AND [name] LIKE @careerNamePattern;
        COMMIT TRANSACTION;
      `);
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

      DECLARE @PaymentIds TABLE ([Id] bigint PRIMARY KEY);
      INSERT INTO @PaymentIds SELECT [id] FROM [Payments] WHERE [student_id] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [Receipts] WHERE [payment_id] IN (SELECT [Id] FROM @PaymentIds);
      DELETE FROM [PaymentReversals] WHERE [payment_id] IN (SELECT [Id] FROM @PaymentIds);
      DELETE FROM [PaymentReconciliations] WHERE [payment_id] IN (SELECT [Id] FROM @PaymentIds);
      DELETE FROM [PaymentAllocations] WHERE [payment_id] IN (SELECT [Id] FROM @PaymentIds);
      DELETE FROM [Payments] WHERE [id] IN (SELECT [Id] FROM @PaymentIds);

      DECLARE @BillingPlanIds TABLE ([Id] bigint PRIMARY KEY);
      INSERT INTO @BillingPlanIds SELECT [Id] FROM [BillingPlans] WHERE [career_id] = @careerId;
      DECLARE @FinancialConceptIds TABLE ([Id] int PRIMARY KEY);
      INSERT INTO @FinancialConceptIds
        SELECT DISTINCT [financial_concept_id] FROM [FinancialRates] WHERE [career_id] = @careerId
        UNION
        SELECT DISTINCT [financial_concept_id] FROM [BillingPlanItems]
          WHERE [billing_plan_id] IN (SELECT [Id] FROM @BillingPlanIds);
      DELETE FROM [StudentDebts]
        WHERE [student_id] IN (SELECT [Id] FROM @StudentIds)
           OR [debt_generation_batch_id] IN
              (SELECT [Id] FROM [DebtGenerationBatches] WHERE [billing_plan_id] IN (SELECT [Id] FROM @BillingPlanIds));
      DELETE FROM [DebtGenerationBatches] WHERE [billing_plan_id] IN (SELECT [Id] FROM @BillingPlanIds);
      DELETE FROM [BillingPlanItems] WHERE [billing_plan_id] IN (SELECT [Id] FROM @BillingPlanIds);
      DELETE FROM [BillingPlans] WHERE [Id] IN (SELECT [Id] FROM @BillingPlanIds);
      DELETE FROM [FinancialBenefits] WHERE [career_id] = @careerId;
      DELETE FROM [FinancialRates] WHERE [career_id] = @careerId;
      DELETE FROM [FinancialConcepts] WHERE [Id] IN (SELECT [Id] FROM @FinancialConceptIds)
        AND NOT EXISTS (SELECT 1 FROM [FinancialRates] r WHERE r.[financial_concept_id] = [FinancialConcepts].[Id])
        AND NOT EXISTS (SELECT 1 FROM [BillingPlanItems] i WHERE i.[financial_concept_id] = [FinancialConcepts].[Id])
        AND NOT EXISTS (SELECT 1 FROM [StudentDebts] d WHERE d.[financial_concept_id] = [FinancialConcepts].[Id]);

      DELETE FROM [CertificateIssuances] WHERE [certificate_request_id] IN
        (SELECT [id] FROM [CertificateRequests] WHERE [user_id] IN (${users}));
      DELETE FROM [CertificateRequests] WHERE [user_id] IN (${users});

      DECLARE @AttendanceSessionIds TABLE ([Id] bigint PRIMARY KEY);
      INSERT INTO @AttendanceSessionIds
        SELECT [id] FROM [AttendanceSessions]
        WHERE [course_id] IN (SELECT [id] FROM [Courses] WHERE [career_id] = @careerId)
           OR [commission_id] IN (SELECT [Id] FROM [Commissions] WHERE [CareerId] = @careerId);
      DELETE FROM [AttendanceJustifications] WHERE [attendance_record_id] IN
        (SELECT [id] FROM [AttendanceRecords] WHERE [attendance_session_id] IN (SELECT [Id] FROM @AttendanceSessionIds));
      DELETE FROM [AttendanceSessionReopenings] WHERE [attendance_session_id] IN (SELECT [Id] FROM @AttendanceSessionIds);
      DELETE FROM [AttendanceRecords] WHERE [attendance_session_id] IN (SELECT [Id] FROM @AttendanceSessionIds);
      DELETE FROM [AttendanceSessions] WHERE [id] IN (SELECT [Id] FROM @AttendanceSessionIds);

      DECLARE @ExamTableIds TABLE ([Id] bigint PRIMARY KEY);
      INSERT INTO @ExamTableIds SELECT [id] FROM [ExamTables]
        WHERE [course_id] IN (SELECT [id] FROM [Courses] WHERE [career_id] = @careerId);
      DELETE FROM [ExamGradeRevisions] WHERE [exam_registration_id] IN
        (SELECT [id] FROM [ExamRegistrations] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds));
      DELETE FROM [ExamTableReopenings] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds);
      DELETE FROM [ExamRegistrations] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds);
      DELETE FROM [ExamTribunalMembers] WHERE [exam_table_id] IN (SELECT [Id] FROM @ExamTableIds);
      DELETE FROM [ExamTables] WHERE [id] IN (SELECT [Id] FROM @ExamTableIds);

      DECLARE @GradebookIds TABLE ([Id] bigint PRIMARY KEY);
      INSERT INTO @GradebookIds SELECT [id] FROM [Gradebooks]
        WHERE [course_id] IN (SELECT [id] FROM [Courses] WHERE [career_id] = @careerId)
           OR [commission_id] IN (SELECT [Id] FROM [Commissions] WHERE [CareerId] = @careerId);
      DELETE FROM [GradeEntryRevisions] WHERE [gradebook_id] IN (SELECT [Id] FROM @GradebookIds);
      DELETE FROM [GradebookReopenings] WHERE [gradebook_id] IN (SELECT [Id] FROM @GradebookIds);
      DELETE FROM [GradebookEvaluations] WHERE [gradebook_id] IN (SELECT [Id] FROM @GradebookIds);
      DELETE FROM [Gradebooks] WHERE [id] IN (SELECT [Id] FROM @GradebookIds);

      DELETE FROM [Enrollments]
        WHERE [student_id] IN (SELECT [Id] FROM @StudentIds)
           OR [enrollment_period_id] IN (SELECT [id] FROM [EnrollmentPeriods] WHERE [career_id] = @careerId);
      DELETE FROM [EnrollmentPeriods] WHERE [career_id] = @careerId;
      DELETE FROM [StudentDocuments] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentScholarships] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentCustomFieldValues] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentStatusHistory] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentRematriculations]
        WHERE [student_id] IN (SELECT [Id] FROM @StudentIds) OR [career_id] = @careerId;
      DELETE FROM [StudentAcademicAssignments] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentStudyPlans] WHERE [student_id] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [StudentCareers] WHERE [StudentId] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [Students] WHERE [id] IN (SELECT [Id] FROM @StudentIds);
      DELETE FROM [ActiveSessions] WHERE [user_id] IN (${users});
      DELETE FROM [Users] WHERE [id] IN (${users}) AND [email] LIKE @emailPattern;

      DELETE FROM [TeacherAssignments] WHERE [teaching_position_id] IN
        (SELECT [id] FROM [TeachingPositions] WHERE [course_id] IN (SELECT [id] FROM [Courses] WHERE [career_id] = @careerId));
      DELETE FROM [TeachingPositions] WHERE [course_id] IN (SELECT [id] FROM [Courses] WHERE [career_id] = @careerId);
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
