import { useDispatch } from 'react-redux';
import { useHistory } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { authApi } from '../infrastructure/authApi';
import { LOGOUT } from '../../../store/actions';

/**
 * Hook para manejar el logout de usuarios
 * Capa de aplicación - Use Case
 */
export const useLogout = () => {
    const dispatch = useDispatch();
    const history = useHistory();
    const account = useSelector((state) => state.account);

    const logout = async () => {
        try {
            if (account.token) {
                await authApi.logout(account.token);
            }
        } catch (err) {
            // Even if logout fails on server, clear local state
            console.error('Logout error:', err);
        } finally {
            // Always clear local state and redirect
            dispatch({ type: LOGOUT });
            history.push('/auth/signin');
        }
    };

    return { logout };
};

