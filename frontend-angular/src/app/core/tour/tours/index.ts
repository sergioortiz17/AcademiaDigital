import { IStepOption } from 'ngx-ui-tour-md-menu';
import { UserRole } from '../../../store/account/account.actions';
import { ADMIN_TOUR } from './admin.tour';
import { STUDENT_TOUR } from './student.tour';
import { TEACHER_TOUR } from './teacher.tour';

/** Texto del botón de la barra superior para cada rol con tour. */
export const TOUR_LABELS: Partial<Record<UserRole, string>> = {
  [UserRole.Alumno]: 'Tour estudiante',
  [UserRole.Profesor]: 'Tour docente',
  [UserRole.Admin]: 'Tour admin'
};

/** Tours por rol. Los roles sin tour (Secretaría, Coordinación, etc.) no ven el botón del tour. */
export const TOURS_BY_ROLE: Partial<Record<UserRole, IStepOption[]>> = {
  [UserRole.Alumno]: STUDENT_TOUR,
  [UserRole.Profesor]: TEACHER_TOUR,
  [UserRole.Admin]: ADMIN_TOUR
};
