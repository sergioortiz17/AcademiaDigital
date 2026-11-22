import { UserRole } from '../../store/account/account.actions';

/**
 * Áreas de la gestión por rol. Es la misma matriz que el backend
 * (backend-dotnet/src/AcademiaDigital.Domain/Services/RolePermissions.cs): si cambia una, cambiar la otra.
 */
export type Permission =
  | 'ManageUsers' | 'ManageCareers' | 'ViewStudents' | 'ManageStudents' | 'ManageTeachers' | 'ManageGrades'
  | 'ManageAttendance' | 'ManageExams' | 'ManageEnrollments' | 'ManageCertificates' | 'ManageCalendar' | 'ViewReports';

const MATRIX: Partial<Record<UserRole, Permission[]>> = {
  [UserRole.Secretario]: ['ViewStudents', 'ManageStudents', 'ManageTeachers', 'ManageGrades', 'ManageAttendance',
    'ManageExams', 'ManageEnrollments', 'ManageCertificates', 'ManageCalendar', 'ViewReports'],
  [UserRole.Coordinador]: ['ViewStudents', 'ManageGrades', 'ManageAttendance', 'ManageExams', 'ManageCalendar', 'ViewReports'],
  [UserRole.Preceptor]: ['ViewStudents', 'ManageAttendance', 'ManageCalendar'],
  [UserRole.Cooperadora]: ['ViewStudents', 'ViewReports']
};

export function hasPermission(role: UserRole | null | undefined, permission: Permission): boolean {
  if (role === UserRole.Admin) return true;
  return role != null && (MATRIX[role] ?? []).includes(permission);
}

/** Personal de la institución con acceso a la gestión (Admin o un rol de la matriz). */
export function isStaff(role: UserRole | null | undefined): boolean {
  return role === UserRole.Admin || (role != null && MATRIX[role] != null);
}

export const ROLE_LABELS: Record<UserRole, string> = {
  [UserRole.Alumno]: 'Alumno',
  [UserRole.Profesor]: 'Profesor',
  [UserRole.Admin]: 'Administrador',
  [UserRole.Secretario]: 'Secretaría',
  [UserRole.Coordinador]: 'Coordinación',
  [UserRole.Preceptor]: 'Preceptoría',
  [UserRole.Cooperadora]: 'Cooperadora'
};
