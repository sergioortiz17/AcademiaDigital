using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class TenantPolicyTests
{
    private readonly TenantPolicy _policy = new();

    [Theory]
    [InlineData("ucv", "ucv")]
    [InlineData("  UCV  ", "ucv")]
    [InlineData("instituto-norte-2", "instituto-norte-2")]
    [InlineData("a", "a")]
    public void NormalizeSlug_accepts_and_normalizes_valid_values(string input, string expected)
        => Assert.Equal(expected, _policy.NormalizeSlug(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-ucv")]
    [InlineData("ucv-")]
    [InlineData("u cv")]
    [InlineData("ucv_1")]
    [InlineData("ucv;drop")]
    [InlineData("ñandu")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 31
    [InlineData("control")]
    [InlineData("platform")]
    [InlineData("postgres")]
    public void NormalizeSlug_rejects_invalid_or_reserved_values(string? input)
        => Assert.Throws<ArgumentException>(() => _policy.NormalizeSlug(input));

    [Theory]
    [InlineData("ucv.edu.ar", "ucv.edu.ar")]
    [InlineData("UCV.localhost", "ucv.localhost")]
    [InlineData("http://ucv.localhost:4200", "ucv.localhost")]
    [InlineData("https://ucv.edu.ar/login?x=1", "ucv.edu.ar")]
    [InlineData("localhost:4200", "localhost")]
    [InlineData("127.0.0.1", "127.0.0.1")]
    public void NormalizeHost_strips_scheme_port_and_path(string input, string expected)
        => Assert.Equal(expected, _policy.NormalizeHost(input));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ucv..edu")]
    [InlineData("-ucv.edu")]
    [InlineData("ucv edu.ar")]
    [InlineData("ucv_edu.ar")]
    public void NormalizeHost_rejects_invalid_hosts(string input)
        => Assert.Throws<ArgumentException>(() => _policy.NormalizeHost(input));

    [Fact]
    public void NormalizeHosts_removes_duplicates_and_requires_at_least_one()
    {
        Assert.Equal(["ucv.localhost"], _policy.NormalizeHosts(["ucv.localhost", "HTTP://UCV.localhost:80", " "]));
        Assert.Throws<ArgumentException>(() => _policy.NormalizeHosts([]));
        Assert.Throws<ArgumentException>(() => _policy.NormalizeHosts(null));
    }

    [Fact]
    public void DatabaseNameFor_uses_a_safe_prefixed_identifier()
        => Assert.Equal("academiadigital_instituto_norte", _policy.DatabaseNameFor("instituto-norte"));

    [Fact]
    public void ValidateAdmin_accepts_complete_data()
        => _policy.ValidateAdmin("admin@ucv.edu", "Ana", "Pérez", "30111222", "Cambiar123!");

    [Theory]
    [InlineData("sin-arroba", "Ana", "Pérez", "30111222", "Cambiar123!")]
    [InlineData("a@b", "Ana", "Pérez", "30111222", "Cambiar123!")]
    [InlineData("admin@ucv.edu", "", "Pérez", "30111222", "Cambiar123!")]
    [InlineData("admin@ucv.edu", "Ana", " ", "30111222", "Cambiar123!")]
    [InlineData("admin@ucv.edu", "Ana", "Pérez", "", "Cambiar123!")]
    [InlineData("admin@ucv.edu", "Ana", "Pérez", "30111222", "corta")]
    public void ValidateAdmin_rejects_incomplete_data(string email, string name, string lastName, string dni, string password)
        => Assert.Throws<ArgumentException>(() => _policy.ValidateAdmin(email, name, lastName, dni, password));
}
