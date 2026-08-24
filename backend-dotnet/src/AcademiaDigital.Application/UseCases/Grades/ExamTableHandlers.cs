using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Grades;

public sealed record ExamTribunalInput(long TeacherId, ExamTribunalRole Role);
public sealed record ExamResultInput(long RegistrationId, ExamResultOutcome Outcome, decimal? Grade, string? Notes);
public sealed record GetExamTablesQuery(int? AcademicYear, int? CourseId, long ActorUserId, bool IsAdmin);
public sealed record GetExamTableQuery(long ExamTableId, long ActorUserId, bool IsAdmin);
public sealed record CreateExamTableCommand(
    string IdempotencyKey,
    int CourseId,
    int AcademicYear,
    int CallNumber,
    DateTime ExamDateUtc,
    DateTime RegistrationDeadlineUtc,
    string Location,
    IReadOnlyList<ExamTribunalInput> Tribunal,
    long ActorUserId);
public sealed record RegisterForExamCommand(long ExamTableId, long EnrollmentId, long ActorUserId, bool IsAdmin);
public sealed record StartExamGradingCommand(long ExamTableId, long ActorUserId);
public sealed record SaveExamResultsCommand(long ExamTableId, IReadOnlyList<ExamResultInput> Results, long ActorUserId, bool IsAdmin);
public sealed record PublishExamTableCommand(long ExamTableId, long ActorUserId);
public sealed record ReopenExamTableCommand(long ExamTableId, string Reason, long ActorUserId);
public sealed record GetMyExamTablesQuery(long UserId);

public sealed record ExamTribunalMemberDto(long TeacherId, string EmployeeNumber, string TeacherName, ExamTribunalRole Role);
public sealed record ExamGradeDto(long RevisionId, int Version, ExamResultOutcome Outcome, decimal? Grade, string? Notes, DateTime CreatedAt);
public sealed record ExamRegistrationDto(
    long Id,
    long EnrollmentId,
    long StudentId,
    string StudentName,
    string LegajoNumber,
    int AttemptNumber,
    DateTime RegisteredAt,
    ExamGradeDto? Result);
public sealed record ExamTableDto(
    long Id,
    string IdempotencyKey,
    int CourseId,
    string CourseCode,
    string CourseName,
    int AcademicYear,
    int CallNumber,
    DateTime ExamDateUtc,
    DateTime RegistrationDeadlineUtc,
    string Location,
    ExamTableStatus Status,
    int TribunalCount,
    int RegistrationCount,
    int ReopeningCount,
    DateTime CreatedAt,
    DateTime? GradingStartedAt,
    DateTime? PublishedAt);
public sealed record ExamTableDetailDto(
    ExamTableDto ExamTable,
    IReadOnlyList<ExamTribunalMemberDto> Tribunal,
    IReadOnlyList<ExamRegistrationDto> Registrations);
public sealed record StudentExamTableDto(
    ExamTableDto ExamTable,
    bool CanRegister,
    long? RegistrationId,
    int? AttemptNumber,
    ExamGradeDto? Result);

public sealed class GetExamTablesQueryHandler(IExamTableRepository repository)
{
    public async Task<IReadOnlyList<ExamTableDto>> Handle(GetExamTablesQuery query, CancellationToken ct = default)
        => (await repository.GetAsync(
                query.AcademicYear, query.CourseId, query.IsAdmin ? null : query.ActorUserId, ct))
            .Select(ExamTableMapper.MapSummary)
            .ToArray();
}

public sealed class GetExamTableQueryHandler(IExamTableRepository repository)
{
    public async Task<ExamTableDetailDto> Handle(GetExamTableQuery query, CancellationToken ct = default)
    {
        await ExamTableAuthorization.EnsureCanManage(repository, query.ExamTableId, query.ActorUserId, query.IsAdmin, ct);
        return ExamTableMapper.MapDetail(await repository.FindAsync(query.ExamTableId, ct)
            ?? throw new KeyNotFoundException("Exam table not found."));
    }
}

public sealed class CreateExamTableCommandHandler(
    ICourseRepository courseRepository,
    ITeacherRepository teacherRepository,
    IExamTableRepository examTableRepository,
    ExamTablePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ExamTableDto> Handle(CreateExamTableCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 100)
            throw new ArgumentException("A valid idempotency key of up to 100 characters is required.");
        _ = await courseRepository.FindByIdAsync(command.CourseId, ct)
            ?? throw new KeyNotFoundException("Course not found.");
        var tribunal = command.Tribunal.Select(item => new ExamTribunalMember
        {
            TeacherId = item.TeacherId,
            Role = item.Role
        }).ToArray();
        foreach (var member in tribunal)
        {
            var teacher = await teacherRepository.FindByIdAsync(member.TeacherId, ct)
                ?? throw new KeyNotFoundException($"Teacher {member.TeacherId} not found.");
            if (!teacher.IsActive)
                throw new InvalidOperationException("Every tribunal member must be an active teacher.");
        }
        var now = timeProvider.GetUtcNow().UtcDateTime;
        policy.EnsureCanCreate(
            command.AcademicYear, command.CallNumber, command.ExamDateUtc,
            command.RegistrationDeadlineUtc, command.Location, tribunal, now);
        var result = await unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCt => examTableRepository.CreateIdempotentAsync(new ExamTable
            {
                IdempotencyKey = command.IdempotencyKey.Trim(),
                CourseId = command.CourseId,
                AcademicYear = command.AcademicYear,
                CallNumber = command.CallNumber,
                ExamDateUtc = command.ExamDateUtc,
                RegistrationDeadlineUtc = command.RegistrationDeadlineUtc,
                Location = command.Location.Trim(),
                Status = ExamTableStatus.Open,
                CreatedAt = now,
                CreatedByUserId = command.ActorUserId,
                TribunalMembers = tribunal
            }, transactionCt), ct);
        return ExamTableMapper.MapSummary(result.ExamTable);
    }
}

public sealed class RegisterForExamCommandHandler(
    IExamTableRepository repository,
    ExamTablePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ExamRegistrationDto> Handle(RegisterForExamCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var table = await repository.FindForUpdateAsync(command.ExamTableId, transactionCt)
                ?? throw new KeyNotFoundException("Exam table not found.");
            var enrollment = await repository.FindEnrollmentForUpdateAsync(command.EnrollmentId, transactionCt)
                ?? throw new KeyNotFoundException("Enrollment not found.");
            if (!command.IsAdmin && enrollment.Student.UserId != command.ActorUserId)
                throw new ForbiddenException("A student can only register their own enrollment.");
            var now = timeProvider.GetUtcNow().UtcDateTime;
            policy.EnsureCanRegister(table, enrollment, now);
            await repository.RegisterAsync(table, enrollment, command.ActorUserId, now, transactionCt);
            return true;
        }, ct);
        var loaded = await repository.FindAsync(command.ExamTableId, ct)
            ?? throw new KeyNotFoundException("Exam table not found.");
        return ExamTableMapper.MapRegistration(loaded.Registrations.Single(item => item.EnrollmentId == command.EnrollmentId));
    }
}

public sealed class StartExamGradingCommandHandler(
    IExamTableRepository repository,
    ExamTablePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ExamTableDto> Handle(StartExamGradingCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var table = await repository.FindForUpdateAsync(command.ExamTableId, transactionCt)
                ?? throw new KeyNotFoundException("Exam table not found.");
            policy.EnsureCanStartGrading(table);
            table.Status = ExamTableStatus.Grading;
            table.GradingStartedAt = timeProvider.GetUtcNow().UtcDateTime;
            table.GradingStartedByUserId = command.ActorUserId;
            await repository.SaveAsync(table, transactionCt);
            return true;
        }, ct);
        return ExamTableMapper.MapSummary(await repository.FindAsync(command.ExamTableId, ct)
            ?? throw new KeyNotFoundException("Exam table not found."));
    }
}

public sealed class SaveExamResultsCommandHandler(
    IExamTableRepository repository,
    ExamTablePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ExamTableDetailDto> Handle(SaveExamResultsCommand command, CancellationToken ct = default)
    {
        if (command.Results.Count == 0) throw new ArgumentException("At least one exam result is required.");
        if (command.Results.Select(item => item.RegistrationId).Distinct().Count() != command.Results.Count)
            throw new ArgumentException("An exam registration cannot appear more than once.");
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            await ExamTableAuthorization.EnsureCanManage(repository, command.ExamTableId, command.ActorUserId, command.IsAdmin, transactionCt);
            var table = await repository.FindForUpdateAsync(command.ExamTableId, transactionCt)
                ?? throw new KeyNotFoundException("Exam table not found.");
            policy.EnsureCanRecordResults(table);
            var registrations = table.Registrations.ToDictionary(item => item.Id);
            if (command.Results.Any(item => !registrations.ContainsKey(item.RegistrationId)))
                throw new ArgumentException("Every result must belong to the exam table.");
            foreach (var result in command.Results)
            {
                var rule = registrations[result.RegistrationId].Enrollment.StudyPlanCourse?.ApprovalRule;
                policy.EnsureResultIsValid(result.Outcome, result.Grade, rule?.MinimumFinalExamGrade ?? 6m);
            }
            var now = timeProvider.GetUtcNow().UtcDateTime;
            await repository.SaveGradeRevisionsAsync(command.Results.Select(result => new ExamGradeRevision
            {
                ExamRegistrationId = result.RegistrationId,
                IsCurrent = true,
                Outcome = result.Outcome,
                Grade = result.Grade,
                Notes = string.IsNullOrWhiteSpace(result.Notes) ? null : result.Notes.Trim(),
                CreatedAt = now,
                CreatedByUserId = command.ActorUserId
            }).ToArray(), transactionCt);
            return true;
        }, ct);
        return ExamTableMapper.MapDetail(await repository.FindAsync(command.ExamTableId, ct)
            ?? throw new KeyNotFoundException("Exam table not found."));
    }
}

public sealed class PublishExamTableCommandHandler(
    IExamTableRepository repository,
    ExamTablePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ExamTableDto> Handle(PublishExamTableCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var table = await repository.FindForUpdateAsync(command.ExamTableId, transactionCt)
                ?? throw new KeyNotFoundException("Exam table not found.");
            policy.EnsureCanPublish(table);
            table.Status = ExamTableStatus.Published;
            table.PublishedAt = timeProvider.GetUtcNow().UtcDateTime;
            table.PublishedByUserId = command.ActorUserId;
            await repository.PublishAsync(table, transactionCt);
            return true;
        }, ct);
        return ExamTableMapper.MapSummary(await repository.FindAsync(command.ExamTableId, ct)
            ?? throw new KeyNotFoundException("Exam table not found."));
    }
}

public sealed class ReopenExamTableCommandHandler(
    IExamTableRepository repository,
    ExamTablePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ExamTableDto> Handle(ReopenExamTableCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var table = await repository.FindForUpdateAsync(command.ExamTableId, transactionCt)
                ?? throw new KeyNotFoundException("Exam table not found.");
            policy.EnsureCanReopen(table, command.Reason);
            table.Status = ExamTableStatus.Grading;
            table.PublishedAt = null;
            table.PublishedByUserId = null;
            table.Reopenings.Add(new ExamTableReopening
            {
                Reason = command.Reason.Trim(),
                ReopenedAt = timeProvider.GetUtcNow().UtcDateTime,
                ReopenedByUserId = command.ActorUserId
            });
            await repository.SaveAsync(table, transactionCt);
            return true;
        }, ct);
        return ExamTableMapper.MapSummary(await repository.FindAsync(command.ExamTableId, ct)
            ?? throw new KeyNotFoundException("Exam table not found."));
    }
}

public sealed class GetMyExamTablesQueryHandler(
    IStudentRepository studentRepository,
    IExamTableRepository examTableRepository,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<StudentExamTableDto>> Handle(GetMyExamTablesQuery query, CancellationToken ct = default)
    {
        var student = await studentRepository.FindByUserIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("Student profile not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return (await examTableRepository.GetForStudentAsync(student.Id, ct))
            .Select(table => ExamTableMapper.MapStudent(table, student.Id, now))
            .ToArray();
    }
}

internal static class ExamTableAuthorization
{
    public static async Task EnsureCanManage(
        IExamTableRepository repository,
        long examTableId,
        long actorUserId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (!isAdmin && !await repository.CanTeacherManageAsync(actorUserId, examTableId, ct))
            throw new ForbiddenException("The teacher is not a member of this exam tribunal.");
    }
}

internal static class ExamTableMapper
{
    public static ExamTableDto MapSummary(ExamTable item) => new(
        item.Id, item.IdempotencyKey, item.CourseId, item.Course.Code, item.Course.Name,
        item.AcademicYear, item.CallNumber, item.ExamDateUtc, item.RegistrationDeadlineUtc,
        item.Location, item.Status, item.TribunalMembers.Count, item.Registrations.Count,
        item.Reopenings.Count, item.CreatedAt, item.GradingStartedAt, item.PublishedAt);

    public static ExamTableDetailDto MapDetail(ExamTable item) => new(
        MapSummary(item),
        item.TribunalMembers.OrderBy(member => member.Role).ThenBy(member => member.Teacher.EmployeeNumber)
            .Select(member => new ExamTribunalMemberDto(
                member.TeacherId,
                member.Teacher.EmployeeNumber,
                (member.Teacher.User.Username + " " + member.Teacher.User.LastName).Trim(),
                member.Role)).ToArray(),
        item.Registrations.OrderBy(registration => registration.Student.User.LastName)
            .ThenBy(registration => registration.Student.User.Username)
            .Select(MapRegistration).ToArray());

    public static ExamRegistrationDto MapRegistration(ExamRegistration item) => new(
        item.Id, item.EnrollmentId, item.StudentId,
        (item.Student.User.Username + " " + item.Student.User.LastName).Trim(),
        item.Student.LegajoNumber, item.AttemptNumber, item.RegisteredAt,
        MapCurrentResult(item));

    public static StudentExamTableDto MapStudent(ExamTable item, long studentId, DateTime nowUtc)
    {
        var registration = item.Registrations.SingleOrDefault(value => value.StudentId == studentId);
        return new StudentExamTableDto(
            MapSummary(item),
            registration is null && item.Status == ExamTableStatus.Open && nowUtc <= item.RegistrationDeadlineUtc,
            registration?.Id,
            registration?.AttemptNumber,
            registration is null || item.Status != ExamTableStatus.Published ? null : MapCurrentResult(registration));
    }

    private static ExamGradeDto? MapCurrentResult(ExamRegistration item)
    {
        var result = item.GradeRevisions.SingleOrDefault(revision => revision.IsCurrent);
        return result is null ? null : new ExamGradeDto(
            result.Id, result.Version, result.Outcome, result.Grade, result.Notes, result.CreatedAt);
    }
}
