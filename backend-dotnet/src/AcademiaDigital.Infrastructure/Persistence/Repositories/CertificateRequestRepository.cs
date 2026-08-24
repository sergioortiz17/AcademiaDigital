using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CertificateRequestRepository(AppDbContext db) : ICertificateRequestRepository
{
    public async Task<List<CertificateRequest>> GetByUserAsync(long userId, CancellationToken ct = default)
        => await db.CertificateRequests
            .AsNoTracking()
            .Include(c => c.Issuance)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<CertificateRequest>> GetAllAsync(string? search, CertificateStatus? status, CancellationToken ct = default)
    {
        var query = db.CertificateRequests.AsNoTracking().Include(c => c.User).Include(c => c.Issuance).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.User.Username.ToLower().Contains(term) ||
                c.User.Email.ToLower().Contains(term));
        }

        if (status == CertificateStatus.Approved)
            query = query.Where(c => c.Status == CertificateStatus.Approved
                || c.Status == CertificateStatus.Issuing
                || c.Status == CertificateStatus.Issued);
        else if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
    }

    public async Task<CertificateRequest> CreateAsync(CertificateRequest request, CancellationToken ct = default)
    {
        db.CertificateRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request;
    }

    public async Task<CertificateRequest?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.CertificateRequests.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<CertificateRequest?> FindForUpdateAsync(long id, CancellationToken ct = default)
        => await db.CertificateRequests
            .FromSqlInterpolated($"SELECT * FROM [CertificateRequests] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {id}")
            .SingleOrDefaultAsync(ct);

    public Task<bool> HasActiveRequestAsync(
        long userId,
        long studentCareerId,
        CertificateKind kind,
        long? examRegistrationId,
        CancellationToken ct = default)
        => db.CertificateRequests.AsNoTracking().AnyAsync(item =>
            item.UserId == userId
            && item.StudentCareerId == studentCareerId
            && item.Kind == kind
            && item.ExamRegistrationId == examRegistrationId
            && (item.Status == CertificateStatus.Pending
                || item.Status == CertificateStatus.Approved
                || item.Status == CertificateStatus.Issuing), ct);

    public async Task<CertificateAcademicRecord?> GetAcademicRecordAsync(
        long userId,
        long? studentCareerId,
        long? examRegistrationId,
        CancellationToken ct = default)
    {
        var student = await db.Students.AsNoTracking()
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == userId, ct);
        if (student is null) return null;

        var careerQuery = db.StudentCareers.AsNoTracking()
            .Include(item => item.Career)
            .Where(item => item.StudentId == student.Id);
        var career = studentCareerId.HasValue
            ? await careerQuery.SingleOrDefaultAsync(item => item.Id == studentCareerId.Value, ct)
            : await careerQuery.OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.CareerId == student.CareerId)
                .ThenBy(item => item.Id)
                .FirstOrDefaultAsync(ct);
        if (career is null) return null;

        var courses = await db.Enrollments.AsNoTracking()
            .Where(item => item.StudentCareerId == career.Id)
            .Include(item => item.Course)
            .OrderBy(item => item.AcademicYear).ThenBy(item => item.Semester).ThenBy(item => item.Course.Code)
            .Select(item => new CertificateCourseRecord(
                item.CourseId, item.Course.Code, item.Course.Name, item.AcademicYear,
                item.Semester, item.Status, item.FinalGrade))
            .ToArrayAsync(ct);

        CertificateExamRecord? exam = null;
        if (examRegistrationId.HasValue)
        {
            exam = await db.ExamRegistrations.AsNoTracking()
                .Where(item => item.Id == examRegistrationId.Value
                    && item.StudentId == student.Id
                    && item.Enrollment.StudentCareerId == career.Id)
                .Select(item => new CertificateExamRecord(
                    item.Id, item.ExamTable.Course.Code, item.ExamTable.Course.Name,
                    item.ExamTable.ExamDateUtc, item.ExamTable.Location,
                    item.ExamTable.CallNumber, item.ExamTable.Status))
                .SingleOrDefaultAsync(ct);
        }

        var name = $"{student.User.Username} {student.User.LastName}".Trim();
        return new CertificateAcademicRecord(
            student.Id, career.Id, career.IsActive, student.Status, student.LegajoNumber,
            student.User.Dni ?? string.Empty, name, career.Career.Name, courses, exam);
    }

    public Task<CertificateSequence> LockSequenceAsync(CancellationToken ct = default)
        => db.CertificateSequences
            .FromSqlRaw("SELECT * FROM [CertificateSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = 1")
            .SingleAsync(ct);

    public Task<CertificateIssuance?> FindIssuanceByRequestAsync(long requestId, bool tracking, CancellationToken ct = default)
    {
        var query = db.CertificateIssuances.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.CertificateRequestId == requestId, ct);
    }

    public Task<CertificateIssuance?> FindIssuanceByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        => db.CertificateIssuances.AsNoTracking()
            .Include(item => item.CertificateRequest)
            .SingleOrDefaultAsync(item => item.PublicId == publicId, ct);

    public async Task<IReadOnlyList<CertificateIssuance>> GetHistoryByUserAsync(long userId, CancellationToken ct = default)
        => await IssuanceDetails().Where(item => item.CertificateRequest.UserId == userId)
            .OrderByDescending(item => item.CreatedAt).ToArrayAsync(ct);

    public async Task<IReadOnlyList<CertificateIssuance>> GetHistoryByStudentAsync(long studentId, CancellationToken ct = default)
        => await IssuanceDetails().Where(item => db.Students.Any(student =>
                student.Id == studentId && student.UserId == item.CertificateRequest.UserId))
            .OrderByDescending(item => item.CreatedAt).ToArrayAsync(ct);

    public void AddIssuance(CertificateIssuance issuance) => db.CertificateIssuances.Add(issuance);

    private IQueryable<CertificateIssuance> IssuanceDetails()
        => db.CertificateIssuances.AsNoTracking()
            .Include(item => item.CertificateRequest)
            .ThenInclude(item => item.User);
}
