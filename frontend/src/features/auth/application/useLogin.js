import { useState } from 'react';
import { useDispatch } from 'react-redux';
import { useHistory } from 'react-router-dom';
import { authApi } from '../infrastructure/authApi';
import { ACCOUNT_INITIALIZE } from '../../../store/actions';

/**
 * Hook para manejar el login de usuarios
 * Capa de aplicación - Use Case
 */
export const useLogin = () => {
    const dispatch = useDispatch();
    const history = useHistory();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);

    const login = async (credentials) => {
        setIsLoading(true);
        setError(null);

        try {
            const result = await authApi.login(credentials);
            
            if (result.success) {
                dispatch({
                    type: ACCOUNT_INITIALIZE,
                    payload: {
                        isLoggedIn: true,
                        user: result.user,
                        token: result.token
                    }
                });
                history.push('/app/dashboard/default');
                return { success: true };
            } else {
                const errorMessage = result.msg || 'Invalid credentials';
                setError(errorMessage);
                return { success: false, error: errorMessage };
            }
        } catch (err) {
            const errorMessage = err.response?.data?.msg || err.message || 'An error occurred';
            setError(errorMessage);
            return { success: false, error: errorMessage };
        } finally {
            setIsLoading(false);
        }
    };

    return { login, isLoading, error };
};

