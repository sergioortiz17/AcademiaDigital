import { z } from 'zod';

export const problemSchema = z.object({
  success: z.boolean().optional(),
  msg: z.string().optional(),
  title: z.string().optional(),
  detail: z.string().optional(),
  status: z.number().optional()
}).passthrough();

export const loginSchema = z.object({
  success: z.literal(true),
  token: z.string().min(1),
  user: z.object({
    _id: z.number().int().positive(),
    username: z.string(),
    email: z.string().email(),
    role: z.number().int()
  })
});

export const careerSchema = z.object({
  id: z.number().int().positive(),
  name: z.string(),
  code: z.string(),
  description: z.string().nullable(),
  totalCredits: z.number().int().nonnegative(),
  durationYears: z.number().int().positive(),
  isActive: z.boolean()
}).passthrough();

export const courseSchema = z.object({
  id: z.number().int().positive(),
  careerId: z.number().int().positive(),
  code: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  isActive: z.boolean()
});

export const studyPlanSchema = z.object({
  id: z.number().int().positive(),
  careerId: z.number().int().positive(),
  code: z.string(),
  name: z.string(),
  versionNumber: z.number().int().positive(),
  status: z.enum(['Draft', 'Active', 'Archived']),
  effectiveFrom: z.string().nullable(),
  effectiveTo: z.string().nullable(),
  isActive: z.boolean()
});

export const studyPlanCourseSchema = z.object({
  id: z.number().int().positive(),
  studyPlanId: z.number().int().positive(),
  courseId: z.number().int().positive(),
  courseCode: z.string(),
  courseName: z.string(),
  yearNumber: z.number().int().positive(),
  semester: z.number().int(),
  isAnnual: z.boolean(),
  sortOrder: z.number().int(),
  isMandatory: z.boolean(),
  credits: z.number().nullable(),
  workloadHours: z.number().int().nullable(),
  courseType: z.string().nullable()
});

export const commissionSchema = z.object({
  id: z.number().int().positive(),
  careerId: z.number().int().positive(),
  code: z.string(),
  name: z.string(),
  academicYear: z.number().int(),
  yearNumber: z.number().int(),
  shift: z.enum(['Morning', 'Afternoon', 'Evening']),
  isActive: z.boolean()
});

export const studentSummarySchema = z.object({
  id: z.number().int().positive(),
  userId: z.number().int().positive(),
  userEmail: z.string().email(),
  userName: z.string(),
  careerId: z.number().int().positive(),
  careerName: z.string(),
  legajoNumber: z.string(),
  enrollmentDate: z.string(),
  status: z.enum(['Regular', 'Libre', 'Graduated', 'Withdrawn']),
  currentStudyPlanId: z.number().int().positive().nullable(),
  currentStudyPlanName: z.string().nullable(),
  careers: z.array(z.object({
    id: z.number().int().positive(),
    careerId: z.number().int().positive(),
    careerName: z.string(),
    enrollmentDate: z.string(),
    isActive: z.boolean(),
    isPrimary: z.boolean(),
    currentStudyPlanId: z.number().int().positive().nullable(),
    currentStudyPlanName: z.string().nullable()
  }))
});

export const studentCareerSchema = studentSummarySchema.shape.careers.element;

export const studentListItemSchema = z.object({
  id: z.number().int().positive(),
  userId: z.number().int().positive(),
  dni: z.string().nullable(),
  fullName: z.string(),
  legajoNumber: z.string(),
  status: z.enum(['Regular', 'Libre', 'Graduated', 'Withdrawn']),
  careerId: z.number().int().positive(),
  careerName: z.string(),
  academicYear: z.number().int().nullable(),
  yearNumber: z.number().int().nullable(),
  commissionId: z.number().int().positive().nullable(),
  commissionName: z.string().nullable()
});

export const pagedStudentsSchema = z.object({
  items: z.array(studentListItemSchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  total: z.number().int().nonnegative()
});

export const academicAssignmentSchema = z.object({
  id: z.number().int().positive(),
  studentId: z.number().int().positive(),
  careerId: z.number().int().positive(),
  studyPlanId: z.number().int().positive(),
  commissionId: z.number().int().positive().nullable(),
  commissionName: z.string().nullable(),
  academicYear: z.number().int(),
  yearNumber: z.number().int(),
  startedAt: z.string(),
  endedAt: z.string().nullable(),
  isCurrent: z.boolean(),
  reason: z.string().nullable()
});

export const enrollmentPeriodSchema = z.object({
  id: z.number().int().positive(),
  careerId: z.number().int().positive(),
  careerName: z.string(),
  studyPlanId: z.number().int().positive(),
  studyPlanName: z.string(),
  academicYear: z.number().int(),
  semester: z.number().int(),
  quotasMorning: z.number().int().nonnegative(),
  quotasAfternoon: z.number().int().nonnegative(),
  quotasEvening: z.number().int().nonnegative(),
  enrolledMorning: z.number().int().nonnegative(),
  enrolledAfternoon: z.number().int().nonnegative(),
  enrolledEvening: z.number().int().nonnegative(),
  isActive: z.boolean()
}).passthrough();

export const documentRequirementSchema = z.object({
  id: z.number().int().positive(),
  code: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  careerId: z.number().int().positive().nullable(),
  isRequired: z.boolean(),
  isActive: z.boolean(),
  validFrom: z.string().nullable(),
  validTo: z.string().nullable()
});

export const studentDocumentSchema = z.object({
  id: z.number().int().positive(),
  studentId: z.number().int().positive(),
  documentRequirementId: z.number().int().positive(),
  requirementName: z.string(),
  fileUrl: z.string(),
  originalFileName: z.string(),
  contentType: z.enum(['application/pdf', 'image/jpeg', 'image/png']),
  fileSizeBytes: z.number().int().positive(),
  status: z.enum(['Submitted', 'Approved', 'Rejected', 'Expired']),
  submittedAt: z.string(),
  reviewedAt: z.string().nullable(),
  observation: z.string().nullable()
});

export const scholarshipSchema = z.object({
  id: z.number().int().positive(),
  code: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  isActive: z.boolean()
});

export const studentScholarshipSchema = z.object({
  id: z.number().int().positive(),
  studentId: z.number().int().positive(),
  scholarshipId: z.number().int().positive(),
  scholarshipName: z.string(),
  academicYear: z.number().int(),
  status: z.enum(['Requested', 'Granted', 'Rejected', 'Revoked', 'Expired']),
  grantedAt: z.string().nullable(),
  validFrom: z.string().nullable(),
  validTo: z.string().nullable(),
  notes: z.string().nullable()
});

export const customFieldDefinitionSchema = z.object({
  id: z.number().int().positive(),
  key: z.string(),
  label: z.string(),
  dataType: z.enum(['Text', 'Number', 'Date', 'Boolean', 'Select']),
  isRequired: z.boolean(),
  options: z.array(z.string()).nullable(),
  isActive: z.boolean(),
  sortOrder: z.number().int()
});

export const customValuesSchema = z.record(z.string(), z.unknown());

export const studentRecordSchema = z.object({
  student: z.unknown(),
  personalData: z.unknown(),
  address: z.unknown(),
  emergencyContact: z.unknown(),
  currentAcademicAssignment: z.unknown().nullable(),
  documentsSummary: z.unknown(),
  activeScholarships: z.array(studentScholarshipSchema),
  customFields: customValuesSchema
});

export type LoginResponse = z.infer<typeof loginSchema>;
export type Career = z.infer<typeof careerSchema>;
export type Course = z.infer<typeof courseSchema>;
export type StudyPlan = z.infer<typeof studyPlanSchema>;
export type StudyPlanCourse = z.infer<typeof studyPlanCourseSchema>;
export type Commission = z.infer<typeof commissionSchema>;
export type StudentSummary = z.infer<typeof studentSummarySchema>;
export type AcademicAssignment = z.infer<typeof academicAssignmentSchema>;
export type EnrollmentPeriod = z.infer<typeof enrollmentPeriodSchema>;
export type DocumentRequirement = z.infer<typeof documentRequirementSchema>;
export type StudentDocument = z.infer<typeof studentDocumentSchema>;
export type Scholarship = z.infer<typeof scholarshipSchema>;
export type StudentScholarship = z.infer<typeof studentScholarshipSchema>;
export type CustomFieldDefinition = z.infer<typeof customFieldDefinitionSchema>;
