using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class TeacherRepository(AppDbContext db) : ITeacherRepository
{
    public async Task<IEnumerable<Teacher>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
        => await db.Teachers.AsNoTracking()
            .Include(t => t.User)
            .Where(t => includeInactive || t.IsActive)
            .OrderBy(t => t.EmployeeNumber)
            .ToListAsync(ct);

    public async Task<Teacher?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.Teachers.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Teacher?> FindByUserIdAsync(long userId, CancellationToken ct = default)
        => await db.Teachers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

    public async Task<Teacher?> FindByEmployeeNumberAsync(string employeeNumber, CancellationToken ct = default)
        => await db.Teachers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeNumber == employeeNumber, ct);

    public async Task<Teacher> CreateAsync(Teacher teacher, CancellationToken ct = default)
    {
        db.Teachers.Add(teacher);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw DuplicateException(ex);
        }
        return teacher;
    }

    public async Task<Teacher> UpdateAsync(Teacher teacher, CancellationToken ct = default)
    {
        db.Entry(teacher).State = EntityState.Modified;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw DuplicateException(ex);
        }
        return teacher;
    }

    public async Task DeleteAsync(Teacher teacher, CancellationToken ct = default)
    {
        db.Teachers.Remove(teacher);
        await db.SaveChangesAsync(ct);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            || (exception.InnerException?.Message ?? exception.Message)
                .Contains("unique", StringComparison.OrdinalIgnoreCase);

    private static TeacherAlreadyExistsException DuplicateException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("employee_number", StringComparison.OrdinalIgnoreCase)
            ? new TeacherAlreadyExistsException("employee number")
            : new TeacherAlreadyExistsException("user");
    }
}
