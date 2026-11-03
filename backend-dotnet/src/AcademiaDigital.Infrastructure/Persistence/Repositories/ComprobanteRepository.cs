using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class ComprobanteRepository(AppDbContext db) : IComprobanteRepository
{
    public async Task<Comprobante> CreateAsync(Comprobante comprobante, CancellationToken ct = default)
    {
        db.Comprobantes.Add(comprobante);
        await db.SaveChangesAsync(ct);
        return comprobante;
    }

    public Task<Comprobante?> FindByIdAsync(long id, CancellationToken ct = default)
        => db.Comprobantes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Comprobante?> FindForUpdateAsync(long id, CancellationToken ct = default)
        => db.Comprobantes
            .FromSqlInterpolated($"SELECT * FROM \"Comprobantes\" WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    public async Task<List<Comprobante>> GetAllAsync(
        ComprobanteEstado? estado, int? puntoVenta, CancellationToken ct = default)
    {
        var query = db.Comprobantes.AsNoTracking().AsQueryable();

        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);
        if (puntoVenta.HasValue)
            query = query.Where(c => c.PuntoVenta == puntoVenta.Value);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
    }
}
