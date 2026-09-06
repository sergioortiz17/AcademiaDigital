using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Teachers;

public sealed record GetCourseSectionsQuery(
    int? AcademicYear,
    int? Semester,
    bool? IsVacant,
    bool IncludeInactive);

public sealed record GetCourseSectionByIdQuery(int CourseSectionId);

public sealed record CreateCourseSectionCommand(
    int CourseId,
    int DivisionId,
    int AcademicYear,
    int Semester,
    PositionType PositionType,
    int MaxStudents);

public sealed record UpdateCourseSectionCommand(
    int CourseSectionId,
    int CourseId,
    int DivisionId,
    int AcademicYear,
    int Semester,
    PositionType PositionType,
    int MaxStudents);

public sealed record DeactivateCourseSectionCommand(
    int CourseSectionId,
    long ActorUserId,
    string Reason);

public sealed record CourseSectionDto(
    int Id,
    int CourseId,
    string CourseCode,
    string CourseName,
    int? DivisionId,
    string? DivisionCode,
    string? DivisionName,
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

public sealed class GetCourseSectionsQueryHandler(ICourseSectionRepository repository)
{
    public async Task<IReadOnlyList<CourseSectionDto>> Handle(
        GetCourseSectionsQuery query,
        CancellationToken ct = default)
        => (await repository.GetAllAsync(
                query.AcademicYear, query.Semester, query.IsVacant, query.IncludeInactive, ct))
            .Select(CourseSectionMapper.Map)
            .ToArray();
}

public sealed class GetCourseSectionByIdQueryHandler(ICourseSectionRepository repository)
{
    public async Task<CourseSectionDto> Handle(
        GetCourseSectionByIdQuery query,
        CancellationToken ct = default)
        => CourseSectionMapper.Map(await repository.FindByIdAsync(query.CourseSectionId, ct)
            ?? throw new KeyNotFoundException("Cargo docente no encontrado."));
}

public sealed class CreateCourseSectionCommandHandler(
    ICourseSectionRepository repository,
    ICourseRepository courseRepository,
    IDivisionRepository commissionRepository,
    TeachingAssignmentPolicy policy,
    TimeProvider timeProvider)
{
    public async Task<CourseSectionDto> Handle(
        CreateCourseSectionCommand command,
        CancellationToken ct = default)
    {
        var course = await courseRepository.FindByIdAsync(command.CourseId, ct)
            ?? throw new KeyNotFoundException("Materia no encontrada.");
        var commission = await commissionRepository.FindByIdAsync(command.DivisionId, ct)
            ?? throw new KeyNotFoundException("Comisión no encontrada.");
        policy.ValidatePositionDefinition(
            command.AcademicYear, command.Semester, command.MaxStudents, course, commission);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var created = await repository.CreateAsync(new CourseSection
        {
            CourseId = command.CourseId,
            DivisionId = command.DivisionId,
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
        created.Division = commission;
        return CourseSectionMapper.Map(created);
    }
}

public sealed class UpdateCourseSectionCommandHandler(
    ICourseSectionRepository repository,
    ITeacherAssignmentRepository assignmentRepository,
    ICourseRepository courseRepository,
    IDivisionRepository commissionRepository,
    TeachingAssignmentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<CourseSectionDto> Handle(
        UpdateCourseSectionCommand command,
        CancellationToken ct = default)
        => unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var position = await repository.FindByIdAsync(command.CourseSectionId, transactionCt)
                ?? throw new KeyNotFoundException("Cargo docente no encontrado.");
            var course = await courseRepository.FindByIdAsync(command.CourseId, transactionCt)
                ?? throw new KeyNotFoundException("Materia no encontrada.");
            var commission = await commissionRepository.FindByIdAsync(command.DivisionId, transactionCt)
                ?? throw new KeyNotFoundException("Comisión no encontrada.");
            policy.ValidatePositionDefinition(
                command.AcademicYear, command.Semester, command.MaxStudents, course, commission);
            policy.EnsurePositionCanChange(position,
                await assignmentRepository.HasHistoryForPositionAsync(position.Id, transactionCt));

            position.CourseId = command.CourseId;
            position.Course = course;
            position.DivisionId = command.DivisionId;
            position.Division = commission;
            position.AcademicYear = command.AcademicYear;
            position.Semester = command.Semester;
            position.PositionType = command.PositionType;
            position.MaxStudents = command.MaxStudents;
            position.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            return CourseSectionMapper.Map(await repository.UpdateAsync(position, transactionCt));
        }, ct);
}

public sealed class DeactivateCourseSectionCommandHandler(
    ICourseSectionRepository repository,
    TeachingAssignmentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task Handle(DeactivateCourseSectionCommand command, CancellationToken ct = default)
        => unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var position = await repository.FindByIdAsync(command.CourseSectionId, transactionCt)
                ?? throw new KeyNotFoundException("Cargo docente no encontrado.");
            policy.EnsureCanDeactivate(position);
            if (!position.IsActive) return true;
            if (string.IsNullOrWhiteSpace(command.Reason))
                throw new ArgumentException("Se requiere un motivo de desactivación.");
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

internal static class CourseSectionMapper
{
    public static CourseSectionDto Map(CourseSection position) => new(
        position.Id,
        position.CourseId,
        position.Course.Code,
        position.Course.Name,
        position.DivisionId,
        position.Division?.Code,
        position.Division?.Name,
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
