namespace AcademiaDigital.Domain.Exceptions;

public class AuthenticationException(string message) : Exception(message);

public class InvalidCredentialsException(string message = "Email o contraseña inválidos")
    : AuthenticationException(message);

public class InactiveUserException(string message = "La cuenta de usuario no está activa")
    : AuthenticationException(message);

public class AccountLockedException(DateTime lockedUntil)
    : AuthenticationException($"Cuenta bloqueada. Volvé a intentar después de {lockedUntil:O}")
{
    public DateTime LockedUntil { get; } = lockedUntil;
}

public class SessionNotFoundException(string message = "Sesión no encontrada")
    : AuthenticationException(message);

public class EmailAlreadyExistsException(string message = "El email ya está en uso")
    : AuthenticationException(message);

public class DniAlreadyExistsException(string message = "El DNI ya está en uso")
    : AuthenticationException(message);

public class UnauthorizedUserUpdateException(string message = "Error al actualizar el usuario")
    : AuthenticationException(message);

public class UserNotFoundException(long id)
    : AuthenticationException($"No se encontró el usuario con id {id}");

public class ForbiddenException(string message = "Acceso denegado")
    : AuthenticationException(message);

public class InvalidCurrentPasswordException(string message = "La contraseña actual es incorrecta")
    : AuthenticationException(message);

public class InvalidResetTokenException(string message = "El token de recuperación es inválido o expiró")
    : AuthenticationException(message);
