import { AccountState } from './account.reducer';
import { UserRole } from './account.actions';
import {
  selectIsLoggedIn,
  selectIsInitialized,
  selectToken,
  selectUser,
  selectUserRole,
  selectIsAdmin,
  selectIsProfesor,
  selectIsAlumno
} from './account.selectors';

const buildState = (partial: Partial<AccountState>): { account: AccountState } => ({
  account: {
    isLoggedIn: false,
    isInitialized: false,
    token: '',
    user: null,
    ...partial
  }
});

describe('account selectors', () => {
  it('selectIsLoggedIn returns isLoggedIn', () => {
    expect(selectIsLoggedIn(buildState({ isLoggedIn: true }))).toBe(true);
    expect(selectIsLoggedIn(buildState({ isLoggedIn: false }))).toBe(false);
  });

  it('selectIsInitialized returns isInitialized', () => {
    expect(selectIsInitialized(buildState({ isInitialized: true }))).toBe(true);
  });

  it('selectToken returns token', () => {
    expect(selectToken(buildState({ token: 'abc' }))).toBe('abc');
  });

  it('selectUser returns user', () => {
    const user = { email: 'a@b.com', role: UserRole.Admin };
    expect(selectUser(buildState({ user }))).toEqual(user);
  });

  it('selectUserRole returns role when user exists', () => {
    const state = buildState({ user: { email: 'a@b.com', role: UserRole.Admin } });
    expect(selectUserRole(state)).toBe(UserRole.Admin);
  });

  it('selectUserRole returns null when no user', () => {
    expect(selectUserRole(buildState({ user: null }))).toBeNull();
  });

  it('selectIsAdmin is true only for Admin role', () => {
    expect(selectIsAdmin(buildState({ user: { email: 'a@b.com', role: UserRole.Admin } }))).toBe(true);
    expect(selectIsAdmin(buildState({ user: { email: 'a@b.com', role: UserRole.Alumno } }))).toBe(false);
  });

  it('selectIsProfesor is true only for Profesor role', () => {
    expect(selectIsProfesor(buildState({ user: { email: 'a@b.com', role: UserRole.Profesor } }))).toBe(true);
    expect(selectIsProfesor(buildState({ user: { email: 'a@b.com', role: UserRole.Admin } }))).toBe(false);
  });

  it('selectIsAlumno is true only for Alumno role', () => {
    expect(selectIsAlumno(buildState({ user: { email: 'a@b.com', role: UserRole.Alumno } }))).toBe(true);
    expect(selectIsAlumno(buildState({ user: { email: 'a@b.com', role: UserRole.Profesor } }))).toBe(false);
  });
});
