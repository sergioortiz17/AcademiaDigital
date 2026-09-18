using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AdmissionFormPolicyTests
{
    private readonly AdmissionFormPolicy _policy = new();

    [Fact]
    public void NormalizeSlug_trims_and_lowercases_a_valid_slug()
        => Assert.Equal("backend-2027", _policy.NormalizeSlug(" Backend-2027 "));

    [Theory]
    [InlineData(0)]
    [InlineData(721)]
    public void ValidateDefinition_rejects_an_invalid_reservation_window(int hours)
        => Assert.Throws<ArgumentException>(() =>
            _policy.ValidateDefinition("Ingreso", "Terms", hours, ValidFields()));

    [Fact]
    public void ValidateDefinition_rejects_duplicate_field_keys_case_insensitively()
    {
        var fields = ValidFields();
        fields.Add(Field("EMAIL", AdmissionFieldType.Email, true, 3));

        var exception = Assert.Throws<ArgumentException>(() =>
            _policy.ValidateDefinition("Ingreso", "Terms", 72, fields));

        Assert.Contains("duplicada", exception.Message);
    }

    [Fact]
    public void ValidateDefinition_requires_email_and_dni_with_the_expected_contract()
    {
        var fields = ValidFields();
        fields[0].IsRequired = false;

        var exception = Assert.Throws<ArgumentException>(() =>
            _policy.ValidateDefinition("Ingreso", "Terms", 72, fields));

        Assert.Contains("campo obligatorio 'email'", exception.Message);
    }

    [Fact]
    public void ValidateDefinition_accepts_a_valid_form()
        => _policy.ValidateDefinition("Ingreso", "Terms", 72, ValidFields());

    [Fact]
    public void ValidateDefinition_rejects_an_unknown_field_type()
    {
        var fields = ValidFields();
        fields[0].Type = (AdmissionFieldType)999;

        Assert.Throws<ArgumentException>(() =>
            _policy.ValidateDefinition("Ingreso", "Terms", 72, fields));
    }

    private static List<AdmissionFormField> ValidFields()
        =>
        [
            Field("email", AdmissionFieldType.Email, true, 1),
            Field("dni", AdmissionFieldType.Text, true, 2)
        ];

    private static AdmissionFormField Field(
        string key,
        AdmissionFieldType type,
        bool required,
        int sortOrder)
        => new()
        {
            Key = key,
            Label = key,
            Type = type,
            IsRequired = required,
            SortOrder = sortOrder
        };
}
