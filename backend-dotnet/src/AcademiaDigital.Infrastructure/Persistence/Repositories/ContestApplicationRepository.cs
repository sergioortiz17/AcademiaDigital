using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class ContestApplicationRepository(AppDbContext db) : IContestApplicationRepository
{
    public async Task<IEnumerable<ContestApplication>> GetByContestAsync(int contestId, CancellationToken ct = default)
        => await db.ContestApplications.AsNoTracking()
            .Include(ca => ca.Applicant)
            .Where(ca => ca.ContestId == contestId)
            .OrderBy(ca => ca.ApplicationDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<ContestApplication>> GetByApplicantAsync(long applicantId, CancellationToken ct = default)
        => await db.ContestApplications.AsNoTracking()
            .Include(ca => ca.Contest)
            .Where(ca => ca.ApplicantId == applicantId)
            .OrderByDescending(ca => ca.ApplicationDate)
            .ToListAsync(ct);

    public async Task<ContestApplication?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.ContestApplications.AsNoTracking()
            .Include(ca => ca.Contest)
            .Include(ca => ca.Applicant)
            .FirstOrDefaultAsync(ca => ca.Id == id, ct);

    public async Task<ContestApplication?> FindByContestAndApplicantAsync(int contestId, long applicantId, CancellationToken ct = default)
        => await db.ContestApplications.AsNoTracking()
            .FirstOrDefaultAsync(ca => ca.ContestId == contestId && ca.ApplicantId == applicantId, ct);

    public async Task<ContestApplication> CreateAsync(ContestApplication application, CancellationToken ct = default)
    {
        db.ContestApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task<ContestApplication> UpdateAsync(ContestApplication application, CancellationToken ct = default)
    {
        db.ContestApplications.Update(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task DeleteAsync(ContestApplication application, CancellationToken ct = default)
    {
        db.ContestApplications.Remove(application);
        await db.SaveChangesAsync(ct);
    }
}
