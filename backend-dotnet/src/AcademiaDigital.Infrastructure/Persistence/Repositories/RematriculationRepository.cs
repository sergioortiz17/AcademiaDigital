using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class RematriculationRepository(AppDbContext db) : IRematriculationRepository
{
    public Task<StudentCareer?> LockActiveStudentCareerAsync(
        long studentId,
        int careerId,
        CancellationToken ct = default)
        => db.StudentCareers
            .FromSqlInterpolated($"SELECT * FROM [StudentCareers] WITH (UPDLOCK, HOLDLOCK) WHERE [StudentId] = {studentId} AND [CareerId] = {careerId} AND [IsActive] = 1")
            .SingleOrDefaultAsync(ct);

    public Task<StudentRematriculation?> FindByCycleAsync(
        long studentCareerId,
        int academicYear,
        CancellationToken ct = default)
        => db.StudentRematriculations.AsNoTracking()
            .Include(item => item.StudyPlan)
            .Include(item => item.Commission)
            .FirstOrDefaultAsync(item => item.StudentCareerId == studentCareerId
                && item.AcademicYear == academicYear, ct);

    public Task<int?> GetLatestAcademicYearAsync(long studentCareerId, CancellationToken ct = default)
        => db.StudentAcademicAssignments.AsNoTracking()
            .Where(item => item.StudentCareerId == studentCareerId)
            .MaxAsync(item => (int?)item.AcademicYear, ct);

    public async Task<IReadOnlyList<StudentAcademicAssignment>> GetCurrentAssignmentsAsync(
        long studentCareerId,
        CancellationToken ct = default)
        => await db.StudentAcademicAssignments
            .Where(item => item.StudentCareerId == studentCareerId && item.IsCurrent)
            .ToArrayAsync(ct);

    public Task<StudentStudyPlan?> FindCurrentStudyPlanAsync(
        long studentCareerId,
        CancellationToken ct = default)
        => db.StudentStudyPlans.FirstOrDefaultAsync(
            item => item.StudentCareerId == studentCareerId && item.IsCurrent, ct);

    public async Task<StudentRematriculation> CreateAsync(
        StudentRematriculation rematriculation,
        StudentAcademicAssignment assignment,
        StudentStudyPlan? newStudyPlan,
        CancellationToken ct = default)
    {
        db.StudentRematriculations.Add(rematriculation);
        db.StudentAcademicAssignments.Add(assignment);
        if (newStudyPlan is not null)
            db.StudentStudyPlans.Add(newStudyPlan);
        try
        {
            await db.SaveChangesAsync(ct);
            return rematriculation;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new StudentRematriculationAlreadyExistsException(
                rematriculation.StudentCareerId,
                rematriculation.AcademicYear);
        }
    }
}
