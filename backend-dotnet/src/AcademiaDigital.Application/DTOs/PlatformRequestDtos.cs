using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Application.DTOs;

/// <summary>Formulario público "Pide tu plataforma".</summary>
/// <param name="Website">Trampa para bots (campo oculto en el formulario): una persona lo deja vacío.</param>
public sealed record SubmitPlatformRequest(
    string InstitutionName,
    string? RequestedSlug,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    string? Location,
    int? StudentsEstimate,
    string? Message,
    string? Website = null);

public sealed record PlatformRequestDto(
    Guid Id,
    string InstitutionName,
    string? RequestedSlug,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    string? Location,
    int? StudentsEstimate,
    string? Message,
    PlatformRequestStatus Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? TenantSlug)
{
    public static PlatformRequestDto From(PlatformRequest r) => new(
        r.Id, r.InstitutionName, r.RequestedSlug, r.ContactName, r.ContactEmail, r.ContactPhone,
        r.Location, r.StudentsEstimate, r.Message, r.Status, r.CreatedAt, r.ReviewedAt, r.TenantSlug);
}

/// <param name="TenantSlug">Al aprobar: la institución que se creó para este pedido.</param>
public sealed record ReviewPlatformRequest(PlatformRequestStatus Status, string? TenantSlug);
