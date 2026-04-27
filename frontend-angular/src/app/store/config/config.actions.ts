import { createAction, props } from '@ngrx/store';

export const collapseMenu = createAction('[Config] Collapse Menu');
export const collapseToggle = createAction(
  '[Config] Collapse Toggle',
  props<{ id: string }>()
);
export const navCollapseLeave = createAction('[Config] Nav Collapse Leave');
export const navContentLeave = createAction('[Config] Nav Content Leave');
