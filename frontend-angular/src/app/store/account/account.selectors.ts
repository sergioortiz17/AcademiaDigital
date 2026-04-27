import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AccountState } from './account.reducer';

export const selectAccountState = createFeatureSelector<AccountState>('account');

export const selectIsLoggedIn = createSelector(
  selectAccountState,
  (state) => state.isLoggedIn
);

export const selectIsInitialized = createSelector(
  selectAccountState,
  (state) => state.isInitialized
);

export const selectToken = createSelector(
  selectAccountState,
  (state) => state.token
);

export const selectUser = createSelector(
  selectAccountState,
  (state) => state.user
);
