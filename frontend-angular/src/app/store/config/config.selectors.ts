import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ConfigState } from './config.reducer';

export const selectConfigState = createFeatureSelector<ConfigState>('config');

export const selectCollapseMenu = createSelector(
  selectConfigState,
  (state) => state.collapseMenu
);

export const selectOpenItem = createSelector(
  selectConfigState,
  (state) => state.openItem
);
