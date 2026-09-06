using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Services;

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
public sealed record GetMyEnrollmentsQuery(long StudentId);
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
            ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");

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
    IStudyPlanRepository studyPlanRepository,
    EnrollmentCapacityPolicy capacityPolicy,
    TimeProvider timeProvider)
{
    public async Task<EnrollmentPeriodDto> Handle(OpenEnrollmentPeriodCommand command, CancellationToken ct = default)
    {
        capacityPolicy.EnsureValidQuotas(
            command.QuotasMorning,
            command.QuotasAfternoon,
            command.QuotasEvening);

        _ = await careerRepository.FindByIdAsync(command.CareerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");

        _ = await studyPlanRepository.GetByIdAsync(command.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Plan de estudios no encontrado.");

        var existing = await repository.GetActiveByCareerAsync(command.CareerId, ct);
        if (existing is not null && existing.AcademicYear == command.AcademicYear && existing.Semester == command.Semester)
            throw new InvalidOperationException("Ya existe un período de inscripción activo para esta carrera, año y cuatrimestre.");

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
            StartDate = timeProvider.GetUtcNow().UtcDateTime
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
            ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");

        period.IsActive = false;
        period.EndDate = DateTime.UtcNow;
        period.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(period, ct);
    }
}

public sealed record UpdatePeriodQuotasCommand(int PeriodId, int QuotasMorning, int QuotasAfternoon, int QuotasEvening);
public sealed record RemoveStudentFromPeriodCommand(int PeriodId, long StudentId);
public sealed record ActivateEnrollmentPeriodCommand(int PeriodId);
public sealed record DeleteEnrollmentPeriodCommand(int PeriodId);
public sealed record GetPeriodReportQuery(int PeriodId);

public sealed class UpdatePeriodQuotasCommandHandler(
    IEnrollmentPeriodRepository repository,
    EnrollmentCapacityPolicy capacityPolicy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<EnrollmentPeriodDto> Handle(UpdatePeriodQuotasCommand command, CancellationToken ct = default)
    {
        capacityPolicy.EnsureValidQuotas(
            command.QuotasMorning,
            command.QuotasAfternoon,
            command.QuotasEvening);

        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var period = await repository.LockForEnrollmentAsync(command.PeriodId, transactionCt)
                ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");
            var counts = await repository.GetEnrolledShiftCountsAsync(period.Id, transactionCt);
            capacityPolicy.EnsureQuotasCoverCurrentEnrollment(
                counts,
                command.QuotasMorning,
                command.QuotasAfternoon,
                command.QuotasEvening);

            period.QuotasMorning = command.QuotasMorning;
            period.QuotasAfternoon = command.QuotasAfternoon;
            period.QuotasEvening = command.QuotasEvening;
            period.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            await repository.UpdateAsync(period, transactionCt);
            return true;
        }, ct);

        var updated = await repository.FindByIdAsync(command.PeriodId, ct)
            ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");
        var updatedCounts = await repository.GetEnrolledShiftCountsAsync(updated.Id, ct);
        return Mapper.Map(updated, updatedCounts);
    }
}

public sealed class MyEnrollmentPeriodDto
{
    public int PeriodId { get; set; }
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public string? Shift { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public IReadOnlyList<string> CourseNames { get; set; } = [];
}

public sealed class GetMyEnrollmentsQueryHandler(IEnrollmentRepository enrollmentRepository)
{
    public async Task<IReadOnlyList<MyEnrollmentPeriodDto>> Handle(GetMyEnrollmentsQuery query, CancellationToken ct = default)
    {
        // Projected query — no TeachingPosition/Teacher/User joins, only course name needed
        var rows = await enrollmentRepository.GetMyEnrollmentRowsAsync(query.StudentId, ct);

        // Group by (year, semester) with dictionary — O(n) single pass
        var index = new Dictionary<(int year, int sem), MyEnrollmentPeriodDto>(capacity: rows.Count);
        foreach (var row in rows)
        {
            var key = (row.AcademicYear, row.Semester);
            if (!index.TryGetValue(key, out var dto))
            {
                dto = new MyEnrollmentPeriodDto
                {
                    PeriodId = row.PeriodId,
                    AcademicYear = row.AcademicYear,
                    Semester = row.Semester,
                    Shift = row.Shift,
                    EnrollmentDate = row.EnrollmentDate,
                    CourseNames = new List<string>()
                };
                index[key] = dto;
            }
            ((List<string>)dto.CourseNames).Add(row.CourseName);
        }

        return index.Values
            .OrderByDescending(d => d.AcademicYear)
            .ThenByDescending(d => d.Semester)
            .ToList();
    }
}

public sealed class RemoveStudentFromPeriodCommandHandler(
    IEnrollmentPeriodRepository periodRepository,
    IEnrollmentRepository enrollmentRepository)
{
    public async Task Handle(RemoveStudentFromPeriodCommand command, CancellationToken ct = default)
    {
        _ = await periodRepository.FindByIdAsync(command.PeriodId, ct)
            ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");

        await enrollmentRepository.DeleteByStudentAndPeriodAsync(command.StudentId, command.PeriodId, ct);
    }
}

public sealed class ActivateEnrollmentPeriodCommandHandler(IEnrollmentPeriodRepository repository)
{
    public async Task Handle(ActivateEnrollmentPeriodCommand command, CancellationToken ct = default)
    {
        var period = await repository.FindByIdAsync(command.PeriodId, ct)
            ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");

        period.IsActive = true;
        period.EndDate = null;
        period.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(period, ct);
    }
}

public sealed class DeleteEnrollmentPeriodCommandHandler(IEnrollmentPeriodRepository repository)
{
    public async Task Handle(DeleteEnrollmentPeriodCommand command, CancellationToken ct = default)
        => await repository.DeleteAsync(command.PeriodId, ct);
}

public sealed class PeriodReportDto
{
    public IReadOnlyList<GenderReportItem> GenderCounts { get; set; } = [];
    public IReadOnlyList<CourseReportItem> CourseCounts { get; set; } = [];
    public IReadOnlyList<DailyReportItem> DailyCounts { get; set; } = [];
}
public sealed record GenderReportItem(string Gender, int Count);
public sealed record CourseReportItem(string CourseName, int StudentCount);
public sealed record DailyReportItem(string Date, int StudentCount);

public sealed class GetPeriodReportQueryHandler(IEnrollmentRepository enrollmentRepository)
{
    public async Task<PeriodReportDto> Handle(GetPeriodReportQuery query, CancellationToken ct = default)
    {
        var genders = await enrollmentRepository.GetGenderCountsByPeriodAsync(query.PeriodId, ct);
        var courses = await enrollmentRepository.GetCourseCountsByPeriodAsync(query.PeriodId, ct);
        var daily = await enrollmentRepository.GetDailyCountsByPeriodAsync(query.PeriodId, 14, ct);

        return new PeriodReportDto
        {
            GenderCounts = genders.Select(g => new GenderReportItem(g.Gender, g.Count)).ToList(),
            CourseCounts = courses.Select(c => new CourseReportItem(c.CourseName, c.StudentCount)).ToList(),
            DailyCounts = daily.Select(d => new DailyReportItem(d.Date.ToString("dd/MM"), d.StudentCount)).ToList()
        };
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
