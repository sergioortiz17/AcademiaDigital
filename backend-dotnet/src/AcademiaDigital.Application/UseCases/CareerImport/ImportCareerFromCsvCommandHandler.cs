using System.Globalization;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using CsvHelper;
using CsvHelper.Configuration;

namespace AcademiaDigital.Application.UseCases.CareerImport;

/// <summary>
/// Imports a full Career (Career + StudyPlan v1 Active + Courses + StudyPlanCourses +
/// CoursePrerequisites) from a single CSV in one transactional operation.
///
/// Two distinct failure modes, matching the rest of the Careers/Courses use cases:
///  - Career.Code already exists -> InvalidOperationException (409, handled by ExceptionMiddleware).
///  - Any row of the CSV is invalid -> no exception; the handler returns a Failed result with the
///    full list of row errors so the caller can render them and let the admin fix the file. No
///    partial data is ever committed in this case (validation happens before the transaction opens).
/// </summary>
public sealed class ImportCareerFromCsvCommandHandler(
    ICareerRepository careerRepository,
    ICourseRepository courseRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    ICoursePrerequisiteRepository prerequisiteRepository,
    ICourseTypeRepository courseTypeRepository,
    PrerequisiteCycleValidator cycleValidator,
    IUnitOfWork unitOfWork)
{
    private static readonly string[] RequiredColumns =
    [
        "sort_order", "course_code", "name", "year_number", "semester",
        "course_type_code", "workload_hours", "is_mandatory", "prerequisites"
    ];

    public async Task<ImportCareerCsvResult> Handle(ImportCareerFromCsvCommand command, CancellationToken ct = default)
    {
        var code = command.Code.Trim();
        var existingCareer = await careerRepository.FindByCodeAsync(code, ct);
        if (existingCareer is not null)
            throw new InvalidOperationException("Career code already exists.");

        var (rows, parseErrors) = await ParseAsync(command.CsvContent, ct);
        if (parseErrors.Count > 0)
            return ImportCareerCsvResult.Failed(parseErrors);

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
            return ImportCareerCsvResult.Failed(errors);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var career = await careerRepository.CreateAsync(new Career
            {
                Name = command.Name.Trim(),
                Code = code,
                Description = command.Description?.Trim(),
                TotalCredits = command.TotalCredits,
                DurationYears = command.DurationYears
            }, transactionCt);

            var studyPlan = await studyPlanRepository.CreateAsync(new StudyPlan
            {
                CareerId = career.Id,
                Code = command.StudyPlanCode.Trim(),
                Name = command.StudyPlanName.Trim(),
                VersionNumber = command.VersionNumber,
                Status = StudyPlanStatus.Active,
                EffectiveFrom = command.EffectiveFrom
            }, transactionCt);

            var courseIdByCode = new Dictionary<string, int>();
            foreach (var row in rows)
            {
                var course = await courseRepository.CreateAsync(new Course
                {
                    CareerId = career.Id,
                    Code = row.CourseCode,
                    Name = row.Name
                }, transactionCt);
                courseIdByCode[row.CourseCode] = course.Id;

                CourseType? courseType = string.IsNullOrWhiteSpace(row.CourseTypeCode)
                    ? null
                    : courseTypeCache.GetValueOrDefault(row.CourseTypeCode);

                await studyPlanCourseRepository.CreateAsync(new StudyPlanCourse
                {
                    StudyPlanId = studyPlan.Id,
                    CourseId = course.Id,
                    YearNumber = row.YearNumber,
                    Semester = row.Semester,
                    CourseTypeId = courseType?.Id,
                    SortOrder = row.SortOrder,
                    IsMandatory = row.IsMandatory,
                    WorkloadHours = row.WorkloadHours
                }, transactionCt);
            }

            var prerequisitesCreated = 0;
            foreach (var (courseCode, prerequisiteCode) in prerequisitePairs)
            {
                await prerequisiteRepository.CreateAsync(new CoursePrerequisite
                {
                    StudyPlanId = studyPlan.Id,
                    CourseId = courseIdByCode[courseCode],
                    PrerequisiteCourseId = courseIdByCode[prerequisiteCode]
                }, transactionCt);
                prerequisitesCreated++;
            }

            return ImportCareerCsvResult.Succeeded(career.Id, studyPlan.Id, rows.Count, prerequisitesCreated);
        }, ct);
    }

    private static async Task<(List<CareerImportRow> Rows, List<CsvRowError> Errors)> ParseAsync(Stream csvContent, CancellationToken ct)
    {
        var rows = new List<CareerImportRow>();
        var errors = new List<CsvRowError>();

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            DetectDelimiter = false
        };

        using var reader = new StreamReader(csvContent);
        using var csv = new CsvReader(reader, csvConfig);

        if (!await csv.ReadAsync())
        {
            errors.Add(new CsvRowError(0, "El archivo CSV está vacío."));
            return (rows, errors);
        }
        csv.ReadHeader();
        var header = csv.HeaderRecord ?? [];
        var missingColumns = RequiredColumns.Except(header).ToList();
        if (missingColumns.Count > 0)
        {
            errors.Add(new CsvRowError(1, $"Faltan columnas obligatorias en el header: {string.Join(", ", missingColumns)}."));
            return (rows, errors);
        }

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            var rowErrorsBefore = errors.Count;

            string? Field(string name) => csv.TryGetField<string>(name, out var v) ? v?.Trim() : null;

            var sortOrderRaw = Field("sort_order");
            var courseCode = Field("course_code");
            var name = Field("name");
            var yearNumberRaw = Field("year_number");
            var semesterRaw = Field("semester");
            var courseTypeCode = Field("course_type_code");
            var workloadHoursRaw = Field("workload_hours");
            var isMandatoryRaw = Field("is_mandatory");
            var prerequisitesRaw = Field("prerequisites");

            if (!int.TryParse(sortOrderRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sortOrder))
                errors.Add(new CsvRowError(rowNumber, $"sort_order '{sortOrderRaw}' no es un entero válido."));

            if (string.IsNullOrWhiteSpace(courseCode))
                errors.Add(new CsvRowError(rowNumber, "course_code es obligatorio."));
            else if (courseCode.Length > 20)
                errors.Add(new CsvRowError(rowNumber, "course_code no puede superar los 20 caracteres."));

            if (string.IsNullOrWhiteSpace(name))
                errors.Add(new CsvRowError(rowNumber, "name es obligatorio."));
            else if (name.Length > 200)
                errors.Add(new CsvRowError(rowNumber, "name no puede superar los 200 caracteres."));

            if (!int.TryParse(yearNumberRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yearNumber) || yearNumber <= 0)
                errors.Add(new CsvRowError(rowNumber, $"year_number '{yearNumberRaw}' debe ser un entero mayor a 0."));

            if (!int.TryParse(semesterRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var semester) || (semester != 1 && semester != 2))
                errors.Add(new CsvRowError(rowNumber, $"semester '{semesterRaw}' debe ser 1 o 2."));

            if (!string.IsNullOrWhiteSpace(courseTypeCode) && courseTypeCode.Length > 20)
                errors.Add(new CsvRowError(rowNumber, "course_type_code no puede superar los 20 caracteres."));

            int? workloadHours = null;
            if (!string.IsNullOrWhiteSpace(workloadHoursRaw))
            {
                if (int.TryParse(workloadHoursRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var wh) && wh >= 0)
                    workloadHours = wh;
                else
                    errors.Add(new CsvRowError(rowNumber, $"workload_hours '{workloadHoursRaw}' no es un entero válido."));
            }

            var isMandatory = true;
            if (!string.IsNullOrWhiteSpace(isMandatoryRaw))
            {
                if (!TryParseBool(isMandatoryRaw, out isMandatory))
                    errors.Add(new CsvRowError(rowNumber, $"is_mandatory '{isMandatoryRaw}' debe ser true/false (o 1/0)."));
            }

            var prerequisiteCodes = string.IsNullOrWhiteSpace(prerequisitesRaw)
                ? new List<string>()
                : prerequisitesRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            if (rowErrorsBefore == errors.Count)
            {
                rows.Add(new CareerImportRow
                {
                    RowNumber = rowNumber,
                    SortOrder = sortOrder,
                    CourseCode = courseCode!,
                    Name = name!,
                    YearNumber = yearNumber,
                    Semester = semester,
                    CourseTypeCode = string.IsNullOrWhiteSpace(courseTypeCode) ? null : courseTypeCode,
                    WorkloadHours = workloadHours,
                    IsMandatory = isMandatory,
                    PrerequisiteCourseCodes = prerequisiteCodes
                });
            }
        }

        if (rows.Count == 0 && errors.Count == 0)
            errors.Add(new CsvRowError(0, "El archivo CSV no contiene filas de datos."));

        return (rows, errors);
    }

    private static bool TryParseBool(string raw, out bool value)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "true": case "1": value = true; return true;
            case "false": case "0": value = false; return true;
            default: value = true; return false;
        }
    }
}
