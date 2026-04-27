import { createReducer, on } from '@ngrx/store';
import { accountInitialize, login, logout, UserModel } from './account.actions';

export interface AccountState {
  token: string;
  isLoggedIn: boolean;
  isInitialized: boolean;
  user: UserModel | null;
}

export const initialAccountState: AccountState = {
  token: '',
  isLoggedIn: false,
  isInitialized: false,
  user: null
};

export const accountReducer = createReducer(
  initialAccountState,
  on(accountInitialize, (state, { isLoggedIn, user, token }) => ({
    ...state,
    isLoggedIn,
    isInitialized: true,
    token,
    user
  })),
  on(login, (state, { user, token }) => ({
    ...state,
    isLoggedIn: true,
    token,
    user
  })),
  on(logout, (state) => ({
    ...state,
    isLoggedIn: false,
    token: '',
    user: null
  }))
);
