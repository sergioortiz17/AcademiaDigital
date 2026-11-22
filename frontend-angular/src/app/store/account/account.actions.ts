import { createAction, props } from '@ngrx/store';

export enum UserRole {
  Alumno = 1,
  Profesor = 2,
  Admin = 3,
  Secretario = 4,
  Coordinador = 5,
  Preceptor = 6,
  Cooperadora = 7
}

export interface UserModel {
  _id?: string | number;
  email: string;
  username?: string;
  lastName?: string;
  dni?: string;
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
