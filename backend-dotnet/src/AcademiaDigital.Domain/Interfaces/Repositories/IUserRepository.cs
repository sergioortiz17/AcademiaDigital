using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByDniAsync(string dni, CancellationToken ct = default);
    Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default);
    Task<User> CreateAsync(string email, string username, string lastName, string password, string dni, UserRole role = UserRole.Alumno, CancellationToken ct = default);
    Task<User> RecordFailedLoginAsync(long userId, int maxAttempts, TimeSpan lockoutDuration, CancellationToken ct = default);
    Task ResetLoginFailuresAsync(long userId, CancellationToken ct = default);
    Task<User> UpdateAsync(User user, CancellationToken ct = default);
    Task UpdatePasswordAsync(long userId, string hashedPassword, CancellationToken ct = default);

    // Admin methods
    Task<(List<User> Users, int Total)> GetAllAsync(string? search, UserRole? role, int skip, int take, CancellationToken ct = default);
    Task<User> UpdateRoleAsync(long userId, UserRole newRole, CancellationToken ct = default);
    Task<User> UpdateActiveStatusAsync(long userId, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(long userId, CancellationToken ct = default);

    // Aspirantes: altas del registro público
    Task<(List<ApplicantRow> Rows, int Total)> GetApplicantsAsync(ApplicantQuery query, CancellationToken ct = default);
    /// <summary>Aprueba los usuarios indicados que estén pendientes; ignora el resto. Devuelve los aprobados.</summary>
    Task<List<User>> ApproveRegistrationsAsync(IReadOnlyCollection<long> userIds, long approvedByUserId, DateTime approvedAt, CancellationToken ct = default);
}

/// <param name="Status">Pending: pendientes de alta. Approved: dados de alta desde <paramref name="ApprovedSince"/>.</param>
public sealed record ApplicantQuery(
    string? Search, UserRole? Role, RegistrationStatus Status, DateTime? ApprovedSince, int Skip, int Take);

public sealed record ApplicantRow(User User, string? CareerName);
