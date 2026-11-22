namespace AcademiaDigital.Domain.Interfaces.Repositories;

/// <summary>
/// Datos personales y de contacto que cada usuario carga desde su perfil: los que piden las
/// planillas del instituto (base de estudiantes y listado de contactos).
/// </summary>
public sealed record ProfileContactInfo(
    string? DocumentType,
    string? AlternatePhone,
    string? AddressLine,
    string? Neighborhood,
    string? City,
    string? District,
    string? Province,
    string? Country,
    string? PostalCode,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    string? HealthInsurance);

/// <summary>Lee y guarda solo los datos de contacto de la cuenta, sin tocar el resto del usuario.</summary>
public interface IProfileContactRepository
{
    /// <summary>Null si el usuario no existe.</summary>
    Task<ProfileContactInfo?> GetAsync(long userId, CancellationToken ct = default);

    /// <summary>False si el usuario no existe.</summary>
    Task<bool> SaveAsync(long userId, ProfileContactInfo info, CancellationToken ct = default);
}
