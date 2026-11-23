import { IStepOption } from 'ngx-ui-tour-md-menu';
import { UserRole } from '../../../store/account/account.actions';
import { ADMIN_TOUR } from './admin.tour';
import { STUDENT_TOUR } from './student.tour';
import { TEACHER_TOUR } from './teacher.tour';

/** Tours por rol. Los roles sin tour (Secretaría, Coordinación, etc.) no ven el botón "Ver tour". */
export const TOURS_BY_ROLE: Partial<Record<UserRole, IStepOption[]>> = {
  [UserRole.Alumno]: STUDENT_TOUR,
  [UserRole.Profesor]: TEACHER_TOUR,
  [UserRole.Admin]: ADMIN_TOUR
};
