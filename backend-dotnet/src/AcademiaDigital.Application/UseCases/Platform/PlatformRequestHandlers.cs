using System.Net;
using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Platform;

/// <summary>
/// Formulario público "Pide tu plataforma": valida, guarda el pedido y avisa al operador por
/// correo (si hay una dirección configurada). Un fallo del correo no pierde el pedido: queda
/// guardado y se ve en admin.dryvn.org.
/// </summary>
public sealed class SubmitPlatformRequestHandler(
    TenantPolicy policy,
    ITenantRegistry registry,
    IPlatformRequestStore store,
    IEmailSender emailSender,
    TimeProvider timeProvider)
{
    /// <returns>El pedido guardado, o null si se descartó por la trampa para bots.</returns>
    public async Task<PlatformRequestDto?> Handle(SubmitPlatformRequest request, string? notifyEmail, CancellationToken ct = default)
    {
        // Un bot completa el campo oculto: se responde igual que a una persona, sin guardar nada.
        if (!string.IsNullOrWhiteSpace(request.Website))
            return null;

        var institution = policy.NormalizeName(request.InstitutionName);
        string? slug = null;
        if (!string.IsNullOrWhiteSpace(request.RequestedSlug))
        {
            slug = policy.NormalizeSlug(request.RequestedSlug);
            if (await registry.SlugExistsAsync(slug, ct))
                throw new InvalidOperationException($"El subdominio '{slug}' ya está en uso. Elige otro.");
        }

        var contactName = Required(request.ContactName, "Tu nombre", 200);
        var email = Required(request.ContactEmail, "El email", 254).ToLowerInvariant();
        if (!IsEmail(email))
            throw new ArgumentException("El email no es válido.");
        if (request.StudentsEstimate is < 0 or > 1_000_000)
            throw new ArgumentException("La cantidad de alumnos no es válida.");

        var entity = new PlatformRequest
        {
            Id = Guid.NewGuid(),
            InstitutionName = institution,
            RequestedSlug = slug,
            ContactName = contactName,
            ContactEmail = email,
            ContactPhone = Optional(request.ContactPhone, "El teléfono", 40),
            Location = Optional(request.Location, "La ciudad", 200),
            StudentsEstimate = request.StudentsEstimate,
            Message = Optional(request.Message, "El mensaje", 2000),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        await store.AddAsync(entity, ct);

        if (!string.IsNullOrWhiteSpace(notifyEmail))
            await emailSender.SendAsync(BuildNotification(notifyEmail, entity), ct);

        return PlatformRequestDto.From(entity);
    }

    private static string Required(string? value, string field, int max)
    {
        var v = value?.Trim() ?? string.Empty;
        if (v.Length == 0) throw new ArgumentException($"{field} es obligatorio.");
        if (v.Length > max) throw new ArgumentException($"{field} admite hasta {max} caracteres.");
        return v;
    }

    private static string? Optional(string? value, string field, int max)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (v.Length > max) throw new ArgumentException($"{field} admite hasta {max} caracteres.");
        return v;
    }

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at == value.LastIndexOf('@') && at < value.Length - 3
            && value.IndexOf('.', at) > at + 1 && !value.Contains(' ');
    }

    private static EmailMessage BuildNotification(string to, PlatformRequest r)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? "—");
        var subject = $"Nuevo pedido de plataforma: {r.InstitutionName}";
        var text =
            $"Institución: {r.InstitutionName}\nSubdominio pedido: {r.RequestedSlug ?? "—"}\n" +
            $"Contacto: {r.ContactName} <{r.ContactEmail}>\nTeléfono: {r.ContactPhone ?? "—"}\n" +
            $"Ciudad: {r.Location ?? "—"}\nAlumnos aprox.: {r.StudentsEstimate?.ToString() ?? "—"}\n\n" +
            $"{r.Message}\n\nRevisarlo en admin.dryvn.org.";
        var html =
            $"<p><strong>{E(r.InstitutionName)}</strong> pidió su plataforma.</p><ul>" +
            $"<li>Subdominio pedido: {E(r.RequestedSlug)}</li>" +
            $"<li>Contacto: {E(r.ContactName)} &lt;{E(r.ContactEmail)}&gt;</li>" +
            $"<li>Teléfono: {E(r.ContactPhone)}</li><li>Ciudad: {E(r.Location)}</li>" +
            $"<li>Alumnos aprox.: {E(r.StudentsEstimate?.ToString())}</li></ul>" +
            $"<p>{E(r.Message)}</p><p>Revisarlo en admin.dryvn.org.</p>";
        return new EmailMessage(to, subject, html, text);
    }
}

public sealed class ListPlatformRequestsHandler(IPlatformRequestStore store)
{
    public async Task<IReadOnlyList<PlatformRequestDto>> Handle(PlatformRequestStatus? status, CancellationToken ct = default)
        => (await store.ListAsync(status, ct)).Select(PlatformRequestDto.From).ToList();
}

/// <summary>Aprobar (con la institución ya creada) o rechazar un pedido.</summary>
public sealed class ReviewPlatformRequestHandler(
    IPlatformRequestStore store, ITenantRegistry registry, TimeProvider timeProvider)
{
    public async Task<PlatformRequestDto> Handle(Guid id, ReviewPlatformRequest review, CancellationToken ct = default)
    {
        var request = await store.FindAsync(id, ct)
            ?? throw new KeyNotFoundException("Pedido no encontrado.");
        if (request.Status != PlatformRequestStatus.Pending)
            throw new InvalidOperationException("El pedido ya fue revisado.");

        switch (review.Status)
        {
            case PlatformRequestStatus.Approved:
                var slug = review.TenantSlug?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(slug) || !await registry.SlugExistsAsync(slug, ct))
                    throw new ArgumentException("Para aprobar el pedido, primero crea la institución e indica su identificador.");
                request.TenantSlug = slug;
                break;
            case PlatformRequestStatus.Rejected:
                break;
            default:
                throw new ArgumentException("Estado no válido: usa Approved o Rejected.");
        }

        request.Status = review.Status;
        request.ReviewedAt = timeProvider.GetUtcNow().UtcDateTime;
        await store.SaveAsync(ct);
        return PlatformRequestDto.From(request);
    }
}
