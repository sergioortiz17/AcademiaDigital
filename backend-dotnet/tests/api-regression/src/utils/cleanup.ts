import type { ApiClients } from '../fixtures/api.fixture';
import { E2eDatabase } from '../db/e2e-database';
import { env } from '../config/environment';

type Identified = { id: number };

function identifiedList(value: unknown): Identified[] {
  return Array.isArray(value)
    ? value.filter((item): item is Identified => typeof item === 'object' && item !== null && typeof (item as Identified).id === 'number')
    : [];
}

async function bestEffort(operation: () => Promise<unknown>): Promise<void> {
  try {
    await operation();
  } catch {
    // The SQL fallback below is the final cleanup mechanism for the isolated E2E database.
  }
}

export async function cleanupScenario(
  api: ApiClients,
  db: E2eDatabase,
  careerId: number,
  userIds: number[],
  enrollmentPeriodIds: number[] = []
): Promise<void> {
  if (env.preserveData) return;

  if (!careerId) {
    for (const userId of userIds) await db.deleteUser(userId);
    return;
  }

  const studentIds = (await Promise.all(userIds.map((userId) => db.findStudentIdByUserId(userId))))
    .filter((id): id is number => id !== null);

  for (const periodId of [...enrollmentPeriodIds].reverse()) {
    for (const studentId of [...studentIds].reverse()) {
      await bestEffort(() => api.enrollments.removeStudent(periodId, studentId));
    }
    await bestEffort(() => api.enrollments.deletePeriod(periodId));
  }

  for (const studentId of [...studentIds].reverse()) {
    await bestEffort(() => api.students.remove(studentId));
  }

  const plans = await api.academic.listStudyPlans(careerId).catch(() => undefined);
  for (const plan of identifiedList(plans?.body).reverse()) {
    const planCourses = await api.academic.listStudyPlanCourses(plan.id).catch(() => undefined);
    for (const planCourse of identifiedList(planCourses?.body).reverse()) {
      await bestEffort(() => api.academic.deleteStudyPlanCourse(plan.id, planCourse.id));
    }
  }

  const commissions = await api.academic.listCommissions(careerId).catch(() => undefined);
  for (const commission of identifiedList(commissions?.body).reverse()) {
    await bestEffort(() => api.academic.deleteCommission(careerId, commission.id));
  }

  const courses = await api.academic.listCourses(careerId).catch(() => undefined);
  for (const course of identifiedList(courses?.body).reverse()) {
    await bestEffort(() => api.academic.deleteCourse(careerId, course.id));
  }
  await bestEffort(() => api.academic.deleteCareer(careerId));

  await db.cleanupScenario(careerId, userIds);
}
