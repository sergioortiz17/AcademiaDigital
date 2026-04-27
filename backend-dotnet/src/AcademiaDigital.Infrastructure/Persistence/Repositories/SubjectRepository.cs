using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class SubjectRepository(AppDbContext db) : ISubjectRepository
{
    public async Task<IEnumerable<Subject>> GetAllAsync(CancellationToken ct = default)
        => await db.Subjects.AsNoTracking().Include(s => s.Career).ToListAsync(ct);

    public async Task<IEnumerable<Subject>> GetByCareerAsync(int careerId, CancellationToken ct = default)
        => await db.Subjects.AsNoTracking()
            .Where(s => s.CareerId == careerId)
            .OrderBy(s => s.Year).ThenBy(s => s.Semester)
            .ToListAsync(ct);

    public async Task<Subject?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.Subjects.AsNoTracking()
            .Include(s => s.Career)
            .Include(s => s.Prerequisites)
                .ThenInclude(sp => sp.PrerequisiteSubject)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Subject?> FindByCodeAsync(string code, CancellationToken ct = default)
        => await db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Code == code, ct);

    public async Task<IEnumerable<Subject>> GetPrerequisitesAsync(int subjectId, CancellationToken ct = default)
        => await db.SubjectPrerequisites.AsNoTracking()
            .Where(sp => sp.SubjectId == subjectId)
            .Select(sp => sp.PrerequisiteSubject)
            .ToListAsync(ct);

    public async Task AddPrerequisiteAsync(int subjectId, int prerequisiteSubjectId, CancellationToken ct = default)
    {
        var prerequisite = new SubjectPrerequisite
        {
            SubjectId = subjectId,
            PrerequisiteSubjectId = prerequisiteSubjectId
        };
        db.SubjectPrerequisites.Add(prerequisite);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemovePrerequisiteAsync(int subjectId, int prerequisiteSubjectId, CancellationToken ct = default)
    {
        await db.SubjectPrerequisites
            .Where(sp => sp.SubjectId == subjectId && sp.PrerequisiteSubjectId == prerequisiteSubjectId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<Subject> CreateAsync(Subject subject, CancellationToken ct = default)
    {
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(ct);
        return subject;
    }

    public async Task<Subject> UpdateAsync(Subject subject, CancellationToken ct = default)
    {
        db.Subjects.Update(subject);
        await db.SaveChangesAsync(ct);
        return subject;
    }

    public async Task DeleteAsync(Subject subject, CancellationToken ct = default)
    {
        db.Subjects.Remove(subject);
        await db.SaveChangesAsync(ct);
    }
}
