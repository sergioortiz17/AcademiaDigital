using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class ContactFormatPolicyTests
{
    [Theory]
    [InlineData("20123456789", "20-12345678-9")]
    [InlineData("20-12345678-9", "20-12345678-9")]
    [InlineData("20.12345678.9", "20-12345678-9")]
    public void Cuil_is_normalized_with_dashes(string input, string expected)
        => Assert.Equal(expected, ContactFormatPolicy.NormalizeCuil(input));

    [Theory]
    [InlineData("20-1234ABCD-9")]
    [InlineData("20-1234567-9")]
    public void Cuil_with_letters_or_wrong_length_is_rejected(string input)
        => Assert.Throws<ArgumentException>(() => ContactFormatPolicy.NormalizeCuil(input));

    [Fact]
    public void Empty_cuil_means_no_cuil() => Assert.Null(ContactFormatPolicy.NormalizeCuil("  "));

    [Fact]
    public void Phone_must_have_ten_digits_between_area_code_and_number()
    {
        Assert.Equal(("351", "5551234"), ContactFormatPolicy.NormalizePhone("351", "555-1234"));
        Assert.Throws<ArgumentException>(() => ContactFormatPolicy.NormalizePhone("1213213131313", "1111111111111111"));
        Assert.Throws<ArgumentException>(() => ContactFormatPolicy.NormalizePhone("351", "55512"));
        Assert.Throws<ArgumentException>(() => ContactFormatPolicy.NormalizePhone("351", "55512345"));
        Assert.Throws<ArgumentException>(() => ContactFormatPolicy.NormalizePhone("351", null));
    }
}
