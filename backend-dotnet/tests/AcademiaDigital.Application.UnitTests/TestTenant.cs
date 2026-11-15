using AcademiaDigital.Application.Interfaces;
using NSubstitute;

namespace AcademiaDigital.Application.UnitTests;

/// <summary>ITenantContext de prueba (por defecto la institución original, ITSC).</summary>
internal static class TestTenant
{
    public static ITenantContext Context(string slug = "itsc", string name = "Instituto Tecnológico Superior Córdoba", bool isDefault = true)
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.Current.Returns(new TenantInfo(Guid.NewGuid(), slug, name, $"academiadigital_{slug}", isDefault));
        return tenant;
    }
}
