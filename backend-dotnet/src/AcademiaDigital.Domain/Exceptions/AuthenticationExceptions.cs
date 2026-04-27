namespace AcademiaDigital.Domain.Exceptions;

public class AuthenticationException(string message) : Exception(message);

public class InvalidCredentialsException(string message = "Invalid email or password")
    : AuthenticationException(message);

public class InactiveUserException(string message = "User account is not active")
    : AuthenticationException(message);

public class SessionNotFoundException(string message = "Session not found")
    : AuthenticationException(message);

public class EmailAlreadyExistsException(string message = "Email already taken")
    : AuthenticationException(message);

public class UnauthorizedUserUpdateException(string message = "Error updating user")
    : AuthenticationException(message);
