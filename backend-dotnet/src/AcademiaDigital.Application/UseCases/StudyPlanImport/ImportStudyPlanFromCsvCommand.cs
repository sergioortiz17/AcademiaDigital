namespace AcademiaDigital.Application.UseCases.StudyPlanImport;

/// <summary>
/// CsvContent is a plain Stream (not IFormFile) on purpose: the Application layer must not
/// depend on ASP.NET Core web types. The API controller reads the uploaded IFormFile into a
/// stream and hands it here.
///
/// Unlike the old career-import flow, CareerId points at an ALREADY EXISTING career (the handler
/// looks it up and 404s if missing) — this command never creates a Career, only a new StudyPlan
/// (+ its Courses/StudyPlanCourses/CoursePrerequisites) inside it.
/// </summary>
public sealed record ImportStudyPlanFromCsvCommand(
    int CareerId,
    string Code,
    string Name,
    int VersionNumber,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    Stream CsvContent);
