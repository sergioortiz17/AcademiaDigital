// Domain types for Auth feature

export const AuthTypes = {
    // User
    USER: {
        id: 'number',
        email: 'string',
        username: 'string',
    },
    // Login
    LOGIN_CREDENTIALS: {
        email: 'string',
        password: 'string',
    },
    // Register
    REGISTER_CREDENTIALS: {
        email: 'string',
        username: 'string',
        password: 'string',
    },
    // Login Result
    LOGIN_RESULT: {
        success: 'boolean',
        token: 'string',
        user: 'object',
    },
    // Register Result
    REGISTER_RESULT: {
        success: 'boolean',
        userID: 'number',
        msg: 'string',
    },
};

