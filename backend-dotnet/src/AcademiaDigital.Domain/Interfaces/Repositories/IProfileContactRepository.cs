namespace AcademiaDigital.Domain.Interfaces.Repositories;

/// <summary>Domicilio, contacto de emergencia y obra social del alumno o docente.</summary>
public sealed record ProfileContactInfo(
    string? AddressLine,
    string? City,
    string? Province,
    string? PostalCode,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    string? HealthInsurance);

/// <summary>
/// Lee y guarda solo los datos de contacto de la ficha del usuario (alumno o docente), sin tocar
/// el resto de la entidad ni sus relaciones.
/// </summary>
public interface IProfileContactRepository
{
    /// <summary>Null si el usuario no tiene ficha de alumno ni de docente (por ejemplo, Admin).</summary>
    Task<ProfileContactInfo?> GetAsync(long userId, CancellationToken ct = default);

    /// <summary>False si el usuario no tiene ficha de alumno ni de docente.</summary>
    Task<bool> SaveAsync(long userId, ProfileContactInfo info, CancellationToken ct = default);
}
