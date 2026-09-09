using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AdmissionApplicationPolicyTests
{
    private readonly AdmissionApplicationPolicy _policy = new();

    [Fact]
    public void ValidateAndNormalize_trims_values_and_normalizes_identity_fields()
    {
        var result = _policy.ValidateAndNormalize(
            ValidForm(),
            new Dictionary<string, string?>
            {
                ["email"] = "  Applicant@Example.COM ",
                ["dni"] = " 12345678 ",
                ["firstName"] = " Ada "
            },
            acceptedTerms: true);

        Assert.Equal("applicant@example.com", result["email"]);
        Assert.Equal("12345678", result["dni"]);
        Assert.Equal("Ada", result["firstName"]);
    }

    [Fact]
    public void ValidateAndNormalize_rejects_an_inactive_form()
    {
        var form = ValidForm();
        form.IsActive = false;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _policy.ValidateAndNormalize(form, ValidFields(), acceptedTerms: true));

        Assert.Equal("El formulario de admisión no está activo.", exception.Message);
    }

    [Fact]
    public void ValidateAndNormalize_requires_terms_acceptance()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _policy.ValidateAndNormalize(ValidForm(), ValidFields(), acceptedTerms: false));

        Assert.Equal("Se deben aceptar los términos de admisión.", exception.Message);
    }

    [Fact]
    public void ValidateAndNormalize_reports_missing_required_fields()
    {
        var fields = ValidFields();
        fields.Remove("firstName");

        var exception = Assert.Throws<ArgumentException>(() =>
            _policy.ValidateAndNormalize(ValidForm(), fields, acceptedTerms: true));

        Assert.Contains("firstName", exception.Message);
    }

    [Fact]
    public void ValidateAndNormalize_rejects_unknown_fields()
    {
        var fields = ValidFields();
        fields["internalStatus"] = "Confirmed";

        var exception = Assert.Throws<ArgumentException>(() =>
            _policy.ValidateAndNormalize(ValidForm(), fields, acceptedTerms: true));

        Assert.Contains("internalStatus", exception.Message);
    }

    [Fact]
    public void ValidateAndNormalize_rejects_invalid_email()
    {
        var fields = ValidFields();
        fields["email"] = "invalid";

        var exception = Assert.Throws<ArgumentException>(() =>
            _policy.ValidateAndNormalize(ValidForm(), fields, acceptedTerms: true));

        Assert.Equal("El email de admisión no es válido.", exception.Message);
    }

    [Fact]
    public void ValidateAndNormalize_rejects_invalid_dni()
    {
        var fields = ValidFields();
        fields["dni"] = "12-34";

        var exception = Assert.Throws<ArgumentException>(() =>
            _policy.ValidateAndNormalize(ValidForm(), fields, acceptedTerms: true));

        Assert.Equal("El DNI de admisión no es válido.", exception.Message);
    }

    private static AdmissionForm ValidForm()
        => new()
        {
            IsActive = true,
            Fields =
            [
                Field("email", order: 1),
                Field("dni", order: 2),
                Field("firstName", order: 3)
            ]
        };

    private static AdmissionFormField Field(string key, int order)
        => new() { Key = key, Label = key, IsRequired = true, SortOrder = order };

    private static Dictionary<string, string?> ValidFields()
        => new()
        {
            ["email"] = "applicant@example.com",
            ["dni"] = "12345678",
            ["firstName"] = "Ada"
        };
}
