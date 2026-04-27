using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CooperativeEntityRepository(AppDbContext db) : ICooperativeEntityRepository
{
    public async Task<IEnumerable<CooperativeEntity>> GetAllAsync(CancellationToken ct = default)
        => await db.CooperativeEntities.AsNoTracking().ToListAsync(ct);

    public async Task<CooperativeEntity?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.CooperativeEntities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<CooperativeEntity?> FindByCuitAsync(string cuit, CancellationToken ct = default)
        => await db.CooperativeEntities.AsNoTracking().FirstOrDefaultAsync(e => e.Cuit == cuit, ct);

    public async Task<CooperativeEntity> CreateAsync(CooperativeEntity entity, CancellationToken ct = default)
    {
        db.CooperativeEntities.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<CooperativeEntity> UpdateAsync(CooperativeEntity entity, CancellationToken ct = default)
    {
        db.CooperativeEntities.Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task DeleteAsync(CooperativeEntity entity, CancellationToken ct = default)
    {
        db.CooperativeEntities.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
