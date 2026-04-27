using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default);
    Task<User> CreateAsync(string email, string username, string password, CancellationToken ct = default);
    Task<User> UpdateAsync(User user, CancellationToken ct = default);
}
