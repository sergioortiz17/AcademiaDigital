import { createAction, props } from '@ngrx/store';

export interface UserModel {
  id?: string;
  email: string;
  username?: string;
}

export const accountInitialize = createAction(
  '[Account] Initialize',
  props<{ isLoggedIn: boolean; user: UserModel | null; token: string }>()
);

export const login = createAction(
  '[Account] Login',
  props<{ user: UserModel; token: string }>()
);

export const logout = createAction('[Account] Logout');
