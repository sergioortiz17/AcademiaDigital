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

export const admissionFormSchema = z.object({
  id: z.number().int().positive(),
  slug: z.string().min(3),
  title: z.string().min(1),
  description: z.string().nullable(),
  termsText: z.string().min(1),
  careerId: z.number().int().positive(),
  careerName: z.string().min(1),
  commissionId: z.number().int().positive().nullable(),
  commissionCode: z.string().nullable(),
  commissionName: z.string().nullable(),
  academicYear: z.number().int().nullable(),
  yearNumber: z.number().int().nullable(),
  shift: z.enum(['Morning', 'Afternoon', 'Evening']).nullable(),
  reservationHours: z.number().int().positive(),
  capacity: z.number().int().positive().nullable(),
  isActive: z.boolean(),
  fields: z.array(z.object({
    key: z.string().min(1),
    label: z.string().min(1),
    type: z.enum(['Text', 'Email', 'Phone', 'Date', 'Checkbox']),
    isRequired: z.boolean(),
    sortOrder: z.number().int().nonnegative()
  }))
});

export const admissionApplicationSchema = z.object({
  publicId: z.string().uuid(),
  status: z.enum(['PreEnrolled', 'Enrolled', 'Confirmed', 'Waitlisted', 'Expired', 'Rejected']),
  reservationExpiresAt: z.string().datetime().nullable(),
  createdAt: z.string().datetime()
});

export const admissionApplicationSummarySchema = z.object({
  publicId: z.string().uuid(),
  admissionFormId: z.number().int().positive(),
  formSlug: z.string().min(3),
  formTitle: z.string().min(1),
  applicantEmail: z.string().email(),
  applicantDni: z.string().min(1),
  status: z.enum(['PreEnrolled', 'Enrolled', 'Confirmed', 'Waitlisted', 'Expired', 'Rejected']),
  reservationExpiresAt: z.string().datetime({ local: true }).nullable(),
  createdAt: z.string().datetime({ local: true }),
  updatedAt: z.string().datetime({ local: true })
});

export const admissionApplicationDetailSchema = z.object({
  application: admissionApplicationSummarySchema,
  fields: z.record(z.string(), z.string()),
  history: z.array(z.object({
    fromStatus: z.enum(['PreEnrolled', 'Enrolled', 'Confirmed', 'Waitlisted', 'Expired', 'Rejected']).nullable(),
    toStatus: z.enum(['PreEnrolled', 'Enrolled', 'Confirmed', 'Waitlisted', 'Expired', 'Rejected']),
    changedAt: z.string().datetime({ local: true }),
    changedByUserId: z.number().int().positive().nullable(),
    reason: z.string().nullable()
  }))
});

export const admissionApplicationPageSchema = z.object({
  items: z.array(admissionApplicationSummarySchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  total: z.number().int().nonnegative()
});

export const admissionExpirationResultSchema = z.object({
  formsProcessed: z.number().int().nonnegative(),
  expired: z.number().int().nonnegative(),
  promoted: z.number().int().nonnegative()
});

export const admissionApplicationDocumentSchema = z.object({
  id: z.number().int().positive(),
  applicationPublicId: z.string().uuid(),
  documentRequirementId: z.number().int().positive(),
  requirementCode: z.string().min(1),
  requirementName: z.string().min(1),
  fileUrl: z.string().min(1),
  originalFileName: z.string().min(1),
  contentType: z.enum(['application/pdf', 'image/jpeg', 'image/png']),
  fileSizeBytes: z.number().int().positive(),
  status: z.enum(['Submitted', 'Approved', 'Rejected', 'Expired']),
  submittedAt: z.string().datetime({ local: true }),
  reviewedAt: z.string().datetime({ local: true }).nullable(),
  reviewedByUserId: z.number().int().positive().nullable(),
  observation: z.string().nullable()
});

export const admissionAgreementSchema = z.object({
  agreementNumber: z.string().startsWith('ADM-'),
  status: z.enum(['Pending', 'Ready', 'Failed']),
  fileName: z.string().endsWith('.pdf'),
  contentType: z.literal('application/pdf'),
  sha256: z.string().length(64).nullable(),
  createdAt: z.string().datetime({ local: true }),
  generatedAt: z.string().datetime({ local: true }).nullable(),
  downloadPath: z.string().nullable(),
  lastError: z.string().nullable()
});

export const admissionOutboxResultSchema = z.object({
  claimed: z.number().int().nonnegative(),
  processed: z.number().int().nonnegative(),
  failed: z.number().int().nonnegative()
});

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

export const studentRematriculationSchema = z.object({
  id: z.number().int().positive(),
  studentId: z.number().int().positive(),
  studentCareerId: z.number().int().positive(),
  careerId: z.number().int().positive(),
  studyPlanId: z.number().int().positive(),
  studyPlanName: z.string().min(1),
  commissionId: z.number().int().positive(),
  commissionName: z.string().min(1),
  shift: z.enum(['Morning', 'Afternoon', 'Evening']),
  academicYear: z.number().int(),
  yearNumber: z.number().int().positive(),
  rematriculatedAt: z.string().datetime(),
  createdByUserId: z.number().int().positive(),
  notes: z.string().nullable()
});

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

export const teacherSchema = z.object({
  id: z.number().int().positive(),
  userId: z.number().int().positive(),
  employeeNumber: z.string().min(1),
  firstName: z.string().min(1),
  lastName: z.string(),
  email: z.string().email(),
  dni: z.string().nullable(),
  gender: z.string().nullable(),
  birthDate: z.string().datetime({ local: true }).nullable(),
  department: z.string().nullable(),
  specializationArea: z.string().nullable(),
  hireDate: z.string().datetime({ local: true }),
  isActive: z.boolean(),
  phoneNumber: z.string().nullable(),
  addressLine: z.string().nullable(),
  city: z.string().nullable(),
  province: z.string().nullable(),
  postalCode: z.string().nullable(),
  emergencyContactName: z.string().nullable(),
  emergencyContactRelationship: z.string().nullable(),
  emergencyContactPhone: z.string().nullable(),
  deactivatedAt: z.string().datetime({ local: true }).nullable(),
  deactivatedByUserId: z.number().int().positive().nullable(),
  deactivationReason: z.string().nullable()
});

export const teacherDocumentSchema = z.object({
  id: z.number().int().positive(),
  teacherId: z.number().int().positive(),
  documentType: z.string().min(1),
  version: z.number().int().positive(),
  fileUrl: z.string().min(1),
  originalFileName: z.string().min(1),
  contentType: z.enum(['application/pdf', 'image/jpeg', 'image/png']),
  fileSizeBytes: z.number().int().positive(),
  status: z.enum(['Submitted', 'Approved', 'Rejected', 'Expired']),
  submittedAt: z.string().datetime({ local: true }),
  validUntil: z.string().nullable(),
  reviewedAt: z.string().datetime({ local: true }).nullable(),
  reviewedByUserId: z.number().int().positive().nullable(),
  observation: z.string().nullable()
});

export const teachingPositionSchema = z.object({
  id: z.number().int().positive(),
  courseId: z.number().int().positive(),
  courseCode: z.string().min(1),
  courseName: z.string().min(1),
  commissionId: z.number().int().positive().nullable(),
  commissionCode: z.string().nullable(),
  commissionName: z.string().nullable(),
  academicYear: z.number().int(),
  semester: z.number().int(),
  positionType: z.enum(['Titular', 'Adjunct', 'JTP', 'Assistant']),
  maxStudents: z.number().int().positive(),
  isVacant: z.boolean(),
  isActive: z.boolean(),
  teacherId: z.number().int().positive().nullable(),
  teacherName: z.string().nullable(),
  createdAt: z.string().datetime({ local: true }),
  updatedAt: z.string().datetime({ local: true }),
  deactivatedAt: z.string().datetime({ local: true }).nullable(),
  deactivatedByUserId: z.number().int().positive().nullable(),
  deactivationReason: z.string().nullable()
});

export const teacherAssignmentSchema = z.object({
  id: z.number().int().positive(),
  teacherId: z.number().int().positive(),
  teacherName: z.string().min(1),
  teachingPositionId: z.number().int().positive(),
  courseId: z.number().int().positive(),
  courseCode: z.string().min(1),
  courseName: z.string().min(1),
  commissionId: z.number().int().positive().nullable(),
  commissionCode: z.string().nullable(),
  commissionName: z.string().nullable(),
  academicYear: z.number().int(),
  semester: z.number().int(),
  positionType: z.enum(['Titular', 'Adjunct', 'JTP', 'Assistant']),
  maxStudents: z.number().int().positive(),
  startedOn: z.string(),
  endedOn: z.string().nullable(),
  isCurrent: z.boolean(),
  assignmentReason: z.string().nullable(),
  endReason: z.string().nullable(),
  assignedByUserId: z.number().int().positive().nullable(),
  endedByUserId: z.number().int().positive().nullable(),
  createdAt: z.string().datetime({ local: true }),
  endedAt: z.string().datetime({ local: true }).nullable()
});

export const attendanceJustificationSchema = z.object({
  id: z.number().int().positive(),
  category: z.string().min(1),
  reason: z.string().min(1),
  evidenceUrl: z.string().nullable(),
  createdAt: z.string().datetime({ local: true }),
  createdByUserId: z.number().int().positive()
});

export const attendanceRecordSchema = z.object({
  id: z.number().int().positive().nullable(),
  enrollmentId: z.number().int().positive(),
  studentId: z.number().int().positive(),
  studentName: z.string().min(1),
  legajoNumber: z.string().min(1),
  dni: z.string(),
  status: z.enum(['Present', 'Late', 'Absent', 'Justified']).nullable(),
  notes: z.string().nullable(),
  updatedAt: z.string().datetime({ local: true }).nullable(),
  justification: attendanceJustificationSchema.nullable()
});

export const attendanceSessionSchema = z.object({
  id: z.number().int().positive(),
  idempotencyKey: z.string().min(1),
  teachingPositionId: z.number().int().positive(),
  courseId: z.number().int().positive(),
  courseCode: z.string().min(1),
  courseName: z.string().min(1),
  commissionId: z.number().int().positive(),
  commissionCode: z.string().min(1),
  commissionName: z.string().min(1),
  academicYear: z.number().int(),
  semester: z.number().int(),
  sessionDate: z.string(),
  startTime: z.string().nullable(),
  endTime: z.string().nullable(),
  scope: z.enum(['ClassHour', 'FullDay']),
  units: z.number().int().positive(),
  status: z.enum(['Open', 'Closed']),
  editDeadlineUtc: z.string().datetime({ local: true }),
  isAdministrativelyReopened: z.boolean(),
  recordCount: z.number().int().nonnegative(),
  reopeningCount: z.number().int().nonnegative(),
  createdAt: z.string().datetime({ local: true }),
  createdByUserId: z.number().int().positive(),
  closedAt: z.string().datetime({ local: true }).nullable(),
  closedByUserId: z.number().int().positive().nullable()
});

export const attendanceSessionDetailSchema = z.object({
  session: attendanceSessionSchema,
  records: z.array(attendanceRecordSchema)
});

export const attendanceSummarySchema = z.object({
  studentId: z.number().int().positive(),
  studentName: z.string().min(1),
  legajoNumber: z.string().min(1),
  items: z.array(z.object({
    courseId: z.number().int().positive(),
    courseCode: z.string().min(1),
    courseName: z.string().min(1),
    commissionId: z.number().int().positive(),
    commissionCode: z.string().min(1),
    commissionName: z.string().min(1),
    academicYear: z.number().int(),
    semester: z.number().int(),
    minimumAttendancePercentage: z.number().nullable(),
    earnedUnits: z.number().nonnegative(),
    possibleUnits: z.number().nonnegative(),
    attendancePercentage: z.number().nullable(),
    isAtRisk: z.boolean(),
    presentCount: z.number().int().nonnegative(),
    lateCount: z.number().int().nonnegative(),
    absentCount: z.number().int().nonnegative(),
    justifiedCount: z.number().int().nonnegative()
  }))
});

export const gradebookEvaluationSchema = z.object({
  id: z.number().int().positive(),
  name: z.string().min(1),
  weightPercentage: z.number().positive().max(100),
  maximumScore: z.number().positive().max(100),
  displayOrder: z.number().int().positive()
});

export const gradeEntrySchema = z.object({
  revisionId: z.number().int().positive().nullable(),
  evaluationId: z.number().int().positive(),
  score: z.number().nullable(),
  version: z.number().int().positive().nullable(),
  notes: z.string().nullable(),
  updatedAt: z.string().datetime({ local: true }).nullable()
});

export const gradebookSchema = z.object({
  id: z.number().int().positive(),
  idempotencyKey: z.string().min(1),
  teachingPositionId: z.number().int().positive(),
  courseId: z.number().int().positive(),
  courseCode: z.string().min(1),
  courseName: z.string().min(1),
  commissionId: z.number().int().positive(),
  commissionCode: z.string().min(1),
  commissionName: z.string().min(1),
  academicYear: z.number().int(),
  semester: z.number().int(),
  status: z.enum(['Draft', 'Submitted', 'Approved', 'Published', 'Closed']),
  evaluationCount: z.number().int().nonnegative(),
  currentGradeCount: z.number().int().nonnegative(),
  reopeningCount: z.number().int().nonnegative(),
  createdAt: z.string().datetime({ local: true }),
  submittedAt: z.string().datetime({ local: true }).nullable(),
  approvedAt: z.string().datetime({ local: true }).nullable(),
  publishedAt: z.string().datetime({ local: true }).nullable(),
  closedAt: z.string().datetime({ local: true }).nullable()
});

export const gradebookDetailSchema = z.object({
  gradebook: gradebookSchema,
  evaluations: z.array(gradebookEvaluationSchema),
  students: z.array(z.object({
    enrollmentId: z.number().int().positive(),
    studentId: z.number().int().positive(),
    studentName: z.string().min(1),
    legajoNumber: z.string().min(1),
    dni: z.string(),
    grades: z.array(gradeEntrySchema),
    average: z.number().nullable(),
    resultStatus: z.enum(['Enrolled', 'Regularized', 'Approved', 'Promoted', 'Failed', 'Withdrawn']).nullable()
  }))
});

export const publishedGradebookSchema = z.object({
  gradebookId: z.number().int().positive(),
  courseId: z.number().int().positive(),
  courseCode: z.string().min(1),
  courseName: z.string().min(1),
  academicYear: z.number().int(),
  semester: z.number().int(),
  status: z.enum(['Published', 'Closed']),
  evaluations: z.array(gradebookEvaluationSchema),
  grades: z.array(gradeEntrySchema),
  average: z.number(),
  resultStatus: z.enum(['Enrolled', 'Regularized', 'Approved', 'Promoted', 'Failed', 'Withdrawn']),
  publishedAt: z.string().datetime({ local: true })
});

export const examGradeSchema = z.object({
  revisionId: z.number().int().positive(),
  version: z.number().int().positive(),
  outcome: z.enum(['Passed', 'Failed', 'Absent']),
  grade: z.number().nullable(),
  notes: z.string().nullable(),
  createdAt: z.string().datetime({ local: true })
});

export const examTableSchema = z.object({
  id: z.number().int().positive(),
  idempotencyKey: z.string().min(1),
  courseId: z.number().int().positive(),
  courseCode: z.string().min(1),
  courseName: z.string().min(1),
  academicYear: z.number().int(),
  callNumber: z.number().int().positive(),
  examDateUtc: z.string().datetime({ local: true }),
  registrationDeadlineUtc: z.string().datetime({ local: true }),
  location: z.string().min(1),
  status: z.enum(['Open', 'Grading', 'Published']),
  tribunalCount: z.number().int().nonnegative(),
  registrationCount: z.number().int().nonnegative(),
  reopeningCount: z.number().int().nonnegative(),
  createdAt: z.string().datetime({ local: true }),
  gradingStartedAt: z.string().datetime({ local: true }).nullable(),
  publishedAt: z.string().datetime({ local: true }).nullable()
});

export const examRegistrationSchema = z.object({
  id: z.number().int().positive(),
  enrollmentId: z.number().int().positive(),
  studentId: z.number().int().positive(),
  studentName: z.string().min(1),
  legajoNumber: z.string().min(1),
  attemptNumber: z.number().int().positive(),
  registeredAt: z.string().datetime({ local: true }),
  result: examGradeSchema.nullable()
});

export const examTableDetailSchema = z.object({
  examTable: examTableSchema,
  tribunal: z.array(z.object({
    teacherId: z.number().int().positive(),
    employeeNumber: z.string().min(1),
    teacherName: z.string().min(1),
    role: z.enum(['President', 'Vocal'])
  })),
  registrations: z.array(examRegistrationSchema)
});

export const studentExamTableSchema = z.object({
  examTable: examTableSchema,
  canRegister: z.boolean(),
  registrationId: z.number().int().positive().nullable(),
  attemptNumber: z.number().int().positive().nullable(),
  result: examGradeSchema.nullable()
});

export const certificateIssuanceSchema = z.object({
  id: z.string().uuid(),
  certificateNumber: z.string().regex(/^CERT-\d{8}$/),
  certificateType: z.string().min(1),
  status: z.enum(['Generating', 'Ready', 'Failed']),
  fileName: z.string().endsWith('.pdf'),
  sha256: z.string().length(64).nullable(),
  createdAt: z.string().datetime({ local: true }),
  generatedAt: z.string().datetime({ local: true }).nullable(),
  downloadPath: z.string().nullable(),
  lastError: z.string().nullable(),
  userId: z.number().int().nonnegative(),
  username: z.string().nullable().optional(),
  email: z.string().email().nullable().optional()
});

export const certificateRequestSchema = z.object({
  id: z.number().int().positive(),
  userId: z.number().int().positive(),
  username: z.string().nullable().optional(),
  email: z.string().email().nullable().optional(),
  certificateType: z.string().min(1),
  status: z.enum(['Pending', 'Approved', 'Rejected']),
  createdAt: z.string().datetime({ local: true }),
  updatedAt: z.string().datetime({ local: true }).nullable(),
  kind: z.enum(['RegularStudent', 'Enrollment', 'ApprovedCourses', 'AcademicStatus', 'Transcript', 'GeneralAcademicStatus', 'ExamPermit']),
  studentCareerId: z.number().int().positive().nullable(),
  examRegistrationId: z.number().int().positive().nullable(),
  reviewedAt: z.string().datetime({ local: true }).nullable(),
  reviewedByUserId: z.number().int().positive().nullable(),
  rejectionReason: z.string().nullable(),
  issuance: certificateIssuanceSchema.nullable()
});

export const certificateRequestsResponseSchema = z.object({
  success: z.literal(true),
  requests: z.array(certificateRequestSchema)
});

export const certificateRequestResponseSchema = z.object({
  success: z.literal(true),
  request: certificateRequestSchema
});

export const financialConceptSchema = z.object({
  id: z.number().int().positive(),
  code: z.string().min(2).max(30),
  name: z.string().min(1),
  description: z.string().nullable(),
  isActive: z.boolean()
});

export const financialRateSchema = z.object({
  id: z.number().int().positive(),
  conceptId: z.number().int().positive(),
  careerId: z.number().int().positive(),
  academicYear: z.number().int(),
  studentCondition: z.enum(['Regular', 'Libre', 'Graduated', 'Withdrawn']).nullable(),
  amount: z.number().positive(),
  surchargePercentage: z.number().min(0).max(100),
  isActive: z.boolean()
});

export const financialBenefitSchema = z.object({
  id: z.number().int().positive(),
  code: z.string().min(2).max(30),
  name: z.string().min(1),
  kind: z.enum(['Discount', 'Scholarship']),
  scholarshipId: z.number().int().positive().nullable(),
  careerId: z.number().int().positive().nullable(),
  studentCondition: z.enum(['Regular', 'Libre', 'Graduated', 'Withdrawn']).nullable(),
  percentage: z.number().positive().max(100),
  validFrom: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).nullable(),
  validTo: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).nullable(),
  isActive: z.boolean()
});

export const billingPlanItemSchema = z.object({
  id: z.number().int().positive(),
  conceptId: z.number().int().positive(),
  installmentNumber: z.number().int().positive(),
  dueDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/)
});

export const billingPlanSchema = z.object({
  id: z.number().int().positive(),
  name: z.string().min(1),
  careerId: z.number().int().positive(),
  academicYear: z.number().int(),
  currency: z.literal('ARS'),
  isActive: z.boolean(),
  items: z.array(billingPlanItemSchema).min(1)
});

export const studentDebtSchema = z.object({
  publicId: z.string().uuid(),
  studentId: z.number().int().positive(),
  studentCareerId: z.number().int().positive(),
  studentName: z.string().min(1),
  dni: z.string().min(1),
  legajoNumber: z.string().min(1),
  careerName: z.string().min(1),
  conceptCode: z.string().min(1),
  conceptName: z.string().min(1),
  installmentNumber: z.number().int().positive(),
  dueDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  currency: z.literal('ARS'),
  baseAmount: z.number().positive(),
  surchargeAmount: z.number().nonnegative(),
  discountAmount: z.number().nonnegative(),
  totalAmount: z.number().nonnegative(),
  paidAmount: z.number().nonnegative(),
  outstandingAmount: z.number().nonnegative(),
  status: z.enum(['Pending', 'PartiallyPaid', 'Paid', 'Cancelled']),
  appliedBenefitCode: z.string().nullable(),
  appliedBenefitName: z.string().nullable(),
  createdAt: z.string().datetime({ local: true })
});

export const debtGenerationResultSchema = z.object({
  batchPublicId: z.string().uuid(),
  idempotencyKey: z.string().min(8),
  billingPlanId: z.number().int().positive(),
  generatedDebtCount: z.number().int().positive(),
  generatedTotal: z.number().nonnegative(),
  generatedAt: z.string().datetime({ local: true }),
  debts: z.array(studentDebtSchema).min(1)
});

export const paymentMethodSchema = z.object({
  id: z.number().int().positive(),
  code: z.enum(['CASH', 'BANK_TRANSFER', 'DEBIT_CARD', 'CREDIT_CARD']),
  name: z.string().min(1),
  kind: z.enum(['Cash', 'BankTransfer', 'DebitCard', 'CreditCard']),
  requiresReconciliation: z.boolean()
});

export const paymentAllocationSchema = z.object({
  debtPublicId: z.string().uuid(),
  amount: z.number().positive(),
  debtTotal: z.number().nonnegative(),
  debtPaid: z.number().nonnegative(),
  debtOutstanding: z.number().nonnegative(),
  debtStatus: z.enum(['Pending', 'PartiallyPaid', 'Paid', 'Cancelled'])
});

export const receiptSchema = z.object({
  publicId: z.string().uuid(),
  receiptNumber: z.string().regex(/^REC-\d{8}$/),
  paymentPublicId: z.string().uuid(),
  paymentStatus: z.enum(['Draft', 'PendingReconciliation', 'Confirmed', 'Rejected', 'Reversed']),
  studentId: z.number().int().positive(),
  studentName: z.string().min(1),
  dni: z.string().min(7),
  paymentMethodCode: z.enum(['CASH', 'BANK_TRANSFER', 'DEBIT_CARD', 'CREDIT_CARD']),
  paymentMethodName: z.string().min(1),
  currency: z.literal('ARS'),
  amount: z.number().positive(),
  issuedAt: z.string().datetime({ local: true }),
  operatorUserId: z.number().int().positive(),
  operatorName: z.string().min(1),
  items: z.array(z.object({
    debtPublicId: z.string().uuid(),
    conceptCode: z.string().min(1),
    conceptName: z.string().min(1),
    amount: z.number().positive()
  })).min(1),
  status: z.enum(['Generating', 'Ready', 'Failed']),
  fileName: z.string().endsWith('.pdf'),
  sha256: z.string().length(64).nullable(),
  generatedAt: z.string().datetime({ local: true }).nullable(),
  lastError: z.string().nullable(),
  fiscalCae: z.string().nullable(),
  fiscalQrData: z.string().nullable(),
  downloadPath: z.string().nullable()
});

export const paymentSchema = z.object({
  publicId: z.string().uuid(),
  studentId: z.number().int().positive(),
  studentName: z.string().min(1),
  studentDni: z.string().min(7),
  method: paymentMethodSchema,
  currency: z.literal('ARS'),
  amount: z.number().positive(),
  status: z.enum(['Draft', 'PendingReconciliation', 'Confirmed', 'Rejected', 'Reversed']),
  externalReference: z.string().nullable(),
  notes: z.string().nullable(),
  createdAt: z.string().datetime({ local: true }),
  createdByUserId: z.number().int().positive(),
  confirmationRequestedAt: z.string().datetime({ local: true }).nullable(),
  confirmationRequestedByUserId: z.number().int().positive().nullable(),
  confirmedAt: z.string().datetime({ local: true }).nullable(),
  confirmedByUserId: z.number().int().positive().nullable(),
  allocations: z.array(paymentAllocationSchema).min(1),
  reconciliations: z.array(z.object({
    decision: z.enum(['Approve', 'Reject']),
    note: z.string().nullable(),
    createdAt: z.string().datetime({ local: true }),
    createdByUserId: z.number().int().positive()
  })),
  reversals: z.array(z.object({
    publicId: z.string().uuid(),
    amount: z.number().positive(),
    reason: z.string().min(5),
    createdAt: z.string().datetime({ local: true }),
    createdByUserId: z.number().int().positive()
  })),
  receipt: receiptSchema.nullable().optional()
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
export type AdmissionForm = z.infer<typeof admissionFormSchema>;
export type AdmissionApplication = z.infer<typeof admissionApplicationSchema>;
export type AdmissionApplicationDocument = z.infer<typeof admissionApplicationDocumentSchema>;
export type AdmissionAgreement = z.infer<typeof admissionAgreementSchema>;
export type StudentRematriculation = z.infer<typeof studentRematriculationSchema>;
export type DocumentRequirement = z.infer<typeof documentRequirementSchema>;
export type StudentDocument = z.infer<typeof studentDocumentSchema>;
export type Scholarship = z.infer<typeof scholarshipSchema>;
export type StudentScholarship = z.infer<typeof studentScholarshipSchema>;
export type CustomFieldDefinition = z.infer<typeof customFieldDefinitionSchema>;
export type Teacher = z.infer<typeof teacherSchema>;
export type TeacherDocument = z.infer<typeof teacherDocumentSchema>;
export type TeachingPosition = z.infer<typeof teachingPositionSchema>;
export type TeacherAssignment = z.infer<typeof teacherAssignmentSchema>;
export type AttendanceSession = z.infer<typeof attendanceSessionSchema>;
export type AttendanceSessionDetail = z.infer<typeof attendanceSessionDetailSchema>;
export type AttendanceSummary = z.infer<typeof attendanceSummarySchema>;
export type Gradebook = z.infer<typeof gradebookSchema>;
export type GradebookDetail = z.infer<typeof gradebookDetailSchema>;
export type ExamTable = z.infer<typeof examTableSchema>;
export type ExamTableDetail = z.infer<typeof examTableDetailSchema>;
export type CertificateRequest = z.infer<typeof certificateRequestSchema>;
export type CertificateIssuance = z.infer<typeof certificateIssuanceSchema>;
export type FinancialConcept = z.infer<typeof financialConceptSchema>;
export type FinancialRate = z.infer<typeof financialRateSchema>;
export type FinancialBenefit = z.infer<typeof financialBenefitSchema>;
export type BillingPlan = z.infer<typeof billingPlanSchema>;
export type StudentDebt = z.infer<typeof studentDebtSchema>;
export type PaymentMethod = z.infer<typeof paymentMethodSchema>;
export type Payment = z.infer<typeof paymentSchema>;
export type Receipt = z.infer<typeof receiptSchema>;
