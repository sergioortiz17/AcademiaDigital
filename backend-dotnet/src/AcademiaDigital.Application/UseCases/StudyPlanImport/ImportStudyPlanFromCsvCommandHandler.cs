using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.StudyPlanImport;

/// <summary>
/// Imports a new StudyPlan (+ Courses + StudyPlanCourses + CoursePrerequisites) from a CSV into an
/// ALREADY EXISTING Career. Replaces the old "import everything in one shot" flow
/// (POST api/v1/careers/import, AcademiaDigital.Application.UseCases.CareerImport) now that
/// "create career" and "import a study plan for a career" are two separate admin actions: a career
/// can have several study plans over time (e.g. successive Resolución Ministerial versions), so
/// importing must be repeatable against the same career without recreating it.
///
/// Courses are career-scoped (unique per CareerId+Code), not plan-scoped, so a course that already
/// exists in this career (e.g. carried over from a previous plan version) is reused by Id instead of
/// being re-inserted — re-inserting would violate the unique index and also fork the Course identity
/// across plan versions, breaking cross-plan history for a student's academic record.
///
/// Same two failure modes as the old handler:
///  - Career not found -> KeyNotFoundException (404, handled by the controller).
///  - Any row of the CSV is invalid -> no exception; returns a Failed result with the full list of
///    row errors. No partial data is ever committed (validation happens before the transaction opens).
/// </summary>
public sealed class ImportStudyPlanFromCsvCommandHandler(
    ICareerRepository careerRepository,
    ICourseRepository courseRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    ICoursePrerequisiteRepository prerequisiteRepository,
    ICourseTypeRepository courseTypeRepository,
    PrerequisiteCycleValidator cycleValidator,
    StudyPlanCsvValidator csvValidator,
    IUnitOfWork unitOfWork)
{
    public async Task<ImportStudyPlanCsvResult> Handle(ImportStudyPlanFromCsvCommand command, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(command.CareerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");

        var parseResult = await csvValidator.ParseAndValidateAsync(command.CsvContent, courseTypeRepository, cycleValidator, ct);
        if (!parseResult.Success)
            return ImportStudyPlanCsvResult.Failed(parseResult.Errors);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            // Imported as Draft, not Active: StudyPlan.Create nace en Draft. El endpoint
            // POST .../study-plans/{id}/activate ya archiva el plan activo anterior cuando el
            // admin promueve este. El primer plan de una carrera debe activarse explícitamente.
            var studyPlan = await studyPlanRepository.CreateAsync(StudyPlan.Create(
                career.Id,
                command.Code,
                command.Name,
                command.VersionNumber,
                command.EffectiveFrom,
                command.EffectiveTo), transactionCt);

            var courseIdByCode = new Dictionary<string, int>();
            var coursesCreated = 0;
            foreach (var row in parseResult.Rows)
            {
                var existingCourse = await courseRepository.FindByCodeAsync(career.Id, row.CourseCode, transactionCt);
                if (existingCourse is not null)
                {
                    courseIdByCode[row.CourseCode] = existingCourse.Id;
                }
                else
                {
                    var course = await courseRepository.CreateAsync(new Course
                    {
                        CareerId = career.Id,
                        Code = row.CourseCode,
                        Name = row.Name
                    }, transactionCt);
                    courseIdByCode[row.CourseCode] = course.Id;
                    coursesCreated++;
                }

                CourseType? courseType = string.IsNullOrWhiteSpace(row.CourseTypeCode)
                    ? null
                    : parseResult.CourseTypeCache.GetValueOrDefault(row.CourseTypeCode);

                await studyPlanCourseRepository.CreateAsync(new StudyPlanCourse
                {
                    StudyPlanId = studyPlan.Id,
                    CourseId = courseIdByCode[row.CourseCode],
                    YearNumber = row.YearNumber,
                    Semester = row.Semester,
                    CourseTypeId = courseType?.Id,
                    SortOrder = row.SortOrder,
                    IsMandatory = row.IsMandatory,
                    WorkloadHours = row.WorkloadHours
                }, transactionCt);
            }

            var prerequisitesCreated = 0;
            foreach (var (courseCode, prerequisiteCode) in parseResult.PrerequisitePairs)
            {
                await prerequisiteRepository.CreateAsync(CoursePrerequisite.Create(
                    studyPlan.Id,
                    courseIdByCode[courseCode],
                    courseIdByCode[prerequisiteCode]), transactionCt);
                prerequisitesCreated++;
            }

            return ImportStudyPlanCsvResult.Succeeded(studyPlan.Id, coursesCreated, prerequisitesCreated);
        }, ct);
    }
}
