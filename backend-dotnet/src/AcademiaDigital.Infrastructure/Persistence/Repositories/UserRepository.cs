using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db, IPasswordHasher passwordHasher) : IUserRepository
{
    public async Task<User?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return null;
        return passwordHasher.Verify(password, user.Password) ? user : null;
    }

    public async Task<User> CreateAsync(string email, string username, string password, UserRole role = UserRole.Alumno, CancellationToken ct = default)
    {
        var user = new User
        {
            Email = email,
            Username = username,
            Password = passwordHasher.Hash(password),
            IsActive = true,
            DateJoined = DateTime.UtcNow,
            Role = role
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

    public async Task<List<User>> GetAllAsync(CancellationToken ct = default)
        => await db.Users.AsNoTracking().OrderByDescending(u => u.DateJoined).ToListAsync(ct);

    public async Task<User> UpdateRoleAsync(long userId, UserRole newRole, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException(userId);
        user.Role = newRole;
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task DeleteAsync(long userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException(userId);
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }
}
