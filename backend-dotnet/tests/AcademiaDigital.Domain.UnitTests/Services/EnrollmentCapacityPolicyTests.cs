using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class EnrollmentCapacityPolicyTests
{
    private readonly EnrollmentCapacityPolicy _policy = new();

    [Theory]
    [InlineData(EnrollmentCapacityPolicy.MorningShift)]
    [InlineData(EnrollmentCapacityPolicy.AfternoonShift)]
    [InlineData(EnrollmentCapacityPolicy.EveningShift)]
    public void EnsureValidShift_accepts_supported_values(string shift)
        => _policy.EnsureValidShift(shift);

    [Fact]
    public void EnsureValidShift_rejects_unknown_values()
    {
        var exception = Assert.Throws<ArgumentException>(() => _policy.EnsureValidShift("Morning"));

        Assert.Contains("Valores válidos: Mañana, Tarde, Noche", exception.Message);
    }

    [Fact]
    public void EnsureVacancy_rejects_a_full_shift()
    {
        var period = Period(morning: 2, afternoon: 1, evening: 3);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _policy.EnsureVacancy(period, (Morning: 0, Afternoon: 1, Evening: 0), "Tarde"));

        Assert.Equal("No hay vacantes disponibles para el turno 'Tarde'.", exception.Message);
    }

    [Fact]
    public void EnsureVacancy_accepts_capacity_remaining_in_selected_shift()
        => _policy.EnsureVacancy(
            Period(morning: 2, afternoon: 1, evening: 3),
            (Morning: 1, Afternoon: 1, Evening: 3),
            "Mañana");

    [Fact]
    public void EnsureQuotasCoverCurrentEnrollment_rejects_reducing_below_occupancy()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _policy.EnsureQuotasCoverCurrentEnrollment(
                (Morning: 2, Afternoon: 1, Evening: 0),
                morning: 1,
                afternoon: 1,
                evening: 0));

        Assert.Equal("Los cupos de inscripción no pueden ser menores que la ocupación actual.", exception.Message);
    }

    [Fact]
    public void EnsureValidQuotas_rejects_negative_values()
        => Assert.Throws<ArgumentOutOfRangeException>(() => _policy.EnsureValidQuotas(1, -1, 1));

    private static EnrollmentPeriod Period(int morning, int afternoon, int evening)
        => new()
        {
            QuotasMorning = morning,
            QuotasAfternoon = afternoon,
            QuotasEvening = evening
        };
}
