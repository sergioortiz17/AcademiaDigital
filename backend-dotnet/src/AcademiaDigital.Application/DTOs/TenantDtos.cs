namespace AcademiaDigital.Application.DTOs;

public sealed record TenantSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    bool IsActive,
    bool IsDefault,
    DateTime CreatedAt,
    IReadOnlyList<string> Domains,
    string? TeacherEmailDomain = null);

/// <param name="TeacherEmailDomain">Dominio de correo docente: el registro oculta la carrera para esos emails.</param>
public sealed record CurrentTenantDto(string Slug, string Name, string? TeacherEmailDomain = null);

public sealed record CreateTenantRequest(
    string Slug,
    string Name,
    IReadOnlyList<string> Domains,
    string AdminEmail,
    string AdminName,
    string AdminLastName,
    string AdminDni,
    string AdminPassword,
    string? TeacherEmailDomain = null);
