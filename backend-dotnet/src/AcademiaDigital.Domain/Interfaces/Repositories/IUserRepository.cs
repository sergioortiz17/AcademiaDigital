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

    // Admin methods
    Task<(List<User> Users, int Total)> GetAllAsync(string? search, UserRole? role, int skip, int take, CancellationToken ct = default);
    Task<User> UpdateRoleAsync(long userId, UserRole newRole, CancellationToken ct = default);
    Task<User> UpdateActiveStatusAsync(long userId, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(long userId, CancellationToken ct = default);
}
