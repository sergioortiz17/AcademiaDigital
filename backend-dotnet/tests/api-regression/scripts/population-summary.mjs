import 'dotenv/config';
import sql from 'mssql';

const pool = await sql.connect(process.env.E2E_SQL_CONNECTION_STRING);
try {
  const result = await pool.request()
    .input('code', sql.NVarChar, `C${(process.env.E2E_DATA_PREFIX ?? 'PWAPI').slice(0, 5)}%`)
    .input('email', sql.NVarChar, `${(process.env.E2E_DATA_PREFIX ?? 'PWAPI').toLowerCase()}.%@e2e.local`)
    .query(`
      SELECT
        (SELECT COUNT(*) FROM Careers WHERE code LIKE @code) AS careers,
        (SELECT COUNT(*) FROM Courses c JOIN Careers ca ON ca.id = c.career_id WHERE ca.code LIKE @code) AS courses,
        (SELECT COUNT(*) FROM StudyPlans sp JOIN Careers ca ON ca.id = sp.career_id WHERE ca.code LIKE @code) AS studyPlans,
        (SELECT COUNT(*) FROM Commissions co JOIN Careers ca ON ca.id = co.CareerId WHERE ca.code LIKE @code) AS commissions,
        (SELECT COUNT(*) FROM Users WHERE email LIKE @email) AS users,
        (SELECT COUNT(*) FROM Students s JOIN Users u ON u.id = s.user_id WHERE u.email LIKE @email) AS students,
        (SELECT COUNT(*) FROM StudentCareers sc JOIN Students s ON s.id = sc.StudentId JOIN Users u ON u.id = s.user_id WHERE u.email LIKE @email) AS studentCareers,
        (SELECT COUNT(*) FROM Enrollments e JOIN Students s ON s.id = e.student_id JOIN Users u ON u.id = s.user_id WHERE u.email LIKE @email) AS enrollments;
    `);
  process.stdout.write(`${JSON.stringify(result.recordset[0], null, 2)}\n`);
} finally {
  await pool.close();
}
