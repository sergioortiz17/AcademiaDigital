using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Careers;

/// <summary>
/// CRUD for Careers. DeleteAsync performs a full cascade delete (CoursePrerequisites ->
/// StudyPlanCourses -> StudyPlans -> Courses -> Career) inside a single transaction, mirroring the
/// order used by the reference SQL reset scripts. FK constraints on these tables are configured as
/// ReferentialAction.Restrict, so no DB-level cascade exists; it must be done explicitly here.
/// If the career has any student enrolled (StudentCareers), the delete is refused with an
/// InvalidOperationException (-> 409 via ExceptionMiddleware) instead of silently orphaning data.
/// </summary>
public class CareerService(
    ICareerRepository careerRepository,
    ICourseRepository courseRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    ICoursePrerequisiteRepository coursePrerequisiteRepository,
    IStudentCareerRepository studentCareerRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<IEnumerable<CareerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var careers = await careerRepository.GetAllAsync(ct);
        var dtos = new List<CareerDto>();
        foreach (var career in careers)
        {
            var dto = Map(career);
            dto.CourseCount = await courseRepository.CountByCareerIdAsync(career.Id, ct);
            dtos.Add(dto);
        }
        return dtos;
    }

    public async Task<CareerDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(id, ct);
        return career is null ? null : Map(career);
    }

    public async Task<CareerDto> CreateAsync(CreateCareerDto dto, CancellationToken ct = default)
    {
        var career = new Career
        {
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            Description = dto.Description?.Trim(),
            TotalCredits = dto.TotalCredits,
            DurationYears = dto.DurationYears
        };

        var created = await careerRepository.CreateAsync(career, ct);
        return Map(created);
    }

    public async Task<bool> UpdateAsync(int id, CreateCareerDto dto, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(id, ct);
        if (career is null) return false;

        career.Name = dto.Name.Trim();
        career.Code = dto.Code.Trim();
        career.Description = dto.Description?.Trim();
        career.TotalCredits = dto.TotalCredits;
        career.DurationYears = dto.DurationYears;

        await careerRepository.UpdateAsync(career, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(id, ct);
        if (career is null) return false;

        var hasStudents = await studentCareerRepository.ExistsForCareerAsync(id, ct);
        if (hasStudents)
            throw new InvalidOperationException(
                "No se puede eliminar la carrera porque tiene estudiantes asociados.");

        await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var studyPlans = await studyPlanRepository.GetByCareerIdAsync(id, transactionCt);
            var studyPlanIds = studyPlans.Select(sp => sp.Id).ToList();

            if (studyPlanIds.Count > 0)
            {
                await coursePrerequisiteRepository.DeleteByStudyPlanIdsAsync(studyPlanIds, transactionCt);
                await studyPlanCourseRepository.DeleteByStudyPlanIdsAsync(studyPlanIds, transactionCt);
            }

            await studyPlanRepository.DeleteByCareerIdAsync(id, transactionCt);
            await courseRepository.DeleteByCareerIdAsync(id, transactionCt);
            await careerRepository.DeleteAsync(career, transactionCt);

            return true;
        }, ct);

        return true;
    }

    private static CareerDto Map(Career career) => new()
    {
        Id = career.Id,
        Name = career.Name,
        Code = career.Code,
        Description = career.Description,
        TotalCredits = career.TotalCredits,
        DurationYears = career.DurationYears,
        IsActive = career.IsActive,
        CreatedAt = career.CreatedAt
    };
}
