import { useState } from 'react';
import { useHistory } from 'react-router-dom';
import { authApi } from '../infrastructure/authApi';

/**
 * Hook para manejar el registro de usuarios
 * Capa de aplicación - Use Case
 */
export const useRegister = () => {
    const history = useHistory();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);

    const register = async (credentials) => {
        setIsLoading(true);
        setError(null);

        try {
            const result = await authApi.register(credentials);
            
            if (result.success) {
                // Redirigir al login después del registro exitoso
                history.push('/auth/signin');
                return { success: true };
            } else {
                const errorMessage = result.msg || 'Registration failed';
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

    return { register, isLoading, error };
};

