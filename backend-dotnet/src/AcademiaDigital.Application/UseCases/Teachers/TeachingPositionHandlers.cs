using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Teachers;

public sealed record GetTeachingPositionsQuery(
    int? AcademicYear,
    int? Semester,
    bool? IsVacant,
    bool IncludeInactive);

public sealed record GetTeachingPositionByIdQuery(int TeachingPositionId);

public sealed record CreateTeachingPositionCommand(
    int CourseId,
    int CommissionId,
    int AcademicYear,
    int Semester,
    PositionType PositionType,
    int MaxStudents);

public sealed record UpdateTeachingPositionCommand(
    int TeachingPositionId,
    int CourseId,
    int CommissionId,
    int AcademicYear,
    int Semester,
    PositionType PositionType,
    int MaxStudents);

public sealed record DeactivateTeachingPositionCommand(
    int TeachingPositionId,
    long ActorUserId,
    string Reason);

public sealed record TeachingPositionDto(
    int Id,
    int CourseId,
    string CourseCode,
    string CourseName,
    int? CommissionId,
    string? CommissionCode,
    string? CommissionName,
    int AcademicYear,
    int Semester,
    string PositionType,
    int MaxStudents,
    bool IsVacant,
    bool IsActive,
    long? TeacherId,
    string? TeacherName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeactivatedAt,
    long? DeactivatedByUserId,
    string? DeactivationReason);

public sealed class GetTeachingPositionsQueryHandler(ITeachingPositionRepository repository)
{
    public async Task<IReadOnlyList<TeachingPositionDto>> Handle(
        GetTeachingPositionsQuery query,
        CancellationToken ct = default)
        => (await repository.GetAllAsync(
                query.AcademicYear, query.Semester, query.IsVacant, query.IncludeInactive, ct))
            .Select(TeachingPositionMapper.Map)
            .ToArray();
}

public sealed class GetTeachingPositionByIdQueryHandler(ITeachingPositionRepository repository)
{
    public async Task<TeachingPositionDto> Handle(
        GetTeachingPositionByIdQuery query,
        CancellationToken ct = default)
        => TeachingPositionMapper.Map(await repository.FindByIdAsync(query.TeachingPositionId, ct)
            ?? throw new KeyNotFoundException("Teaching position not found."));
}

public sealed class CreateTeachingPositionCommandHandler(
    ITeachingPositionRepository repository,
    ICourseRepository courseRepository,
    ICommissionRepository commissionRepository,
    TeachingAssignmentPolicy policy,
    TimeProvider timeProvider)
{
    public async Task<TeachingPositionDto> Handle(
        CreateTeachingPositionCommand command,
        CancellationToken ct = default)
    {
        var course = await courseRepository.FindByIdAsync(command.CourseId, ct)
            ?? throw new KeyNotFoundException("Course not found.");
        var commission = await commissionRepository.FindByIdAsync(command.CommissionId, ct)
            ?? throw new KeyNotFoundException("Commission not found.");
        policy.ValidatePositionDefinition(
            command.AcademicYear, command.Semester, command.MaxStudents, course, commission);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var created = await repository.CreateAsync(new TeachingPosition
        {
            CourseId = command.CourseId,
            CommissionId = command.CommissionId,
            AcademicYear = command.AcademicYear,
            Semester = command.Semester,
            PositionType = command.PositionType,
            MaxStudents = command.MaxStudents,
            IsVacant = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        }, ct);
        created.Course = course;
        created.Commission = commission;
        return TeachingPositionMapper.Map(created);
    }
}

public sealed class UpdateTeachingPositionCommandHandler(
    ITeachingPositionRepository repository,
    ITeacherAssignmentRepository assignmentRepository,
    ICourseRepository courseRepository,
    ICommissionRepository commissionRepository,
    TeachingAssignmentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<TeachingPositionDto> Handle(
        UpdateTeachingPositionCommand command,
        CancellationToken ct = default)
        => unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var position = await repository.FindByIdAsync(command.TeachingPositionId, transactionCt)
                ?? throw new KeyNotFoundException("Teaching position not found.");
            var course = await courseRepository.FindByIdAsync(command.CourseId, transactionCt)
                ?? throw new KeyNotFoundException("Course not found.");
            var commission = await commissionRepository.FindByIdAsync(command.CommissionId, transactionCt)
                ?? throw new KeyNotFoundException("Commission not found.");
            policy.ValidatePositionDefinition(
                command.AcademicYear, command.Semester, command.MaxStudents, course, commission);
            policy.EnsurePositionCanChange(position,
                await assignmentRepository.HasHistoryForPositionAsync(position.Id, transactionCt));

            position.CourseId = command.CourseId;
            position.Course = course;
            position.CommissionId = command.CommissionId;
            position.Commission = commission;
            position.AcademicYear = command.AcademicYear;
            position.Semester = command.Semester;
            position.PositionType = command.PositionType;
            position.MaxStudents = command.MaxStudents;
            position.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            return TeachingPositionMapper.Map(await repository.UpdateAsync(position, transactionCt));
        }, ct);
}

public sealed class DeactivateTeachingPositionCommandHandler(
    ITeachingPositionRepository repository,
    TeachingAssignmentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task Handle(DeactivateTeachingPositionCommand command, CancellationToken ct = default)
        => unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var position = await repository.FindByIdAsync(command.TeachingPositionId, transactionCt)
                ?? throw new KeyNotFoundException("Teaching position not found.");
            policy.EnsureCanDeactivate(position);
            if (!position.IsActive) return true;
            if (string.IsNullOrWhiteSpace(command.Reason))
                throw new ArgumentException("A deactivation reason is required.");
            var now = timeProvider.GetUtcNow().UtcDateTime;
            position.IsActive = false;
            position.DeactivatedAt = now;
            position.DeactivatedByUserId = command.ActorUserId;
            position.DeactivationReason = command.Reason.Trim();
            position.UpdatedAt = now;
            await repository.DeactivateAsync(position, transactionCt);
            return true;
        }, ct);
}

internal static class TeachingPositionMapper
{
    public static TeachingPositionDto Map(TeachingPosition position) => new(
        position.Id,
        position.CourseId,
        position.Course.Code,
        position.Course.Name,
        position.CommissionId,
        position.Commission?.Code,
        position.Commission?.Name,
        position.AcademicYear,
        position.Semester,
        position.PositionType.ToString(),
        position.MaxStudents,
        position.IsVacant,
        position.IsActive,
        position.TeacherId,
        position.Teacher is null ? null : $"{position.Teacher.User.Username} {position.Teacher.User.LastName}".Trim(),
        position.CreatedAt,
        position.UpdatedAt,
        position.DeactivatedAt,
        position.DeactivatedByUserId,
        position.DeactivationReason);
}
