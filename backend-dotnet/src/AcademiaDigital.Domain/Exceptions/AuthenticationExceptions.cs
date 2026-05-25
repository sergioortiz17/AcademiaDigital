namespace AcademiaDigital.Domain.Exceptions;

public class AuthenticationException(string message) : Exception(message);

public class InvalidCredentialsException(string message = "Invalid email or password")
    : AuthenticationException(message);

public class InactiveUserException(string message = "User account is not active")
    : AuthenticationException(message);

public class AccountLockedException(DateTime lockedUntil)
    : AuthenticationException($"Account locked. Try again after {lockedUntil:O}")
{
    public DateTime LockedUntil { get; } = lockedUntil;
}

public class SessionNotFoundException(string message = "Session not found")
    : AuthenticationException(message);

public class EmailAlreadyExistsException(string message = "Email already taken")
    : AuthenticationException(message);

public class DniAlreadyExistsException(string message = "DNI already taken")
    : AuthenticationException(message);

public class UnauthorizedUserUpdateException(string message = "Error updating user")
    : AuthenticationException(message);

public class UserNotFoundException(long id)
    : AuthenticationException($"User with id {id} not found");

public class ForbiddenException(string message = "Access denied")
    : AuthenticationException(message);
