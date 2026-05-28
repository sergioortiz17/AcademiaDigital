import { createAction, props } from '@ngrx/store';

export enum UserRole {
  Alumno = 1,
  Profesor = 2,
  Admin = 3
}

export interface UserModel {
  _id?: string | number;
  email: string;
  username?: string;
  role?: UserRole;
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
