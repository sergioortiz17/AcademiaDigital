using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db, IPasswordHasher passwordHasher) : IUserRepository
{
    public async Task<User?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email.Trim(), ct);

    public async Task<User?> FindByDniAsync(string dni, CancellationToken ct = default)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Dni == dni.Trim(), ct);

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim(), ct);
        if (user is null) return null;
        return passwordHasher.Verify(password, user.Password) ? user : null;
    }

    public async Task<User> CreateAsync(string email, string username, string lastName, string password, string dni, UserRole role = UserRole.Alumno, CancellationToken ct = default)
    {
        var user = new User
        {
            Email = email.Trim(),
            Username = username.Trim(),
            LastName = lastName.Trim(),
            Password = passwordHasher.Hash(password),
            Dni = dni.Trim(),
            IsActive = true,
            DateJoined = DateTime.UtcNow,
            Role = role
        };

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, "email"))
        {
            throw new EmailAlreadyExistsException();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, "dni"))
        {
            throw new DniAlreadyExistsException();
        }

        return user;
    }

    public async Task<User> RecordFailedLoginAsync(long userId, int maxAttempts, TimeSpan lockoutDuration, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException(userId);

        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= maxAttempts)
            user.LockedUntil = DateTime.UtcNow.Add(lockoutDuration);

        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task ResetLoginFailuresAsync(long userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException(userId);

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await db.SaveChangesAsync(ct);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex, string column)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        var isUniqueViolation = ex.InnerException is SqlException { Number: 2601 or 2627 }
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicada", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique", StringComparison.OrdinalIgnoreCase);

        return isUniqueViolation
            && (message.Contains(column, StringComparison.OrdinalIgnoreCase)
                || message.Contains($"IX_Users_{column}", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdatePasswordAsync(long userId, string hashedPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException(userId);
        user.Password = hashedPassword;
        await db.SaveChangesAsync(ct);
    }

    public async Task<(List<User> Users, int Total)> GetAllAsync(string? search, UserRole? role, int skip, int take, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                (u.Dni != null && u.Dni.ToLower().Contains(term)));
        }

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderByDescending(u => u.DateJoined)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (users, total);
    }

    public async Task<User> UpdateRoleAsync(long userId, UserRole newRole, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException(userId);
        user.Role = newRole;
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateActiveStatusAsync(long userId, bool isActive, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException(userId);

        user.IsActive = isActive;
        if (!isActive)
            user.LockedUntil = null;

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
