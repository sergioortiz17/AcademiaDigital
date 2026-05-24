namespace AcademiaDigital.Domain.Exceptions;

public class AuthenticationException(string message) : Exception(message);

public class InvalidCredentialsException(string message = "Invalid email or password")
    : AuthenticationException(message);

public class InactiveUserException(string message = "User account is not active")
    : AuthenticationException(message);

public class AccountLockedException(string message = "User account is temporarily locked due to failed login attempts")
    : AuthenticationException(message);

public class SessionNotFoundException(string message = "Session not found")
    : AuthenticationException(message);

public class EmailAlreadyExistsException(string message = "Email already taken")
    : AuthenticationException(message);

public class UnauthorizedUserUpdateException(string message = "Error updating user")
    : AuthenticationException(message);

public class UserNotFoundException(string message = "User not found")
    : AuthenticationException(message);

public class InvalidUserRoleException(string message = "Invalid user role")
    : AuthenticationException(message);

public class UnauthorizedRoleChangeException(string message = "Error changing user role")
    : AuthenticationException(message);
