using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class TeacherEmailDomainsTests
{
    [Theory]
    [InlineData(" ItsCordoba.edu.ar ", "itscordoba.edu.ar")]
    [InlineData("@itscordoba.edu.ar", "itscordoba.edu.ar")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normalize_cleans_the_domain(string? input, string? expected)
        => Assert.Equal(expected, TeacherEmailDomains.Normalize(input));

    [Theory]
    [InlineData("itscordoba")]
    [InlineData("a@itscordoba.edu.ar")]
    [InlineData("itscordoba..edu.ar")]
    public void Normalize_rejects_invalid_domains(string input)
        => Assert.Throws<ArgumentException>(() => TeacherEmailDomains.Normalize(input));

    [Theory]
    [InlineData("maria.quintar@itscordoba.edu.ar", true)]
    [InlineData("MARIA@ITSCORDOBA.EDU.AR", true)]
    [InlineData("maria@sub.itscordoba.edu.ar", false)]
    [InlineData("maria@itscordoba.edu.ar.evil.com", false)]
    [InlineData("itscordoba.edu.ar", false)]
    public void Matches_only_the_exact_domain(string email, bool expected)
        => Assert.Equal(expected, TeacherEmailDomains.Matches(email, "itscordoba.edu.ar"));
}
