namespace AcademiaDigital.Domain.Enums;

/// <summary>
/// Estado del alta de un usuario. Las cuentas creadas por el registro público quedan
/// <see cref="Pending"/> hasta que un Admin las da de alta (sección "Aspirantes"); las creadas
/// por un Admin, la plataforma o los seeds nacen <see cref="Approved"/>.
/// </summary>
public enum RegistrationStatus
{
    Approved = 0,
    Pending = 1
}
