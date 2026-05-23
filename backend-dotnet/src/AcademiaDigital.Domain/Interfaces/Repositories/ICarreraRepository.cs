using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICarreraRepository
{
    Task<IEnumerable<Carrera>> GetAllAsync();
    Task<Carrera?> GetByIdAsync(int id);
    Task AddAsync(Carrera carrera);
    void Update(Carrera carrera);
    void Delete(Carrera carrera);
    Task SaveChangesAsync();
}