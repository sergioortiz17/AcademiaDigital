import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AccountState } from './account.reducer';
import { UserRole } from './account.actions';

export const selectAccountState = createFeatureSelector<AccountState>('account');

export const selectIsLoggedIn = createSelector(selectAccountState, (s) => s.isLoggedIn);
export const selectIsInitialized = createSelector(selectAccountState, (s) => s.isInitialized);
export const selectToken = createSelector(selectAccountState, (s) => s.token);
export const selectUser = createSelector(selectAccountState, (s) => s.user);
export const selectUserRole = createSelector(selectAccountState, (s) => s.user?.role ?? null);
export const selectIsAdmin = createSelector(selectAccountState, (s) => s.user?.role === UserRole.Admin);
export const selectIsProfesor = createSelector(selectAccountState, (s) => s.user?.role === UserRole.Profesor);
export const selectIsAlumno = createSelector(selectAccountState, (s) => s.user?.role === UserRole.Alumno);
