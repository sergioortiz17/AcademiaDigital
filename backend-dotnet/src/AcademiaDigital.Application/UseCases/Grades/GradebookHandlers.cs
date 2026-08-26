using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Grades;

public sealed record GradebookEvaluationInput(string Name, decimal WeightPercentage, decimal MaximumScore = 10m);
public sealed record GradeEntryInput(long EvaluationId, long EnrollmentId, decimal Score, string? Notes);
public sealed record GetGradebooksQuery(int? AcademicYear, int? CourseId, int? CommissionId, long ActorUserId, bool IsAdmin);
public sealed record GetGradebookQuery(long GradebookId, long ActorUserId, bool IsAdmin);
public sealed record CreateGradebookCommand(
    string IdempotencyKey,
    int TeachingPositionId,
    IReadOnlyList<GradebookEvaluationInput> Evaluations,
    long ActorUserId,
    bool IsAdmin);
public sealed record SaveGradeEntriesCommand(long GradebookId, IReadOnlyList<GradeEntryInput> Grades, long ActorUserId, bool IsAdmin);
public sealed record SubmitGradebookCommand(long GradebookId, long ActorUserId, bool IsAdmin);
public sealed record ApproveGradebookCommand(long GradebookId, long ActorUserId);
public sealed record PublishGradebookCommand(long GradebookId, long ActorUserId);
public sealed record CloseGradebookCommand(long GradebookId, long ActorUserId);
public sealed record ReopenGradebookCommand(long GradebookId, string Reason, long ActorUserId);
public sealed record GetMyGradesQuery(long UserId, int? CourseId);

public sealed record GradebookEvaluationDto(long Id, string Name, decimal WeightPercentage, decimal MaximumScore, int DisplayOrder);
public sealed record GradeEntryDto(long? RevisionId, long EvaluationId, decimal? Score, int? Version, string? Notes, DateTime? UpdatedAt);
public sealed record GradebookStudentDto(
    long EnrollmentId,
    long StudentId,
    string StudentName,
    string LegajoNumber,
    string Dni,
    IReadOnlyList<GradeEntryDto> Grades,
    decimal? Average,
    string? ResultStatus);
public sealed record GradebookDto(
    long Id,
    string IdempotencyKey,
    int TeachingPositionId,
    int CourseId,
    string CourseCode,
    string CourseName,
    int CommissionId,
    string CommissionCode,
    string CommissionName,
    int AcademicYear,
    int Semester,
    GradebookStatus Status,
    int EvaluationCount,
    int CurrentGradeCount,
    int ReopeningCount,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DateTime? PublishedAt,
    DateTime? ClosedAt);
public sealed record GradebookDetailDto(
    GradebookDto Gradebook,
    IReadOnlyList<GradebookEvaluationDto> Evaluations,
    IReadOnlyList<GradebookStudentDto> Students);
public sealed record StudentPublishedGradebookDto(
    long GradebookId,
    int CourseId,
    string CourseCode,
    string CourseName,
    int AcademicYear,
    int Semester,
    GradebookStatus Status,
    IReadOnlyList<GradebookEvaluationDto> Evaluations,
    IReadOnlyList<GradeEntryDto> Grades,
    decimal Average,
    string ResultStatus,
    DateTime PublishedAt);

public sealed class GetGradebooksQueryHandler(IGradebookRepository repository)
{
    public async Task<IReadOnlyList<GradebookDto>> Handle(GetGradebooksQuery query, CancellationToken ct = default)
        => (await repository.GetGradebooksAsync(
                query.AcademicYear, query.CourseId, query.CommissionId,
                query.IsAdmin ? null : query.ActorUserId, ct))
            .Select(GradebookMapper.MapSummary)
            .ToArray();
}

public sealed class GetGradebookQueryHandler(IGradebookRepository repository, GradebookPolicy policy)
{
    public async Task<GradebookDetailDto> Handle(GetGradebookQuery query, CancellationToken ct = default)
    {
        await GradebookAuthorization.EnsureCanManage(repository, query.GradebookId, query.ActorUserId, query.IsAdmin, ct);
        return await GradebookMapper.LoadDetail(repository, policy, query.GradebookId, ct);
    }
}

public sealed class CreateGradebookCommandHandler(
    ITeachingPositionRepository positionRepository,
    IGradebookRepository gradebookRepository,
    GradebookPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GradebookDto> Handle(CreateGradebookCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 100)
            throw new ArgumentException("A valid idempotency key of up to 100 characters is required.");
        var position = await positionRepository.FindByIdAsync(command.TeachingPositionId, ct)
            ?? throw new KeyNotFoundException("Teaching position not found.");
        if (!command.IsAdmin && !await gradebookRepository.CanTeacherManagePositionAsync(
                command.ActorUserId, position.Id, ct))
            throw new ForbiddenException("The teacher is not assigned to this course and commission.");
        var evaluations = command.Evaluations.Select((item, index) => new GradebookEvaluation
        {
            Name = item.Name.Trim(),
            WeightPercentage = item.WeightPercentage,
            MaximumScore = item.MaximumScore,
            DisplayOrder = index + 1
        }).ToArray();
        policy.EnsureCanCreate(position, evaluations);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var result = await unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCt => gradebookRepository.CreateIdempotentAsync(new Gradebook
            {
                IdempotencyKey = command.IdempotencyKey.Trim(),
                TeachingPositionId = position.Id,
                CourseId = position.CourseId,
                CommissionId = position.CommissionId!.Value,
                AcademicYear = position.AcademicYear,
                Semester = position.Semester,
                Status = GradebookStatus.Draft,
                CreatedAt = now,
                CreatedByUserId = command.ActorUserId,
                Evaluations = evaluations
            }, transactionCt), ct);
        return GradebookMapper.MapSummary(result.Gradebook);
    }
}

public sealed class SaveGradeEntriesCommandHandler(
    IGradebookRepository repository,
    GradebookPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GradebookDetailDto> Handle(SaveGradeEntriesCommand command, CancellationToken ct = default)
    {
        if (command.Grades.Count == 0) throw new ArgumentException("At least one grade is required.");
        if (command.Grades.Select(item => (item.EvaluationId, item.EnrollmentId)).Distinct().Count() != command.Grades.Count)
            throw new ArgumentException("An evaluation and enrollment pair cannot appear more than once.");
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            await GradebookAuthorization.EnsureCanManage(repository, command.GradebookId, command.ActorUserId, command.IsAdmin, transactionCt);
            var gradebook = await repository.FindForUpdateAsync(command.GradebookId, transactionCt)
                ?? throw new KeyNotFoundException("Gradebook not found.");
            policy.EnsureEditable(gradebook);
            var roster = await repository.GetRosterAsync(gradebook, transactionCt);
            var rosterByEnrollment = roster.ToDictionary(item => item.EnrollmentId);
            var evaluations = gradebook.Evaluations.ToDictionary(item => item.Id);
            if (command.Grades.Any(item => !rosterByEnrollment.ContainsKey(item.EnrollmentId)))
                throw new ArgumentException("Every grade must belong to the gradebook roster.");
            if (command.Grades.Any(item => !evaluations.ContainsKey(item.EvaluationId)))
                throw new ArgumentException("Every grade must belong to a configured evaluation.");
            foreach (var input in command.Grades) policy.EnsureScoreIsValid(evaluations[input.EvaluationId], input.Score);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            await repository.SaveGradeRevisionsAsync(command.Grades.Select(input => new GradeEntryRevision
            {
                GradebookId = gradebook.Id,
                EvaluationId = input.EvaluationId,
                EnrollmentId = input.EnrollmentId,
                StudentId = rosterByEnrollment[input.EnrollmentId].StudentId,
                IsCurrent = true,
                Score = input.Score,
                Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
                CreatedAt = now,
                CreatedByUserId = command.ActorUserId
            }).ToArray(), transactionCt);
            return true;
        }, ct);
        return await GradebookMapper.LoadDetail(repository, policy, command.GradebookId, ct);
    }
}

public sealed class SubmitGradebookCommandHandler(
    IGradebookRepository repository,
    GradebookPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GradebookDto> Handle(SubmitGradebookCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            await GradebookAuthorization.EnsureCanManage(repository, command.GradebookId, command.ActorUserId, command.IsAdmin, transactionCt);
            var gradebook = await repository.FindForUpdateAsync(command.GradebookId, transactionCt)
                ?? throw new KeyNotFoundException("Gradebook not found.");
            var roster = await repository.GetRosterAsync(gradebook, transactionCt);
            policy.EnsureCanSubmit(gradebook, roster.Count);
            gradebook.Status = GradebookStatus.Submitted;
            gradebook.SubmittedAt = timeProvider.GetUtcNow().UtcDateTime;
            gradebook.SubmittedByUserId = command.ActorUserId;
            await repository.SaveAsync(gradebook, transactionCt);
            return true;
        }, ct);
        return GradebookMapper.MapSummary(await repository.FindAsync(command.GradebookId, ct)
            ?? throw new KeyNotFoundException("Gradebook not found."));
    }
}

public sealed class ApproveGradebookCommandHandler(
    IGradebookRepository repository,
    GradebookPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<GradebookDto> Handle(ApproveGradebookCommand command, CancellationToken ct = default)
        => GradebookTransitions.Change(repository, unitOfWork, command.GradebookId, ct, gradebook =>
        {
            policy.EnsureCanApprove(gradebook);
            gradebook.Status = GradebookStatus.Approved;
            gradebook.ApprovedAt = timeProvider.GetUtcNow().UtcDateTime;
            gradebook.ApprovedByUserId = command.ActorUserId;
        });
}

public sealed class PublishGradebookCommandHandler(
    IGradebookRepository repository,
    GradebookPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<GradebookDto> Handle(PublishGradebookCommand command, CancellationToken ct = default)
        => GradebookTransitions.Change(repository, unitOfWork, command.GradebookId, ct, gradebook =>
        {
            policy.EnsureCanPublish(gradebook);
            gradebook.Status = GradebookStatus.Published;
            gradebook.PublishedAt = timeProvider.GetUtcNow().UtcDateTime;
            gradebook.PublishedByUserId = command.ActorUserId;
        });
}

public sealed class CloseGradebookCommandHandler(
    IGradebookRepository repository,
    GradebookPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GradebookDto> Handle(CloseGradebookCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var gradebook = await repository.FindForUpdateAsync(command.GradebookId, transactionCt)
                ?? throw new KeyNotFoundException("Gradebook not found.");
            policy.EnsureCanClose(gradebook);
            var results = gradebook.GradeRevisions.Where(item => item.IsCurrent)
                .GroupBy(item => item.EnrollmentId)
                .Select(group =>
                {
                    var result = policy.CalculateResult(group.Select(item =>
                        (item.Score, item.Evaluation.MaximumScore, item.Evaluation.WeightPercentage)).ToArray(),
                        group.First().Enrollment.StudyPlanCourse?.ApprovalRule);
                    return new EnrollmentGradebookResult(group.Key, result.Average, result.Status);
                }).ToArray();
            gradebook.Status = GradebookStatus.Closed;
            gradebook.ClosedAt = timeProvider.GetUtcNow().UtcDateTime;
            gradebook.ClosedByUserId = command.ActorUserId;
            await repository.ApplyFinalResultsAsync(gradebook, results, transactionCt);
            return true;
        }, ct);
        return GradebookMapper.MapSummary(await repository.FindAsync(command.GradebookId, ct)
            ?? throw new KeyNotFoundException("Gradebook not found."));
    }
}

public sealed class ReopenGradebookCommandHandler(
    IGradebookRepository repository,
    GradebookPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GradebookDto> Handle(ReopenGradebookCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var gradebook = await repository.FindForUpdateAsync(command.GradebookId, transactionCt)
                ?? throw new KeyNotFoundException("Gradebook not found.");
            policy.EnsureCanReopen(gradebook, command.Reason);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            gradebook.Reopenings.Add(new GradebookReopening
            {
                PreviousStatus = gradebook.Status,
                Reason = command.Reason.Trim(),
                ReopenedAt = now,
                ReopenedByUserId = command.ActorUserId
            });
            gradebook.Status = GradebookStatus.Draft;
            gradebook.SubmittedAt = null;
            gradebook.SubmittedByUserId = null;
            gradebook.ApprovedAt = null;
            gradebook.ApprovedByUserId = null;
            gradebook.PublishedAt = null;
            gradebook.PublishedByUserId = null;
            gradebook.ClosedAt = null;
            gradebook.ClosedByUserId = null;
            await repository.SaveAsync(gradebook, transactionCt);
            return true;
        }, ct);
        return GradebookMapper.MapSummary(await repository.FindAsync(command.GradebookId, ct)
            ?? throw new KeyNotFoundException("Gradebook not found."));
    }
}

public sealed class GetMyGradesQueryHandler(
    IStudentRepository studentRepository,
    IGradebookRepository gradebookRepository,
    GradebookPolicy policy)
{
    public async Task<IReadOnlyList<StudentPublishedGradebookDto>> Handle(GetMyGradesQuery query, CancellationToken ct = default)
    {
        var student = await studentRepository.FindByUserIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("Student profile not found.");
        return (await gradebookRepository.GetPublishedForStudentAsync(student.Id, query.CourseId, ct))
            .Select(item => GradebookMapper.MapStudent(item, student.Id, policy))
            .ToArray();
    }
}

internal static class GradebookAuthorization
{
    public static async Task EnsureCanManage(
        IGradebookRepository repository,
        long gradebookId,
        long actorUserId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (!isAdmin && !await repository.CanTeacherManageGradebookAsync(actorUserId, gradebookId, ct))
            throw new ForbiddenException("The teacher cannot manage this gradebook.");
    }
}

internal static class GradebookTransitions
{
    public static async Task<GradebookDto> Change(
        IGradebookRepository repository,
        IUnitOfWork unitOfWork,
        long gradebookId,
        CancellationToken ct,
        Action<Gradebook> transition)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var gradebook = await repository.FindForUpdateAsync(gradebookId, transactionCt)
                ?? throw new KeyNotFoundException("Gradebook not found.");
            transition(gradebook);
            await repository.SaveAsync(gradebook, transactionCt);
            return true;
        }, ct);
        return GradebookMapper.MapSummary(await repository.FindAsync(gradebookId, ct)
            ?? throw new KeyNotFoundException("Gradebook not found."));
    }
}

internal static class GradebookMapper
{
    public static GradebookDto MapSummary(Gradebook item) => new(
        item.Id, item.IdempotencyKey, item.TeachingPositionId,
        item.CourseId, item.Course.Code, item.Course.Name,
        item.CommissionId, item.Commission.Code, item.Commission.Name,
        item.AcademicYear, item.Semester, item.Status,
        item.Evaluations.Count, item.GradeRevisions.Count(revision => revision.IsCurrent), item.Reopenings.Count,
        item.CreatedAt, item.SubmittedAt, item.ApprovedAt, item.PublishedAt, item.ClosedAt);

    public static async Task<GradebookDetailDto> LoadDetail(
        IGradebookRepository repository,
        GradebookPolicy policy,
        long gradebookId,
        CancellationToken ct)
    {
        var gradebook = await repository.FindAsync(gradebookId, ct)
            ?? throw new KeyNotFoundException("Gradebook not found.");
        var roster = await repository.GetRosterAsync(gradebook, ct);
        return new GradebookDetailDto(
            MapSummary(gradebook),
            gradebook.Evaluations.OrderBy(item => item.DisplayOrder).Select(MapEvaluation).ToArray(),
            roster.Select(row => MapStudentRow(gradebook, row, policy)).ToArray());
    }

    public static StudentPublishedGradebookDto MapStudent(Gradebook gradebook, long studentId, GradebookPolicy policy)
    {
        var revisions = gradebook.GradeRevisions.Where(item => item.StudentId == studentId && item.IsCurrent).ToArray();
        var result = policy.CalculateResult(revisions.Select(item =>
            (item.Score, item.Evaluation.MaximumScore, item.Evaluation.WeightPercentage)).ToArray(),
            revisions.First().Enrollment.StudyPlanCourse?.ApprovalRule);
        return new StudentPublishedGradebookDto(
            gradebook.Id, gradebook.CourseId, gradebook.Course.Code, gradebook.Course.Name,
            gradebook.AcademicYear, gradebook.Semester, gradebook.Status,
            gradebook.Evaluations.OrderBy(item => item.DisplayOrder).Select(MapEvaluation).ToArray(),
            gradebook.Evaluations.OrderBy(item => item.DisplayOrder).Select(evaluation =>
            {
                var revision = revisions.Single(item => item.EvaluationId == evaluation.Id);
                return MapGrade(revision);
            }).ToArray(),
            result.Average, result.Status.ToString(), gradebook.PublishedAt!.Value);
    }

    private static GradebookStudentDto MapStudentRow(Gradebook gradebook, GradebookRosterRow row, GradebookPolicy policy)
    {
        var revisions = gradebook.GradeRevisions.Where(item => item.EnrollmentId == row.EnrollmentId && item.IsCurrent)
            .ToDictionary(item => item.EvaluationId);
        var grades = gradebook.Evaluations.OrderBy(item => item.DisplayOrder)
            .Select(evaluation => revisions.TryGetValue(evaluation.Id, out var revision)
                ? MapGrade(revision)
                : new GradeEntryDto(null, evaluation.Id, null, null, null, null))
            .ToArray();
        GradebookResult? result = null;
        if (revisions.Count == gradebook.Evaluations.Count)
            result = policy.CalculateResult(revisions.Values.Select(item =>
                (item.Score, item.Evaluation.MaximumScore, item.Evaluation.WeightPercentage)).ToArray(),
                revisions.Values.First().Enrollment.StudyPlanCourse?.ApprovalRule);
        return new GradebookStudentDto(
            row.EnrollmentId, row.StudentId, row.StudentName, row.LegajoNumber, row.Dni,
            grades, result?.Average, result?.Status.ToString());
    }

    private static GradebookEvaluationDto MapEvaluation(GradebookEvaluation item)
        => new(item.Id, item.Name, item.WeightPercentage, item.MaximumScore, item.DisplayOrder);

    private static GradeEntryDto MapGrade(GradeEntryRevision item)
        => new(item.Id, item.EvaluationId, item.Score, item.Version, item.Notes, item.CreatedAt);
}
