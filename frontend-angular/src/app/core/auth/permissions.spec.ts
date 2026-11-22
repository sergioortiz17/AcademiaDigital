import { describe, expect, it } from 'vitest';
import { UserRole } from '../../store/account/account.actions';
import { hasPermission, isStaff } from './permissions';

describe('permissions', () => {
  it('gives admin everything and keeps users and careers admin-only', () => {
    expect(hasPermission(UserRole.Admin, 'ManageUsers')).toBe(true);
    for (const role of [UserRole.Secretario, UserRole.Coordinador, UserRole.Preceptor, UserRole.Cooperadora]) {
      expect(hasPermission(role, 'ManageUsers')).toBe(false);
      expect(hasPermission(role, 'ManageCareers')).toBe(false);
    }
  });

  it('matches the backend matrix for each staff role', () => {
    expect(hasPermission(UserRole.Secretario, 'ManageStudents')).toBe(true);
    expect(hasPermission(UserRole.Coordinador, 'ManageGrades')).toBe(true);
    expect(hasPermission(UserRole.Coordinador, 'ManageStudents')).toBe(false);
    expect(hasPermission(UserRole.Preceptor, 'ManageAttendance')).toBe(true);
    expect(hasPermission(UserRole.Cooperadora, 'ViewReports')).toBe(true);
    expect(hasPermission(UserRole.Profesor, 'ViewStudents')).toBe(false);
  });

  it('recognizes staff roles', () => {
    expect(isStaff(UserRole.Preceptor)).toBe(true);
    expect(isStaff(UserRole.Profesor)).toBe(false);
  });
});
