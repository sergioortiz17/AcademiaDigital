import React from 'react';
import { Redirect } from 'react-router-dom';
import { useSelector } from 'react-redux';

/**
 * Guard para proteger rutas que requieren autenticación
 * Verifica que el usuario esté autenticado antes de permitir el acceso
 */
const AuthGuard = ({ children }) => {
    const account = useSelector((state) => state.account);
    const { isLoggedIn } = account;

    if (!isLoggedIn) {
        return <Redirect to="/auth/signin" />;
    }

    return children;
};

export default AuthGuard;

