using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICommissionRepository
{
    Task<Commission?> FindByIdAsync(int id, CancellationToken ct = default);
}
