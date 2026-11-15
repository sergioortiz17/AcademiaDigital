namespace AcademiaDigital.Application.Interfaces;

/// <summary>Institución resuelta para el request actual.</summary>
/// <param name="IsDefault">Institución original (ITSC): conserva el comportamiento previo al multitenant.</param>
public sealed record TenantInfo(Guid Id, string Slug, string Name, string DatabaseName, bool IsDefault);

public interface ITenantContext
{
    /// <summary>Institución del request. Lanza si el request no pasó por la resolución de tenant.</summary>
    TenantInfo Current { get; }
}
