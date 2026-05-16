using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class MateriaRepository : IMateriaRepository
{
    private readonly AppDbContext _context;

    public MateriaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Materia>> GetAllAsync()
    {
        return await _context.Materias
            .Include(m => m.Correlativa)
            .ToListAsync();
    }

    public async Task<Materia?> GetByIdAsync(int id)
    {
        return await _context.Materias
            .Include(m => m.Correlativa)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddAsync(Materia materia)
    {
        await _context.Materias.AddAsync(materia);
    }

    public void Update(Materia materia)
    {
        _context.Materias.Update(materia);
    }

    public void Delete(Materia materia)
    {
        _context.Materias.Remove(materia);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
