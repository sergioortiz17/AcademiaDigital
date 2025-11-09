import { apiClient } from '../../../shared/services/api/client';

/**
 * Auth API Service
 * Capa de infraestructura para llamadas API de autenticación
 */
export const authApi = {
    /**
     * Login de usuario
     * @param {Object} credentials - { email, password }
     * @returns {Promise<Object>} - { success, token, user }
     */
    login: async (credentials) => {
        // Usar API v1 (nueva arquitectura) con fallback a API antigua
        try {
            return await apiClient.post('/v1/users/login', credentials);
        } catch (error) {
            // Fallback a API antigua si v1 no está disponible
            return await apiClient.post('/users/login', credentials);
        }
    },

    /**
     * Registro de usuario
     * @param {Object} credentials - { email, username, password }
     * @returns {Promise<Object>} - { success, userID, msg }
     */
    register: async (credentials) => {
        // Usar API v1 (nueva arquitectura) con fallback a API antigua
        try {
            return await apiClient.post('/v1/users/register', credentials);
        } catch (error) {
            // Fallback a API antigua si v1 no está disponible
            return await apiClient.post('/users/register', credentials);
        }
    },

    /**
     * Logout de usuario
     * @param {string} token - Token JWT
     * @returns {Promise<void>}
     */
    logout: async (token) => {
        // Usar API v1 (nueva arquitectura) con fallback a API antigua
        try {
            return await apiClient.post('/v1/users/logout', { token });
        } catch (error) {
            // Fallback a API antigua si v1 no está disponible
            return await apiClient.post('/users/logout', { token });
        }
    },

    /**
     * Verificar sesión activa
     * @returns {Promise<Object>} - { success }
     */
    checkSession: async () => {
        // Usar API v1 (nueva arquitectura) con fallback a API antigua
        try {
            return await apiClient.post('/v1/users/checkSession');
        } catch (error) {
            // Fallback a API antigua si v1 no está disponible
            return await apiClient.post('/users/checkSession');
        }
    },
};

