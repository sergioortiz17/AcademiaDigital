using System.Globalization;
using AcademiaDigital.Application.UseCases.StudyPlanImport;
using CsvHelper;
using CsvHelper.Configuration;

namespace AcademiaDigital.Infrastructure.Csv;

/// <summary>
/// Implementación de <see cref="IStudyPlanCsvParser"/> basada en CsvHelper. Es el ÚNICO lugar del
/// backend que conoce CsvHelper: la capa Application depende solo de la interfaz, no del paquete.
///
/// Hace parseo sintáctico + validaciones de campo por fila (tipos, requeridos, longitudes, rangos).
/// No conoce el dominio (course types, correlatividades): eso lo valida StudyPlanCsvValidator en
/// Application sobre las filas que este parser devuelve.
/// </summary>
public sealed class CsvHelperStudyPlanParser : IStudyPlanCsvParser
{
    private static readonly string[] RequiredColumns =
    [
        "sort_order", "course_code", "name", "year_number", "semester",
        "course_type_code", "workload_hours", "is_mandatory", "prerequisites"
    ];

    public async Task<StudyPlanCsvParseOutcome> ParseAsync(Stream csvContent, CancellationToken ct = default)
    {
        var rows = new List<StudyPlanCsvRow>();
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
            return new StudyPlanCsvParseOutcome { Errors = errors };
        }
        csv.ReadHeader();
        var header = csv.HeaderRecord ?? [];
        var missingColumns = RequiredColumns.Except(header).ToList();
        if (missingColumns.Count > 0)
        {
            errors.Add(new CsvRowError(1, $"Faltan columnas obligatorias en el header: {string.Join(", ", missingColumns)}."));
            return new StudyPlanCsvParseOutcome { Errors = errors };
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
                rows.Add(new StudyPlanCsvRow
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

        return new StudyPlanCsvParseOutcome { Rows = rows, Errors = errors };
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
