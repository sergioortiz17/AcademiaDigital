class AuthenticationException(Exception):
    """Excepción base para errores de autenticación"""
    pass


class InvalidCredentialsException(AuthenticationException):
    """Excepción cuando las credenciales son inválidas"""
    def __init__(self, message: str = "Invalid credentials"):
        self.message = message
        super().__init__(self.message)


class InactiveUserException(AuthenticationException):
    """Excepción cuando el usuario está inactivo"""
    def __init__(self, message: str = "User is not active"):
        self.message = message
        super().__init__(self.message)


class SessionNotFoundException(AuthenticationException):
    """Excepción cuando no se encuentra una sesión"""
    def __init__(self, message: str = "Session not found"):
        self.message = message
        super().__init__(self.message)


class EmailAlreadyExistsException(AuthenticationException):
    """Excepción cuando el email ya existe"""
    def __init__(self, message: str = "Email already taken"):
        self.message = message
        super().__init__(self.message)

