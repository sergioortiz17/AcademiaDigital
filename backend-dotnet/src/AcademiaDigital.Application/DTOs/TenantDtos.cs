namespace AcademiaDigital.Application.DTOs;

public sealed record TenantSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    bool IsActive,
    bool IsDefault,
    DateTime CreatedAt,
    IReadOnlyList<string> Domains);

public sealed record CurrentTenantDto(string Slug, string Name);

public sealed record CreateTenantRequest(
    string Slug,
    string Name,
    IReadOnlyList<string> Domains,
    string AdminEmail,
    string AdminName,
    string AdminLastName,
    string AdminDni,
    string AdminPassword);
