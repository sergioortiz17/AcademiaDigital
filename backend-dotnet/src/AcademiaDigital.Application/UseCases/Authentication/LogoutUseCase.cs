using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class LogoutUseCase(ISessionRepository sessionRepository)
{
    public async Task<LogoutResult> ExecuteAsync(long userId, CancellationToken ct = default)
    {
        var session = await sessionRepository.FindByUserAsync(userId, ct)
            ?? throw new SessionNotFoundException();

        await sessionRepository.DeleteAsync(session, ct);

        return new LogoutResult(Success: true, Msg: "Token revoked");
    }
}

public record LogoutResult(bool Success, string Msg);
