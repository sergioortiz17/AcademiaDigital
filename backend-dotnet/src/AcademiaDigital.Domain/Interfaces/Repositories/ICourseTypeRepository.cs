using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICourseTypeRepository
{
    Task<CourseType?> FindByCodeAsync(string code, CancellationToken ct = default);
}
