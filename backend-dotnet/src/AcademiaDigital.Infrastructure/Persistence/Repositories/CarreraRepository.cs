using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;
public class carreraRepository : ICarreraRepository
{
    private readonly AppDbContext _context;

    public carreraRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Carrera>> GetAllAsync()
    {
        return await _context.Set<Carrera>().ToListAsync();
    }

    public async Task<Carrera?> GetByIdAsync(int id)
    {
        return await _context.Set<Carrera>().FindAsync(id);
    }

    public async Task AddAsync(Carrera carrera)
    {
        await _context.Set<Carrera>().AddAsync(carrera);
    }

    public void Update(Carrera carrera)
    {
        _context.Set<Carrera>().Update(carrera);
    }

    public void Delete(Carrera carrera)
    {
        _context.Set<Carrera>().Remove(carrera);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}