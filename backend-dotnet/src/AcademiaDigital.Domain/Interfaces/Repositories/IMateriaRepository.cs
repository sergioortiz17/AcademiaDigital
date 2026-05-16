using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IMateriaRepository
{
    Task<IEnumerable<Materia>> GetAllAsync();
    Task<Materia?> GetByIdAsync(int id);
    Task AddAsync(Materia materia);
    void Update(Materia materia);
    void Delete(Materia materia);
    Task SaveChangesAsync();
}
