import bcrypt from 'bcryptjs';
import { Pool, type PoolClient, type QueryResultRow } from 'pg';
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
  private pool?: Pool;

  async connect(): Promise<void> {
    assertSafeE2eEnvironment();
    this.pool = new Pool({ connectionString: env.databaseUrl, max: 4 });
    await this.pool.query('SELECT 1');
  }

  async close(): Promise<void> {
    await this.pool?.end();
    this.pool = undefined;
  }

  private get db(): Pool {
    if (!this.pool) throw new Error('E2E database is not connected.');
    return this.pool;
  }

  private async one<T extends QueryResultRow>(text: string, values: unknown[] = []): Promise<T> {
    const result = await this.db.query<T>(text, values);
    if (result.rows.length !== 1) throw new Error(`Expected one database row, received ${result.rows.length}.`);
    return result.rows[0];
  }

  private async transaction<T>(operation: (client: PoolClient) => Promise<T>): Promise<T> {
    const client = await this.db.connect();
    try {
      await client.query('BEGIN');
      const result = await operation(client);
      await client.query('COMMIT');
      return result;
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async seedAdmin(): Promise<SeededUser> {
    const passwordHash = await bcrypt.hash(env.adminPassword, 11);
    const dni = '99000001';
    const row = await this.one<{ id: string }>(`
      INSERT INTO "Users"
        ("username", "last_name", "email", "password", "dni", "is_active", "date_joined", "role", "failed_login_attempts")
      VALUES ('Admin', 'Playwright', $1, $2, $3, true, NOW(), 3, 0)
      ON CONFLICT ("email") DO UPDATE SET
        "password" = EXCLUDED."password",
        "role" = 3,
        "is_active" = true,
        "failed_login_attempts" = 0,
        "locked_until" = NULL
      RETURNING "id";
    `, [env.adminEmail, passwordHash, dni]);
    return { id: Number(row.id), email: env.adminEmail, password: env.adminPassword, dni };
  }

  async createUnlinkedStudentUser(): Promise<SeededUser> {
    return this.createUnlinkedUser(1, 'free');
  }

  async createUnlinkedTeacherUser(): Promise<SeededUser> {
    return this.createUnlinkedUser(2, 'teacher');
  }

  private async createUnlinkedUser(role: number, kind: string): Promise<SeededUser> {
    const suffix = runToken();
    const email = `${env.dataPrefix.toLowerCase()}.${kind}.${suffix.toLowerCase()}@e2e.local`;
    const password = `Pw_${suffix}_Aa1!`;
    const numeric = Number.parseInt(suffix, 36) % 90_000_000 + 10_000_000;
    const dni = String(numeric).slice(0, 8);
    const passwordHash = await bcrypt.hash(password, 11);
    const row = await this.one<{ id: string }>(`
      INSERT INTO "Users"
        ("username", "last_name", "email", "password", "dni", "is_active", "date_joined", "role", "failed_login_attempts")
      VALUES ($1, 'Playwright', $2, $3, $4, true, NOW(), $5, 0)
      RETURNING "id";
    `, [`${kind}${suffix}`, email, passwordHash, dni, role]);
    return { id: Number(row.id), email, password, dni };
  }

  async findStudentIdByUserId(userId: number): Promise<number | null> {
    const result = await this.db.query<{ id: string }>('SELECT "id" FROM "Students" WHERE "user_id" = $1;', [userId]);
    return result.rows[0] ? Number(result.rows[0].id) : null;
  }

  async findUserIdByEmail(email: string): Promise<number | null> {
    const result = await this.db.query<{ id: string }>('SELECT "id" FROM "Users" WHERE "email" = $1;', [email]);
    return result.rows[0] ? Number(result.rows[0].id) : null;
  }

  async countStudentCareers(studentId: number): Promise<number> {
    const row = await this.one<{ total: string }>('SELECT COUNT(*) AS total FROM "StudentCareers" WHERE "StudentId" = $1;', [studentId]);
    return Number(row.total);
  }

  async countStudentStudyPlans(studentId: number): Promise<number> {
    const row = await this.one<{ total: string }>('SELECT COUNT(*) AS total FROM "StudentStudyPlans" WHERE "student_id" = $1;', [studentId]);
    return Number(row.total);
  }

  async getFinanceStudentTarget(studentId: number, academicYear: number): Promise<{
    studentId: number;
    careerId: number;
    studentCareerId: number;
    academicYear: number;
    condition: 'Regular' | 'Libre' | 'Graduated' | 'Withdrawn';
    grantedScholarshipIds: number[];
    studentName: string;
    dni: string;
    legajoNumber: string;
    careerName: string;
  }> {
    const row = await this.one<{
      studentId: string;
      careerId: number;
      studentCareerId: string;
      condition: number;
      studentName: string;
      dni: string;
      legajoNumber: string;
      careerName: string;
    }>(`
      SELECT s."id" AS "studentId", s."career_id" AS "careerId", sc."Id" AS "studentCareerId",
             s."status" AS "condition", CONCAT(u."username", ' ', u."last_name") AS "studentName",
             u."dni" AS "dni", s."legajo_number" AS "legajoNumber", c."name" AS "careerName"
      FROM "Students" s
      JOIN "Users" u ON u."id" = s."user_id"
      JOIN "StudentCareers" sc ON sc."StudentId" = s."id" AND sc."CareerId" = s."career_id" AND sc."IsActive" = true
      JOIN "Careers" c ON c."id" = sc."CareerId"
      WHERE s."id" = $1;
    `, [studentId]);
    const scholarships = await this.db.query<{ scholarshipId: number }>(`
      SELECT "ScholarshipId" AS "scholarshipId"
      FROM "StudentScholarships"
      WHERE "StudentId" = $1 AND "AcademicYear" = $2 AND "Status" = 1;
    `, [studentId, academicYear]);
    const conditions = ['Regular', 'Libre', 'Graduated', 'Withdrawn'] as const;
    return {
      studentId: Number(row.studentId),
      careerId: Number(row.careerId),
      studentCareerId: Number(row.studentCareerId),
      academicYear,
      condition: conditions[Number(row.condition)],
      grantedScholarshipIds: scholarships.rows.map(item => Number(item.scholarshipId)),
      studentName: row.studentName,
      dni: row.dni,
      legajoNumber: row.legajoNumber,
      careerName: row.careerName
    };
  }

  async deleteUser(userId: number): Promise<void> {
    await this.db.query('DELETE FROM "ActiveSessions" WHERE "user_id" = $1;', [userId]);
    await this.db.query(
      'DELETE FROM "Users" WHERE "id" = $1 AND "email" LIKE $2;',
      [userId, `${env.dataPrefix.toLowerCase()}.%@e2e.local`]
    );
  }

  async revokeSessions(userId: number): Promise<void> {
    await this.db.query('DELETE FROM "ActiveSessions" WHERE "user_id" = $1;', [userId]);
  }

  async cleanupTeacher(teacherId: number, userId: number): Promise<void> {
    if (env.preserveData) return;
    await this.transaction(async client => {
      await client.query('DELETE FROM "TeacherDocuments" WHERE "teacher_id" = $1;', [teacherId]);
      await client.query('DELETE FROM "TeacherAssignments" WHERE "teacher_id" = $1;', [teacherId]);
      await client.query('DELETE FROM "ExamTribunalMembers" WHERE "teacher_id" = $1;', [teacherId]);
      await client.query('DELETE FROM "TeacherCareers" WHERE "TeacherId" = $1;', [teacherId]);
      await client.query('UPDATE "CourseSections" SET "teacher_id" = NULL, "is_vacant" = true WHERE "teacher_id" = $1;', [teacherId]);
      await client.query('DELETE FROM "Teachers" WHERE "id" = $1;', [teacherId]);
      await client.query('DELETE FROM "ActiveSessions" WHERE "user_id" = $1;', [userId]);
      // El usuario queda como evidencia de auditoria: sesiones, planillas y actas guardan
      // referencias restrictivas al operador. La base E2E completa es descartable.
    });
  }

  async seedAdmissionForm(): Promise<SeededAdmissionForm> {
    const suffix = runToken();
    const slug = `ingreso-${suffix.toLowerCase()}`;
    const careerCode = `ADM${suffix}`.slice(0, 20);
    return this.transaction(async client => {
      const career = await client.query<{ id: number }>(`
        INSERT INTO "Careers"
          ("name", "code", "description", "total_credits", "duration_years", "is_active", "created_at", "updated_at")
        VALUES ($1, $2, 'Carrera temporal de admisión E2E', 160, 3, true, NOW(), NOW())
        RETURNING "id";
      `, [`${env.dataPrefix} Admisión ${suffix}`, careerCode]);
      const careerId = Number(career.rows[0].id);
      const form = await client.query<{ id: number }>(`
        INSERT INTO "AdmissionForms"
          ("career_id", "slug", "title", "description", "terms_text", "reservation_hours", "is_active", "created_at", "updated_at")
        VALUES ($1, $2, 'Ingreso E2E', 'Formulario público E2E', 'Acepto los términos de admisión.', 72, true, NOW(), NOW())
        RETURNING "id";
      `, [careerId, slug]);
      const formId = Number(form.rows[0].id);
      await client.query(`
        INSERT INTO "AdmissionFormFields"
          ("admission_form_id", "key", "label", "type", "is_required", "sort_order")
        VALUES
          ($1, 'email', 'Correo electrónico', 1, true, 1),
          ($1, 'dni', 'DNI', 0, true, 2),
          ($1, 'firstName', 'Nombre', 0, true, 3),
          ($1, 'phone', 'Teléfono', 2, false, 4);
      `, [formId]);
      return { careerId, formId, slug };
    });
  }

  async countAdmissionApplications(formId: number): Promise<number> {
    const row = await this.one<{ total: string }>('SELECT COUNT(*) AS total FROM "AdmissionApplications" WHERE "admission_form_id" = $1;', [formId]);
    return Number(row.total);
  }

  async expireAdmissionReservation(publicId: string): Promise<void> {
    await this.db.query(`
      UPDATE "AdmissionApplications"
      SET "reservation_expires_at" = NOW() - INTERVAL '1 hour'
      WHERE "public_id" = $1 AND "status" IN (0, 1);
    `, [publicId]);
  }

  async countStudentRematriculations(studentId: number): Promise<number> {
    const row = await this.one<{ total: string }>('SELECT COUNT(*) AS total FROM "StudentRematriculations" WHERE "student_id" = $1;', [studentId]);
    return Number(row.total);
  }

  async getEnrollmentState(enrollmentId: number): Promise<{ status: number; finalGrade: number | null }> {
    const row = await this.one<{ status: number; finalGrade: string | null }>(`
      SELECT "status", "final_grade" AS "finalGrade" FROM "Enrollments" WHERE "id" = $1;
    `, [enrollmentId]);
    return { status: Number(row.status), finalGrade: row.finalGrade === null ? null : Number(row.finalGrade) };
  }

  async getCurrentAcademicAssignment(studentId: number): Promise<{
    academicYear: number;
    yearNumber: number;
    commissionId: number;
  }> {
    const row = await this.one<{ academicYear: number; yearNumber: number; commissionId: number }>(`
      SELECT "AcademicYear" AS "academicYear", "YearNumber" AS "yearNumber", "DivisionId" AS "commissionId"
      FROM "StudentAcademicAssignments"
      WHERE "StudentId" = $1 AND "IsCurrent" = true;
    `, [studentId]);
    return {
      academicYear: Number(row.academicYear),
      yearNumber: Number(row.yearNumber),
      commissionId: Number(row.commissionId)
    };
  }

  async cleanupAdmissionForm(formId: number, careerId: number): Promise<void> {
    if (env.preserveData) return;
    await this.transaction(async client => {
      const form = await client.query<{ division_id: number | null }>(
        'SELECT "division_id" FROM "AdmissionForms" WHERE "id" = $1;', [formId]
      );
      const divisionId = form.rows[0]?.division_id ?? null;
      await client.query(`
        DELETE FROM "OutboxMessages" o USING "AdmissionApplications" a
        WHERE o."aggregate_id" = a."public_id"::text AND a."admission_form_id" = $1;
      `, [formId]);
      await client.query('DELETE FROM "AdmissionApplications" WHERE "admission_form_id" = $1;', [formId]);
      await client.query('DELETE FROM "AdmissionFormFields" WHERE "admission_form_id" = $1;', [formId]);
      await client.query('DELETE FROM "AdmissionForms" WHERE "id" = $1;', [formId]);
      if (divisionId !== null)
        await client.query('DELETE FROM "Divisions" WHERE "Id" = $1;', [divisionId]);
      await client.query('DELETE FROM "Careers" WHERE "id" = $1 AND "name" LIKE $2;', [careerId, `${env.dataPrefix} Admisión %`]);
    });
  }

  async cleanupP1Artifacts(options: {
    documentRequirementIds?: number[];
    scholarshipIds?: number[];
    customFieldIds?: number[];
  }): Promise<void> {
    if (env.preserveData) return;
    const requirements = options.documentRequirementIds ?? [];
    const scholarships = options.scholarshipIds ?? [];
    const fields = options.customFieldIds ?? [];
    if (requirements.length) {
      await this.db.query('DELETE FROM "StudentDocuments" WHERE "DocumentRequirementId" = ANY($1::int[]);', [requirements]);
      await this.db.query('DELETE FROM "DocumentRequirements" WHERE "Id" = ANY($1::int[]);', [requirements]);
    }
    if (scholarships.length) {
      await this.db.query('DELETE FROM "StudentScholarships" WHERE "ScholarshipId" = ANY($1::int[]);', [scholarships]);
      await this.db.query('DELETE FROM "Scholarships" WHERE "Id" = ANY($1::int[]);', [scholarships]);
    }
    if (fields.length) {
      await this.db.query('DELETE FROM "StudentCustomFieldValues" WHERE "CustomFieldDefinitionId" = ANY($1::int[]);', [fields]);
      await this.db.query('DELETE FROM "CustomFieldDefinitions" WHERE "Id" = ANY($1::int[]);', [fields]);
    }
  }

  async cleanupScenario(_careerId: number, _userIds: number[]): Promise<void> {
    // La limpieza principal se realiza por API en cleanup.ts. El entorno E2E es una base
    // PostgreSQL descartable que se elimina con `e2e:reset`; evitar SQL de borrado masivo
    // mantiene este fallback independiente de detalles internos de cada módulo.
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
      await new Promise(resolve => setTimeout(resolve, 2000));
    }
  }
  throw lastError;
}
