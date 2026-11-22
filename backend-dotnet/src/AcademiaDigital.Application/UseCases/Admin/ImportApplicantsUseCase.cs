using System.Net.Mail;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

public sealed record ApplicantImportRow(int RowNumber, string? Dni, string? LastName, string? FirstName, string? Email, string? CareerCode);
public sealed record ApplicantImportFailure(int RowNumber, string Reason);
public sealed record ApplicantImportResult(int Created, IReadOnlyList<ApplicantImportFailure> Failed);

/// <summary>
/// Importa prematriculados desde Excel: cada fila crea un aspirante pendiente de alta (sección
/// Aspirantes). Una fila con error no frena las demás; se informa el motivo por número de fila.
/// </summary>
public sealed class ImportApplicantsUseCase(RegisterUseCase register, ICareerRepository careers)
{
    public const int MaxRows = 1000;

    public async Task<ApplicantImportResult> ExecuteAsync(IReadOnlyList<ApplicantImportRow> rows, CancellationToken ct = default)
    {
        if (rows.Count == 0) throw new ArgumentException("El archivo no tiene filas para importar.");
        if (rows.Count > MaxRows) throw new ArgumentException($"Se pueden importar hasta {MaxRows} filas por vez.");

        var careerIds = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        var failed = new List<ApplicantImportFailure>();
        var created = 0;
        foreach (var row in rows)
        {
            var reason = Validate(row);
            if (reason is not null) { failed.Add(new(row.RowNumber, reason)); continue; }

            var code = row.CareerCode!.Trim();
            if (!careerIds.TryGetValue(code, out var careerId))
                careerIds[code] = careerId = (await careers.FindByCodeAsync(code, ct))?.Id;
            if (careerId is null) { failed.Add(new(row.RowNumber, $"No existe la carrera con código '{code}'.")); continue; }

            try
            {
                await register.ImportApplicantAsync(row.Email!.Trim(), row.FirstName!.Trim(), row.LastName!.Trim(),
                    OnlyDigits(row.Dni!), careerId.Value, ct);
                created++;
            }
            catch (EmailAlreadyExistsException) { failed.Add(new(row.RowNumber, "Ya hay una cuenta con ese correo.")); }
            catch (DniAlreadyExistsException) { failed.Add(new(row.RowNumber, "Ya hay una cuenta con ese DNI.")); }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException)
            {
                failed.Add(new(row.RowNumber, ex.Message));
            }
        }
        return new ApplicantImportResult(created, failed);
    }

    private static string? Validate(ApplicantImportRow row)
    {
        if (string.IsNullOrWhiteSpace(row.LastName) || string.IsNullOrWhiteSpace(row.FirstName)) return "Falta el apellido o el nombre.";
        var dni = OnlyDigits(row.Dni ?? "");
        if (dni.Length is < 7 or > 9) return "El DNI debe tener entre 7 y 9 dígitos.";
        if (string.IsNullOrWhiteSpace(row.Email) || !MailAddress.TryCreate(row.Email.Trim(), out _)) return "El correo no es válido.";
        if (string.IsNullOrWhiteSpace(row.CareerCode)) return "Falta el código de carrera.";
        return null;
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsAsciiDigit).ToArray());
}
