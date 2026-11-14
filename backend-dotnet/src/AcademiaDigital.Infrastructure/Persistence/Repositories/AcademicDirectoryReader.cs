using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class AcademicDirectoryReader(AppDbContext db, TimeProvider timeProvider) : IAcademicDirectoryReader
{
    public async Task<IReadOnlyList<AcademicDirectoryStudent>> ReadAsync(CancellationToken ct = default)
    {
        var fallbackYear = timeProvider.GetUtcNow().Year;
        var rows = await db.StudentCareers.AsNoTracking()
            .Select(item => new
            {
                item.StudentId,
                StudentCareerId = item.Id,
                item.Student.User.Username,
                item.Student.User.LastName,
                item.Student.User.Dni,
                item.Student.LegajoNumber,
                item.CareerId,
                CareerName = item.Career.Name,
                Assignment = item.AcademicAssignments
                    .Where(assignment => assignment.IsCurrent)
                    .Select(assignment => new
                    {
                        CommissionId = assignment.DivisionId,
                        CommissionName = assignment.Division != null ? assignment.Division.Name : null,
                        assignment.AcademicYear
                    })
                    .FirstOrDefault(),
                IsActive = item.IsActive
                    && item.Student.User.IsActive
                    && item.Student.Status != StudentStatus.Withdrawn
            })
            .OrderBy(item => item.StudentCareerId)
            .ToArrayAsync(ct);

        return rows.Select(item => new AcademicDirectoryStudent(
            item.StudentId,
            item.StudentCareerId,
            $"{item.Username} {item.LastName}".Trim(),
            item.Dni ?? string.Empty,
            item.LegajoNumber,
            item.CareerId,
            item.CareerName,
            item.Assignment?.CommissionId,
            item.Assignment?.CommissionName,
            item.Assignment?.AcademicYear ?? fallbackYear,
            item.IsActive)).ToArray();
    }
}
