import { createReducer, on } from '@ngrx/store';
import { collapseMenu, collapseToggle, navCollapseLeave, navContentLeave } from './config.actions';

export interface ConfigState {
  collapseMenu: boolean;
  openItem: string[];
  openComponent: string;
  openSubMenu: string[];
}

export const initialConfigState: ConfigState = {
  collapseMenu: false,
  openItem: ['dashboard'],
  openComponent: 'navigation',
  openSubMenu: []
};

export const configReducer = createReducer(
  initialConfigState,
  on(collapseMenu, (state) => ({
    ...state,
    collapseMenu: !state.collapseMenu
  })),
  on(collapseToggle, (state, { id }) => {
    const openItem = state.openItem.includes(id)
      ? state.openItem.filter((item) => item !== id)
      : [...state.openItem, id];
    return { ...state, openItem };
  }),
  on(navCollapseLeave, (state) => ({
    ...state,
    openSubMenu: []
  })),
  on(navContentLeave, (state) => ({
    ...state,
    openItem: ['dashboard'],
    openSubMenu: []
  }))
);
