namespace AcademiaDigital.Application.UseCases.CareerImport;

/// <summary>
/// CsvContent is a plain Stream (not IFormFile) on purpose: the Application layer must not
/// depend on ASP.NET Core web types. The API controller reads the uploaded IFormFile into a
/// stream and hands it here.
/// </summary>
public sealed record ImportCareerFromCsvCommand(
    string Name,
    string Code,
    string? Description,
    int TotalCredits,
    int DurationYears,
    string StudyPlanCode,
    string StudyPlanName,
    int VersionNumber,
    DateOnly? EffectiveFrom,
    Stream CsvContent);
