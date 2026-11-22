import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from './app';
import { accountInitialize } from './store/account/account.actions';

describe('App', () => {
  type Args = ConstructorParameters<typeof App>;
  let translate: { setDefaultLang: ReturnType<typeof vi.fn>; use: ReturnType<typeof vi.fn> };
  let store: { dispatch: ReturnType<typeof vi.fn> };
  let tenant: { load: ReturnType<typeof vi.fn> };
  let app: App;

  beforeEach(() => {
    localStorage.clear();
    translate = { setDefaultLang: vi.fn(), use: vi.fn() };
    store = { dispatch: vi.fn() };
    tenant = { load: vi.fn() };
    app = new App(
      translate as unknown as Args[0], store as unknown as Args[1],
      {} as Args[2], tenant as unknown as Args[3]);
  });

  afterEach(() => localStorage.clear());

  it('always uses Spanish and drops the old language preference', () => {
    localStorage.setItem('i18nextLng', 'en');
    app.ngOnInit();
    expect(translate.use).toHaveBeenCalledWith('es');
    expect(localStorage.getItem('i18nextLng')).toBeNull();
    expect(tenant.load).toHaveBeenCalledTimes(1);
  });

  it('restores a persisted session', () => {
    localStorage.setItem('academia-account', JSON.stringify({ token: 't', user: { id: 1 } }));
    app.ngOnInit();
    expect(store.dispatch).toHaveBeenCalledWith(
      accountInitialize({ isLoggedIn: true, user: { id: 1 } as never, token: 't' }));
  });

  it('ignores a corrupt persisted session', () => {
    localStorage.setItem('academia-account', '{no json');
    app.ngOnInit();
    expect(store.dispatch).not.toHaveBeenCalled();
  });
});
