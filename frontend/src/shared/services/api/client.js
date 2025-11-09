import axios from 'axios';
import { API_SERVER } from '../../../config/constant';

class ApiClient {
    constructor(baseURL = API_SERVER) {
        this.client = axios.create({
            baseURL,
            headers: {
                'Content-Type': 'application/json',
            },
        });

        this.setupInterceptors();
    }

    setupInterceptors() {
        // Request interceptor
        this.client.interceptors.request.use(
            (config) => {
                const token = this.getToken();
                if (token) {
                    config.headers.Authorization = token;
                }
                return config;
            },
            (error) => Promise.reject(error)
        );

        // Response interceptor
        this.client.interceptors.response.use(
            (response) => response,
            (error) => {
                if (error.response?.status === 401) {
                    // Handle unauthorized
                    this.handleUnauthorized();
                }
                return Promise.reject(this.handleError(error));
            }
        );
    }

    getToken() {
        // Get token from Redux store or localStorage
        try {
            const state = JSON.parse(localStorage.getItem('persist:datta-account') || '{}');
            if (state.account) {
                const account = JSON.parse(state.account);
                return account.token || null;
            }
        } catch (e) {
            // Ignore parsing errors
        }
        return null;
    }

    handleUnauthorized() {
        // Clear token and redirect to login
        localStorage.removeItem('persist:datta-account');
        if (window.location.pathname !== '/auth/signin') {
            window.location.href = '/auth/signin';
        }
    }

    handleError(error) {
        const message = error.response?.data?.msg || error.message || 'An error occurred';
        const customError = new Error(message);
        customError.response = error.response;
        return customError;
    }

    async get(url, config) {
        const response = await this.client.get(url, config);
        return response.data;
    }

    async post(url, data, config) {
        const response = await this.client.post(url, data, config);
        return response.data;
    }

    async put(url, data, config) {
        const response = await this.client.put(url, data, config);
        return response.data;
    }

    async delete(url, config) {
        const response = await this.client.delete(url, config);
        return response.data;
    }
}

export const apiClient = new ApiClient();

