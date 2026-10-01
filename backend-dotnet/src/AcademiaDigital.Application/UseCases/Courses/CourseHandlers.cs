using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Courses;

public sealed record GetCareerCoursesQuery(int CareerId);
public sealed record CreateCourseCommand(int CareerId, UpsertCourseRequest Request);
public sealed record UpdateCourseCommand(int CareerId, int CourseId, UpsertCourseRequest Request);
public sealed record DeleteCourseCommand(int CareerId, int CourseId);

public sealed class GetCareerCoursesQueryHandler(ICourseRepository courseRepository)
{
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCareerCoursesQuery query, CancellationToken ct = default)
    {
        var courses = await courseRepository.GetByCareerAsync(query.CareerId, ct);
        return courses.Select(Map).ToList();
    }

    private static CourseDto Map(Course course) => new()
    {
        Id = course.Id,
        CareerId = course.CareerId,
        Code = course.Code,
        Name = course.Name,
        Description = course.Description,
        IsActive = course.IsActive
    };
}

public sealed class CreateCourseCommandHandler(
    ICareerRepository careerRepository,
    ICourseRepository courseRepository)
{
    public async Task<CourseDto> Handle(CreateCourseCommand command, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(command.CareerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");

        var existing = await courseRepository.FindByCodeAsync(career.Id, command.Request.Code.Trim(), ct);
        if (existing is not null) throw new InvalidOperationException("El código de la materia ya existe en esta carrera.");

        var course = new Course
        {
            CareerId = career.Id,
            Code = command.Request.Code.Trim(),
            Name = command.Request.Name.Trim(),
            Description = command.Request.Description?.Trim(),
            IsActive = command.Request.IsActive
        };

        var created = await courseRepository.CreateAsync(course, ct);
        return Map(created);
    }

    private static CourseDto Map(Course course) => new()
    {
        Id = course.Id,
        CareerId = course.CareerId,
        Code = course.Code,
        Name = course.Name,
        Description = course.Description,
        IsActive = course.IsActive
    };
}

public sealed class UpdateCourseCommandHandler(ICourseRepository courseRepository)
{
    public async Task Handle(UpdateCourseCommand command, CancellationToken ct = default)
    {
        var course = await courseRepository.FindByIdAsync(command.CourseId, ct)
            ?? throw new KeyNotFoundException("Materia no encontrada.");

        if (course.CareerId != command.CareerId) throw new KeyNotFoundException("Materia no encontrada en esta carrera.");

        course.Code = command.Request.Code.Trim();
        course.Name = command.Request.Name.Trim();
        course.Description = command.Request.Description?.Trim();
        course.IsActive = command.Request.IsActive;
        course.UpdatedAt = DateTime.UtcNow;

        await courseRepository.UpdateAsync(course, ct);
    }
}

public sealed class DeleteCourseCommandHandler(ICourseRepository courseRepository)
{
    public async Task Handle(DeleteCourseCommand command, CancellationToken ct = default)
    {
        var course = await courseRepository.FindByIdAsync(command.CourseId, ct)
            ?? throw new KeyNotFoundException("Materia no encontrada.");

        if (course.CareerId != command.CareerId) throw new KeyNotFoundException("Materia no encontrada en esta carrera.");

        await courseRepository.DeleteAsync(course, ct);
    }
}
