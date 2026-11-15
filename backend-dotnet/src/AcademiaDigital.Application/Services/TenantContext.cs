using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Application.Services;

/// <summary>
/// Contenedor scoped de la institución del request. Lo completa el middleware de resolución;
/// el resto del sistema lo lee mediante <see cref="ITenantContext"/>.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private TenantInfo? _current;

    public TenantInfo Current => _current
        ?? throw new InvalidOperationException("El request no tiene una institución resuelta.");

    public void Set(TenantInfo tenant) => _current = tenant;
}
