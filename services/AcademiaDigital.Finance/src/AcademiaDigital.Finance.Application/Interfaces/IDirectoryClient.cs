namespace AcademiaDigital.Finance.Application.Interfaces;

// Display-name lookup against the monolith. Implementations MUST degrade to returning the
// id (as a string) when the monolith is unreachable — Finance never blocks or fails a
// request because a display name could not be resolved (see ADR 0001 / README).
public sealed record CareerInfo(int CareerId, string Name, string? Code);
public sealed record UserInfo(long UserId, string FullName);
public sealed record StudentInfo(long StudentId, string FullName, string? Legajo);

public interface IDirectoryClient
{
    Task<CareerInfo> GetCareerAsync(int careerId, CancellationToken ct = default);
    Task<UserInfo> GetUserAsync(long userId, CancellationToken ct = default);
    Task<StudentInfo> GetStudentAsync(long studentId, CancellationToken ct = default);
}
