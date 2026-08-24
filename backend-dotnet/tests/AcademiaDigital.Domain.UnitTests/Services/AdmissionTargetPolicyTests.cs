using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AdmissionTargetPolicyTests
{
    private readonly AdmissionTargetPolicy policy = new();
    private readonly Career career = new() { Id = 10, IsActive = true };

    [Fact]
    public void Validate_accepts_a_general_form_without_capacity()
        => policy.Validate(career, null, null);

    [Fact]
    public void Validate_requires_capacity_for_a_commission_target()
    {
        var commission = new Commission { Id = 20, CareerId = 10, IsActive = true };

        var exception = Assert.Throws<ArgumentException>(() => policy.Validate(career, commission, null));

        Assert.Contains("Capacity is required", exception.Message);
    }

    [Fact]
    public void Validate_rejects_an_inactive_commission()
    {
        var commission = new Commission { Id = 20, CareerId = 10, IsActive = false };

        Assert.Throws<InvalidOperationException>(() => policy.Validate(career, commission, 20));
    }

    [Fact]
    public void Validate_rejects_a_commission_from_another_career()
    {
        var commission = new Commission { Id = 20, CareerId = 11, IsActive = true };

        Assert.Throws<InvalidOperationException>(() => policy.Validate(career, commission, 20));
    }

    [Fact]
    public void ValidateCapacity_rejects_unlimited_capacity_for_an_existing_target()
        => Assert.Throws<ArgumentException>(() => policy.ValidateCapacity(20, null));
}
