import { IStepOption } from 'ngx-ui-tour-md-menu';
import { UserRole } from '../../../store/account/account.actions';
import { STUDENT_TOUR } from './student.tour';

/** Tours por rol. Docente y administración se agregan en sus etapas. */
export const TOURS_BY_ROLE: Partial<Record<UserRole, IStepOption[]>> = {
  [UserRole.Alumno]: STUDENT_TOUR
};

/** Nombre del tour en la URL de la demo (?tour=student) → rol. */
export const TOUR_KEYS: Record<string, UserRole> = {
  student: UserRole.Alumno
};
