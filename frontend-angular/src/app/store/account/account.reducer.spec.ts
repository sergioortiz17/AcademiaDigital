import { accountInitialize, login, logout, UserRole } from './account.actions';
import { accountReducer, initialAccountState } from './account.reducer';

describe('accountReducer', () => {
  it('returns the initial state for unknown actions', () => {
    const state = accountReducer(undefined, { type: '@@INIT' } as any);
    expect(state).toEqual(initialAccountState);
  });

  describe('accountInitialize', () => {
    it('sets isLoggedIn, user, token and marks initialized', () => {
      const user = { _id: 1, email: 'a@b.com', role: UserRole.Alumno };
      const state = accountReducer(
        initialAccountState,
        accountInitialize({ isLoggedIn: true, user, token: 'tok' })
      );
      expect(state.isLoggedIn).toBe(true);
      expect(state.isInitialized).toBe(true);
      expect(state.token).toBe('tok');
      expect(state.user).toEqual(user);
    });

    it('marks initialized even when not logged in', () => {
      const state = accountReducer(
        initialAccountState,
        accountInitialize({ isLoggedIn: false, user: null, token: '' })
      );
      expect(state.isInitialized).toBe(true);
      expect(state.isLoggedIn).toBe(false);
    });
  });

  describe('login', () => {
    it('sets isLoggedIn, user and token', () => {
      const user = { _id: 2, email: 'prof@test.com', role: UserRole.Profesor };
      const state = accountReducer(initialAccountState, login({ user, token: 'jwt' }));
      expect(state.isLoggedIn).toBe(true);
      expect(state.token).toBe('jwt');
      expect(state.user).toEqual(user);
    });
  });

  describe('logout', () => {
    it('clears user, token and sets isLoggedIn to false', () => {
      const loggedIn = {
        ...initialAccountState,
        isLoggedIn: true,
        token: 'jwt',
        user: { email: 'x@y.com' }
      };
      const state = accountReducer(loggedIn, logout());
      expect(state.isLoggedIn).toBe(false);
      expect(state.token).toBe('');
      expect(state.user).toBeNull();
    });
  });
});
