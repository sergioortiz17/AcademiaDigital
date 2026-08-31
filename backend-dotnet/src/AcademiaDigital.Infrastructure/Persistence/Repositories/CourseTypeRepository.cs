using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CourseTypeRepository(AppDbContext db) : ICourseTypeRepository
{
    public async Task<CourseType?> FindByCodeAsync(string code, CancellationToken ct = default)
        => await db.CourseTypes.AsNoTracking().FirstOrDefaultAsync(ct2 => ct2.Code == code, ct);
}
