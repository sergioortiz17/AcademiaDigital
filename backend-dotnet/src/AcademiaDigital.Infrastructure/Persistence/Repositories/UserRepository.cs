using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db, IPasswordHasher passwordHasher) : IUserRepository
{
    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
        => await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

    public async Task<User?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> FindByDniAsync(string dni, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Dni == dni, ct);

    public async Task<User?> FindByEmailForLoginAsync(string email, CancellationToken ct = default)
        => await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        // Tracking ON para que EF pueda actualizar si es necesario post-login
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return null;

        return passwordHasher.Verify(password, user.Password) ? user : null;
    }

    public async Task<User> CreateAsync(string email, string username, string password, string dni, UserRole role = UserRole.Alumno, CancellationToken ct = default)
    {
        var user = new User
        {
            Email = email,
            Username = username,
            Dni = dni,
            Password = passwordHasher.Hash(password),
            IsActive = true,
            FailedLoginAttempts = 0,
            LockedUntil = null,
            Role = role,
            DateJoined = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
