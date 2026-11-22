using System.Text.RegularExpressions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

/// <summary>
/// Datos personales y de contacto que cualquier usuario (alumno, docente o personal) carga desde
/// su perfil: tipo de documento, segundo teléfono, domicilio completo, contacto de emergencia y
/// obra social.
/// </summary>
public sealed partial class ProfileContactUseCase(IProfileContactRepository repository)
{
    /// <summary>Tipos de documento que usan las planillas del Ministerio.</summary>
    public static readonly IReadOnlyList<string> DocumentTypes = ["DNI", "LC", "LE", "CI", "PAS"];

    public Task<ProfileContactInfo?> GetAsync(long userId, CancellationToken ct = default)
        => repository.GetAsync(userId, ct);

    public async Task<ProfileContactInfo> SaveAsync(long userId, ProfileContactInfo input, CancellationToken ct = default)
    {
        var documentType = Clean(input.DocumentType, 10, "El tipo de documento")?.ToUpperInvariant();
        if (documentType is not null && !DocumentTypes.Contains(documentType))
            throw new ArgumentException($"El tipo de documento debe ser uno de: {string.Join(", ", DocumentTypes)}.");

        var info = new ProfileContactInfo(
            documentType,
            Clean(input.AlternatePhone, 30, "El otro teléfono"),
            Clean(input.AddressLine, 300, "El domicilio"),
            Clean(input.Neighborhood, 120, "El barrio"),
            Clean(input.City, 120, "La ciudad"),
            Clean(input.District, 120, "El departamento"),
            Clean(input.Province, 120, "La provincia"),
            Clean(input.Country, 100, "El país"),
            Clean(input.PostalCode, 20, "El código postal"),
            Clean(input.EmergencyContactName, 200, "El nombre del contacto de emergencia"),
            Clean(input.EmergencyContactRelationship, 100, "El vínculo del contacto de emergencia"),
            Clean(input.EmergencyContactPhone, 30, "El teléfono de emergencia"),
            Clean(input.HealthInsurance, 150, "La obra social"));
        if (info.AlternatePhone is not null && !PhonePattern().IsMatch(info.AlternatePhone))
            throw new ArgumentException("El otro teléfono solo admite números, espacios, guiones y un + inicial (6 a 15 dígitos).");
        if (info.EmergencyContactPhone is not null && !PhonePattern().IsMatch(info.EmergencyContactPhone))
            throw new ArgumentException("El teléfono de emergencia solo admite números, espacios, guiones y un + inicial (6 a 15 dígitos).");
        if (!await repository.SaveAsync(userId, info, ct))
            throw new KeyNotFoundException("Usuario no encontrado.");
        return info;
    }

    private static string? Clean(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > maxLength) throw new ArgumentException($"{field} admite hasta {maxLength} caracteres.");
        return trimmed;
    }

    // Entre 6 y 15 dígitos (estándar E.164), con separadores opcionales.
    [GeneratedRegex(@"^\+?(?:[\s-]*\d){6,15}[\s-]*$")]
    private static partial Regex PhonePattern();
}
