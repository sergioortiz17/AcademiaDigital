namespace AcademiaDigital.Application.UseCases.StudyPlanImport;

/// <summary>
/// Abstracción del parseo de bajo nivel de un CSV de plan de estudios a filas tipadas.
/// Aísla la dependencia de la librería de parsing (CsvHelper) fuera de la capa Application:
/// la interfaz vive acá, pero la implementación concreta (CsvHelper) vive en Infrastructure.
///
/// Solo hace el parseo sintáctico + validaciones de campo por fila (tipos, requeridos, rangos).
/// La validación de DOMINIO (existencia de course_type, ciclos de correlatividad, unicidad entre
/// filas) queda en <see cref="StudyPlanCsvValidator"/>, en Application, porque depende del modelo
/// y de repositorios de dominio, no del formato del archivo.
/// </summary>
public interface IStudyPlanCsvParser
{
    Task<StudyPlanCsvParseOutcome> ParseAsync(Stream csvContent, CancellationToken ct = default);
}

/// <summary>Resultado del parseo de bajo nivel: filas válidas a nivel de campo + errores de parseo.</summary>
public sealed class StudyPlanCsvParseOutcome
{
    public IReadOnlyList<StudyPlanCsvRow> Rows { get; init; } = [];
    public IReadOnlyList<CsvRowError> Errors { get; init; } = [];
}
