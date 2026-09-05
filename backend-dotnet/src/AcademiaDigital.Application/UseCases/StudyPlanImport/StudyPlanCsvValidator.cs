using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.StudyPlanImport;

/// <summary>
/// Result of parsing + validating a study-plan CSV. Shared by the import handler (persists it) and
/// the diff-preview handler (only diffs it against an existing plan, never persists it).
/// </summary>
public sealed class StudyPlanCsvParseResult
{
    public IReadOnlyList<StudyPlanCsvRow> Rows { get; init; } = [];
    public IReadOnlyList<CsvRowError> Errors { get; init; } = [];
    public IReadOnlyDictionary<string, CourseType?> CourseTypeCache { get; init; } = new Dictionary<string, CourseType?>();
    public IReadOnlyList<(string CourseCode, string PrerequisiteCode)> PrerequisitePairs { get; init; } = [];
    public bool Success => Errors.Count == 0;
}

/// <summary>
/// Valida (a nivel de DOMINIO) un CSV de plan de estudios ya parseado a filas: unicidad de
/// course_code dentro del archivo, existencia de course_type_code en el catálogo, y que los
/// prerequisites referencien materias del mismo archivo sin auto-referencia ni ciclos.
///
/// El parseo de bajo nivel (formato del archivo, CsvHelper) se delega a <see cref="IStudyPlanCsvParser"/>,
/// cuya implementación vive en Infrastructure. Así Application no depende de ningún paquete de
/// parsing/IO — solo del modelo de dominio y de repositorios.
///
/// Reemplaza al antiguo StudyPlanCsvParser estático (que usaba CsvHelper directo en Application).
/// </summary>
public sealed class StudyPlanCsvValidator(IStudyPlanCsvParser parser)
{
    public async Task<StudyPlanCsvParseResult> ParseAndValidateAsync(
        Stream csvContent,
        ICourseTypeRepository courseTypeRepository,
        PrerequisiteCycleValidator cycleValidator,
        CancellationToken ct = default)
    {
        var parsed = await parser.ParseAsync(csvContent, ct);
        if (parsed.Errors.Count > 0)
            return new StudyPlanCsvParseResult { Errors = parsed.Errors };

        var rows = parsed.Rows;
        var errors = new List<CsvRowError>();

        // course_code uniqueness within the file
        var duplicateCodes = rows
            .GroupBy(r => r.CourseCode)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();
        foreach (var row in rows.Where(r => duplicateCodes.Contains(r.CourseCode)))
            errors.Add(new CsvRowError(row.RowNumber, $"course_code '{row.CourseCode}' está duplicado en el archivo."));

        // course_type_code must exist in the shared CourseTypes catalog
        var courseTypeCache = new Dictionary<string, CourseType?>();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.CourseTypeCode)) continue;
            if (!courseTypeCache.TryGetValue(row.CourseTypeCode, out var courseType))
            {
                courseType = await courseTypeRepository.FindByCodeAsync(row.CourseTypeCode, ct);
                courseTypeCache[row.CourseTypeCode] = courseType;
            }
            if (courseType is null)
                errors.Add(new CsvRowError(row.RowNumber, $"course_type_code '{row.CourseTypeCode}' no existe en el catálogo de CourseTypes."));
        }

        // prerequisites: must reference other course_codes in the same file, no self-reference, no cycles
        var codeToIndex = rows
            .Where(r => !duplicateCodes.Contains(r.CourseCode))
            .Select((r, i) => (r.CourseCode, Index: i))
            .ToDictionary(x => x.CourseCode, x => x.Index);

        var acceptedEdges = new List<CoursePrerequisite>();
        var prerequisitePairs = new List<(string CourseCode, string PrerequisiteCode)>();

        foreach (var row in rows)
        {
            if (duplicateCodes.Contains(row.CourseCode)) continue;

            foreach (var prereqCode in row.PrerequisiteCourseCodes)
            {
                if (prereqCode == row.CourseCode)
                {
                    errors.Add(new CsvRowError(row.RowNumber, $"'{row.CourseCode}' no puede ser prerequisito de sí misma."));
                    continue;
                }

                if (!codeToIndex.TryGetValue(prereqCode, out var prereqIndex))
                {
                    errors.Add(new CsvRowError(row.RowNumber, $"prerequisites: course_code '{prereqCode}' no existe en el archivo."));
                    continue;
                }

                var courseIndex = codeToIndex[row.CourseCode];
                if (cycleValidator.WouldCreateCycle(acceptedEdges, courseIndex, prereqIndex))
                {
                    errors.Add(new CsvRowError(row.RowNumber, $"prerequisites: '{prereqCode}' como prerequisito de '{row.CourseCode}' generaría un ciclo de correlatividades."));
                    continue;
                }

                acceptedEdges.Add(new CoursePrerequisite { CourseId = courseIndex, PrerequisiteCourseId = prereqIndex, IsActive = true });
                prerequisitePairs.Add((row.CourseCode, prereqCode));
            }
        }

        if (errors.Count > 0)
            return new StudyPlanCsvParseResult { Errors = errors };

        return new StudyPlanCsvParseResult
        {
            Rows = rows,
            Errors = [],
            CourseTypeCache = courseTypeCache,
            PrerequisitePairs = prerequisitePairs
        };
    }
}
