namespace AcademiaDigital.API.Observability;

/// <summary>
/// Auditoría de accesos: login exitoso, login fallido y logout, con usuario, rol e IP.
/// Escribe en su propia categoría de log, que queda en Information aunque el resto de la
/// aplicación esté en Error (Observability__MinimumLevel). Así producción guarda solo
/// errores + accesos, sin el resto de los Info y Warning.
/// Nunca registra el email, la contraseña ni el token.
/// </summary>
public sealed class AuditLog(ILogger<AuditLog> logger)
{
    /// <summary>Categoría de log (nombre completo de esta clase).</summary>
    public static readonly string Category = typeof(AuditLog).FullName!;

    public void LoginSucceeded(long userId, int role, string? ip, string? userAgent)
        => logger.LogInformation("{Event} {UserId} {Role} {Ip} {UserAgent}",
            "login_success", userId, role, ip, userAgent);

    /// <param name="status">401 credenciales inválidas, 403 inactivo, 423 bloqueado, 429 rate limit.</param>
    public void LoginFailed(int status, string? ip, string? userAgent)
        => logger.LogInformation("{Event} {Status} {Ip} {UserAgent}",
            "login_failed", status, ip, userAgent);

    public void LoggedOut(object? userId, string? ip)
        => logger.LogInformation("{Event} {UserId} {Ip}", "logout", userId, ip);
}
