using System.Text.RegularExpressions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

/// <summary>
/// Datos de contacto que el alumno o el docente cargan desde su perfil: domicilio, contacto de
/// emergencia y obra social. El Admin no tiene ficha, así que no tiene estos datos.
/// </summary>
public sealed partial class ProfileContactUseCase(IProfileContactRepository repository)
{
    public Task<ProfileContactInfo?> GetAsync(long userId, CancellationToken ct = default)
        => repository.GetAsync(userId, ct);

    public async Task<ProfileContactInfo> SaveAsync(long userId, ProfileContactInfo input, CancellationToken ct = default)
    {
        var info = new ProfileContactInfo(
            Clean(input.AddressLine, 255, "El domicilio"),
            Clean(input.City, 100, "La ciudad"),
            Clean(input.Province, 100, "La provincia"),
            Clean(input.PostalCode, 20, "El código postal"),
            Clean(input.EmergencyContactName, 200, "El nombre del contacto de emergencia"),
            Clean(input.EmergencyContactRelationship, 100, "El vínculo del contacto de emergencia"),
            Clean(input.EmergencyContactPhone, 30, "El teléfono de emergencia"),
            Clean(input.HealthInsurance, 150, "La obra social"));
        if (info.EmergencyContactPhone is not null && !PhonePattern().IsMatch(info.EmergencyContactPhone))
            throw new ArgumentException("El teléfono de emergencia solo admite números, espacios, guiones y un + inicial (6 a 15 dígitos).");
        if (!await repository.SaveAsync(userId, info, ct))
            throw new InvalidOperationException("Su cuenta no tiene ficha de alumno ni de docente.");
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
