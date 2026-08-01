import { expect } from '@playwright/test';
import { ApiClients } from '../../src/fixtures/api.fixture';
import {
  careerSchema,
  commissionSchema,
  courseSchema,
  studyPlanCourseSchema,
  studyPlanSchema
} from '../../src/contracts/schemas';
import { academicData, studyPlanCourseData } from '../../src/factories/data.factory';

export async function createAcademicScenario(api: ApiClients) {
  const data = academicData();
  const careerCall = await api.academic.createCareer(data.career);
  expect(careerCall.response.status()).toBe(201);
  const career = careerSchema.parse(careerCall.body);

  const introCall = await api.academic.createCourse(career.id, data.introCourse);
  expect(introCall.response.status()).toBe(201);
  const introCourse = courseSchema.parse(introCall.body);

  const advancedCall = await api.academic.createCourse(career.id, data.advancedCourse);
  expect(advancedCall.response.status()).toBe(201);
  const advancedCourse = courseSchema.parse(advancedCall.body);

  const planCall = await api.academic.createStudyPlan(career.id, data.studyPlan);
  expect(planCall.response.status()).toBe(201);
  const studyPlan = studyPlanSchema.parse(planCall.body);

  const introSpcCall = await api.academic.addStudyPlanCourse(studyPlan.id, studyPlanCourseData(introCourse.id, 1));
  expect(introSpcCall.response.status()).toBe(201);
  const introStudyPlanCourse = studyPlanCourseSchema.parse(introSpcCall.body);

  const advancedSpcCall = await api.academic.addStudyPlanCourse(studyPlan.id, studyPlanCourseData(advancedCourse.id, 2));
  expect(advancedSpcCall.response.status()).toBe(201);
  const advancedStudyPlanCourse = studyPlanCourseSchema.parse(advancedSpcCall.body);

  const activate = await api.academic.activateStudyPlan(career.id, studyPlan.id);
  expect(activate.response.status()).toBe(204);

  const commissionCall = await api.academic.createCommission(career.id, data.commission);
  expect(commissionCall.response.status()).toBe(201);
  const commission = commissionSchema.parse(commissionCall.body);

  return { data, career, introCourse, advancedCourse, studyPlan, introStudyPlanCourse, advancedStudyPlanCourse, commission };
}
