using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Enrollments;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed class EnrollmentPeriodDto
{
    public int Id { get; set; }
    public int CareerId { get; set; }
    public string CareerName { get; set; } = null!;
    public int StudyPlanId { get; set; }
    public string StudyPlanName { get; set; } = null!;
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public int QuotasMorning { get; set; }
    public int QuotasAfternoon { get; set; }
    public int QuotasEvening { get; set; }
    public int EnrolledMorning { get; set; }
    public int EnrolledAfternoon { get; set; }
    public int EnrolledEvening { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class EnrolledStudentDto
{
    public long StudentId { get; set; }
    public string FullName { get; set; } = null!;
    public string Dni { get; set; } = null!;
    public string? Shift { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public IReadOnlyList<string> CourseNames { get; set; } = [];
}

// ── Queries / Commands ────────────────────────────────────────────────────────

public sealed record GetAllEnrollmentPeriodsQuery();
public sealed record GetActiveEnrollmentPeriodQuery(int CareerId);
public sealed record GetEnrolledStudentsQuery(int PeriodId);
public sealed record OpenEnrollmentPeriodCommand(
    int CareerId,
    int StudyPlanId,
    int AcademicYear,
    int Semester,
    int QuotasMorning,
    int QuotasAfternoon,
    int QuotasEvening);
public sealed record CloseEnrollmentPeriodCommand(int PeriodId);

// ── Handlers ─────────────────────────────────────────────────────────────────

public sealed class GetAllEnrollmentPeriodsQueryHandler(IEnrollmentPeriodRepository repository)
{
    public async Task<IReadOnlyList<EnrollmentPeriodDto>> Handle(GetAllEnrollmentPeriodsQuery query, CancellationToken ct = default)
    {
        var periods = (await repository.GetAllAsync(ct)).ToList();
        var counts = await repository.GetAllEnrolledShiftCountsAsync(periods.Select(p => p.Id), ct);
        return periods.Select(p => Mapper.Map(p, counts[p.Id])).ToList();
    }
}

public sealed class GetActiveEnrollmentPeriodQueryHandler(IEnrollmentPeriodRepository repository)
{
    public async Task<EnrollmentPeriodDto?> Handle(GetActiveEnrollmentPeriodQuery query, CancellationToken ct = default)
    {
        var period = await repository.GetActiveByCareerAsync(query.CareerId, ct);
        if (period is null) return null;
        var counts = await repository.GetEnrolledShiftCountsAsync(period.Id, ct);
        return Mapper.Map(period, counts);
    }
}

public sealed class GetEnrolledStudentsQueryHandler(
    IEnrollmentPeriodRepository periodRepository,
    IEnrollmentRepository enrollmentRepository)
{
    public async Task<(int Total, IReadOnlyList<EnrolledStudentDto> Students)> Handle(GetEnrolledStudentsQuery query, CancellationToken ct = default)
    {
        _ = await periodRepository.FindByIdAsync(query.PeriodId, ct)
            ?? throw new KeyNotFoundException("Enrollment period not found.");

        // One SQL query projecting only needed columns
        var rows = await enrollmentRepository.GetStudentRowsByPeriodAsync(query.PeriodId, ct);

        // Group by studentId with a dictionary — O(n) single pass
        var index = new Dictionary<long, EnrolledStudentDto>(capacity: rows.Count);
        foreach (var row in rows)
        {
            if (!index.TryGetValue(row.StudentId, out var dto))
            {
                dto = new EnrolledStudentDto
                {
                    StudentId = row.StudentId,
                    FullName = row.FullName,
                    Dni = row.Dni,
                    Shift = row.Shift,
                    EnrollmentDate = row.EnrollmentDate,
                    CourseNames = new List<string>()
                };
                index[row.StudentId] = dto;
            }
            ((List<string>)dto.CourseNames).Add(row.CourseName);
        }

        var students = index.Values.OrderBy(s => s.FullName).ToList();
        return (students.Count, students);
    }
}

public sealed class OpenEnrollmentPeriodCommandHandler(
    IEnrollmentPeriodRepository repository,
    ICareerRepository careerRepository,
    IStudyPlanRepository studyPlanRepository)
{
    public async Task<EnrollmentPeriodDto> Handle(OpenEnrollmentPeriodCommand command, CancellationToken ct = default)
    {
        _ = await careerRepository.FindByIdAsync(command.CareerId, ct)
            ?? throw new KeyNotFoundException("Career not found.");

        _ = await studyPlanRepository.GetByIdAsync(command.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Study plan not found.");

        var existing = await repository.GetActiveByCareerAsync(command.CareerId, ct);
        if (existing is not null && existing.AcademicYear == command.AcademicYear && existing.Semester == command.Semester)
            throw new InvalidOperationException("There is already an active enrollment period for this career, year and semester.");

        var period = new EnrollmentPeriod
        {
            CareerId = command.CareerId,
            StudyPlanId = command.StudyPlanId,
            AcademicYear = command.AcademicYear,
            Semester = command.Semester,
            QuotasMorning = command.QuotasMorning,
            QuotasAfternoon = command.QuotasAfternoon,
            QuotasEvening = command.QuotasEvening,
            IsActive = true,
            StartDate = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(period, ct);
        return Mapper.Map(created, (0, 0, 0));
    }
}

public sealed class CloseEnrollmentPeriodCommandHandler(IEnrollmentPeriodRepository repository)
{
    public async Task Handle(CloseEnrollmentPeriodCommand command, CancellationToken ct = default)
    {
        var period = await repository.FindByIdAsync(command.PeriodId, ct)
            ?? throw new KeyNotFoundException("Enrollment period not found.");

        period.IsActive = false;
        period.EndDate = DateTime.UtcNow;
        period.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(period, ct);
    }
}

public sealed record UpdatePeriodQuotasCommand(int PeriodId, int QuotasMorning, int QuotasAfternoon, int QuotasEvening);
public sealed record RemoveStudentFromPeriodCommand(int PeriodId, long StudentId);

public sealed class UpdatePeriodQuotasCommandHandler(IEnrollmentPeriodRepository repository)
{
    public async Task<EnrollmentPeriodDto> Handle(UpdatePeriodQuotasCommand command, CancellationToken ct = default)
    {
        var period = await repository.FindByIdAsync(command.PeriodId, ct)
            ?? throw new KeyNotFoundException("Enrollment period not found.");

        period.QuotasMorning = command.QuotasMorning;
        period.QuotasAfternoon = command.QuotasAfternoon;
        period.QuotasEvening = command.QuotasEvening;
        period.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(period, ct);

        var counts = await repository.GetEnrolledShiftCountsAsync(period.Id, ct);
        return Mapper.Map(period, counts);
    }
}

public sealed class RemoveStudentFromPeriodCommandHandler(
    IEnrollmentPeriodRepository periodRepository,
    IEnrollmentRepository enrollmentRepository)
{
    public async Task Handle(RemoveStudentFromPeriodCommand command, CancellationToken ct = default)
    {
        _ = await periodRepository.FindByIdAsync(command.PeriodId, ct)
            ?? throw new KeyNotFoundException("Enrollment period not found.");

        await enrollmentRepository.DeleteByStudentAndPeriodAsync(command.StudentId, command.PeriodId, ct);
    }
}

// ── Mapping ───────────────────────────────────────────────────────────────────

file static class Mapper
{
    internal static EnrollmentPeriodDto Map(EnrollmentPeriod p, (int Morning, int Afternoon, int Evening) enrolled) => new()
    {
        Id = p.Id,
        CareerId = p.CareerId,
        CareerName = p.Career.Name,
        StudyPlanId = p.StudyPlanId,
        StudyPlanName = p.StudyPlan.Name,
        AcademicYear = p.AcademicYear,
        Semester = p.Semester,
        QuotasMorning = p.QuotasMorning,
        QuotasAfternoon = p.QuotasAfternoon,
        QuotasEvening = p.QuotasEvening,
        EnrolledMorning = enrolled.Morning,
        EnrolledAfternoon = enrolled.Afternoon,
        EnrolledEvening = enrolled.Evening,
        IsActive = p.IsActive,
        StartDate = p.StartDate,
        EndDate = p.EndDate
    };
}
