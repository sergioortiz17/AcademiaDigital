import { randomUUID } from 'node:crypto';
import { env } from '../config/environment';

function token(length = 8): string {
  return randomUUID().replace(/-/g, '').slice(0, length).toUpperCase();
}

export function runToken(): string {
  return `${Date.now().toString(36).slice(-5)}${token(3)}`.toUpperCase();
}

export function academicData(suffix = runToken()) {
  const short = `${env.dataPrefix.slice(0, 5)}${suffix}`.slice(0, 12);
  return {
    suffix,
    career: {
      name: `${env.dataPrefix} Carrera ${suffix}`,
      code: `C${short}`.slice(0, 20),
      description: 'Carrera creada por Playwright API regression',
      totalCredits: 160,
      durationYears: 3
    },
    introCourse: {
      code: `I${short}`.slice(0, 20),
      name: `Introducción ${suffix}`,
      description: 'Curso inicial E2E',
      isActive: true
    },
    advancedCourse: {
      code: `A${short}`.slice(0, 20),
      name: `Avanzado ${suffix}`,
      description: 'Curso avanzado E2E',
      isActive: true
    },
    studyPlan: {
      code: `P${short}`.slice(0, 20),
      name: `Plan ${suffix}`,
      versionNumber: Number.parseInt(suffix.slice(-3), 36) % 100_000 + 1,
      effectiveFrom: '2026-03-01',
      effectiveTo: null
    },
    commission: {
      code: `M${short}`.slice(0, 20),
      name: `Comisión ${suffix}`,
      academicYear: 2026,
      yearNumber: 1,
      shift: 'Morning'
    }
  };
}

export function studentData(userId: number, careerId: number, suffix = runToken()) {
  return {
    userId,
    careerId,
    legajoNumber: `${env.dataPrefix}-${suffix}`.slice(0, 50),
    enrollmentDate: '2026-03-01T00:00:00Z',
    status: 'Regular',
    addressLine: 'Calle Automation 123',
    city: 'Córdoba',
    province: 'Córdoba',
    postalCode: '5000',
    emergencyContactName: 'Contacto E2E',
    emergencyContactRelationship: 'Tutor',
    emergencyContactPhone: '3515550101'
  };
}

export function registeredStudentData(careerId: number, suffix = runToken()) {
  const numeric = Number.parseInt(suffix.replace(/[^0-9A-Z]/g, '').slice(-6), 36) % 90_000_000 + 10_000_000;
  return {
    name: `Alumno${suffix}`,
    lastname: 'Playwright',
    email: `${env.dataPrefix.toLowerCase()}.${suffix.toLowerCase()}@e2e.local`,
    password: `Pw_${suffix}_Aa1!`,
    DNI: String(numeric).slice(0, 8),
    careerId
  };
}

export function studyPlanCourseData(courseId: number, semester = 1) {
  return {
    courseId,
    yearNumber: 1,
    semester,
    courseTypeId: null,
    isMandatory: true,
    sortOrder: semester,
    credits: 8,
    workloadHours: 96,
    approvalRule: {
      minimumRegularGrade: 4,
      minimumPromotionGrade: 7,
      minimumAttendancePercentage: 75,
      requiresFinalExam: true,
      allowsPromotion: true,
      policyJson: null
    }
  };
}

export function documentRequirementData(careerId: number | null, suffix = runToken()) {
  return {
    code: `DOC-${suffix}`.slice(0, 30),
    name: `Documento ${suffix}`,
    description: 'Requisito documental creado por Playwright',
    careerId,
    isRequired: true,
    validFrom: '2026-01-01',
    validTo: null
  };
}

export function studentDocumentData(documentRequirementId: number, suffix = runToken()) {
  return {
    documentRequirementId,
    fileUrl: `https://files.example.edu/e2e/${suffix.toLowerCase()}.pdf`,
    originalFileName: `documento-${suffix.toLowerCase()}.pdf`,
    contentType: 'application/pdf',
    fileSizeBytes: 245760
  };
}

export function scholarshipData(suffix = runToken()) {
  return {
    code: `SCH-${suffix}`.slice(0, 30),
    name: `Beca ${suffix}`,
    description: 'Beca creada por Playwright API regression'
  };
}

export function customFieldData(
  dataType: 'Text' | 'Number' | 'Date' | 'Boolean' | 'Select',
  suffix = runToken(),
  overrides: Partial<{ isRequired: boolean; options: string[] | null; sortOrder: number }> = {}
) {
  const key = `${dataType.toLowerCase()}_${suffix.toLowerCase()}`.replace(/[^a-z0-9_]/g, '').slice(0, 100);
  return {
    key,
    label: `${dataType} ${suffix}`,
    dataType,
    isRequired: false,
    options: dataType === 'Select' ? ['Español', 'Inglés'] : null,
    sortOrder: 10,
    ...overrides
  };
}
