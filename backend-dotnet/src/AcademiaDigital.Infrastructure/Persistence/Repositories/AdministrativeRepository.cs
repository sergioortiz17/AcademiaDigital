using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class AdministrativeRepository(AppDbContext db) : IAdministrativeRepository
{
    public async Task<IEnumerable<Administrative>> GetAllAsync(CancellationToken ct = default)
        => await db.Administratives.AsNoTracking()
            .Include(a => a.User)
            .ToListAsync(ct);

    public async Task<Administrative?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.Administratives.AsNoTracking()
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Administrative?> FindByUserIdAsync(long userId, CancellationToken ct = default)
        => await db.Administratives.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

    public async Task<Administrative?> FindByEmployeeNumberAsync(string employeeNumber, CancellationToken ct = default)
        => await db.Administratives.AsNoTracking()
            .FirstOrDefaultAsync(a => a.EmployeeNumber == employeeNumber, ct);

    public async Task<Administrative> CreateAsync(Administrative administrative, CancellationToken ct = default)
    {
        db.Administratives.Add(administrative);
        await db.SaveChangesAsync(ct);
        return administrative;
    }

    public async Task<Administrative> UpdateAsync(Administrative administrative, CancellationToken ct = default)
    {
        db.Administratives.Update(administrative);
        await db.SaveChangesAsync(ct);
        return administrative;
    }

    public async Task DeleteAsync(Administrative administrative, CancellationToken ct = default)
    {
        db.Administratives.Remove(administrative);
        await db.SaveChangesAsync(ct);
    }
}
