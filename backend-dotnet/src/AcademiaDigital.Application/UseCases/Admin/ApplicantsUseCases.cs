using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

/// <summary>
/// Sección "Aspirantes": cuentas del registro público pendientes de alta y, para seguimiento,
/// las dadas de alta en los últimos <see cref="RecentDays"/> días.
/// </summary>
public class GetApplicantsUseCase(IUserRepository userRepository)
{
    public const int RecentDays = 7;

    public async Task<GetApplicantsResult> ExecuteAsync(
        string? search, UserRole? role, bool approved, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = new ApplicantQuery(
            search,
            role,
            approved ? RegistrationStatus.Approved : RegistrationStatus.Pending,
            approved ? DateTime.UtcNow.AddDays(-RecentDays) : null,
            (page - 1) * pageSize,
            pageSize);

        var (rows, total) = await userRepository.GetApplicantsAsync(query, ct);
        var items = rows.Select(r => new ApplicantSummary(
            r.User.Id, r.User.Username, r.User.LastName, r.User.Email, r.User.Dni, r.User.Role,
            r.User.RegistrationStatus == RegistrationStatus.Pending, r.User.DateJoined, r.User.ApprovedAt, r.CareerName))
            .ToList();

        return new GetApplicantsResult(items, total, page, pageSize);
    }
}

/// <summary>
/// Da de alta (en masa) a los aspirantes indicados: pasan a poder iniciar sesión y, si son
/// docentes, su perfil queda activo para asignarles materias y divisiones.
/// </summary>
public class ApproveApplicantsUseCase(IUserRepository userRepository, ITeacherRepository teacherRepository)
{
    public const int MaxPerRequest = 500;

    public async Task<ApproveApplicantsResult> ExecuteAsync(
        long approvedByUserId, IReadOnlyCollection<long> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
            throw new ArgumentException("Selecciona al menos un aspirante.");
        if (ids.Length > MaxPerRequest)
            throw new ArgumentException($"Se pueden dar de alta hasta {MaxPerRequest} aspirantes por vez.");

        var approved = await userRepository.ApproveRegistrationsAsync(ids, approvedByUserId, DateTime.UtcNow, ct);

        foreach (var user in approved.Where(u => u.Role == UserRole.Profesor))
        {
            var teacher = await teacherRepository.FindByUserIdAsync(user.Id, ct);
            if (teacher is null || teacher.IsActive) continue;
            teacher.IsActive = true;
            await teacherRepository.UpdateAsync(teacher, ct);
        }

        return new ApproveApplicantsResult(approved.Select(u => u.Id).ToList(), ids.Length - approved.Count);
    }
}

public record ApplicantSummary(
    long Id, string Username, string LastName, string Email, string? Dni, UserRole Role,
    bool Pending, DateTime DateJoined, DateTime? ApprovedAt, string? CareerName);

public record GetApplicantsResult(List<ApplicantSummary> Items, int Total, int Page, int PageSize);

/// <param name="Skipped">Ids que no estaban pendientes (ya dados de alta o inexistentes).</param>
public record ApproveApplicantsResult(List<long> ApprovedIds, int Skipped);
