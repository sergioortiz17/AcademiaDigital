namespace AcademiaDigital.Application.Interfaces;

public interface IAdmissionChallengeVerifier
{
    Task<bool> VerifyAsync(
        string? challengeToken,
        string? remoteIpAddress,
        CancellationToken ct = default);
}
