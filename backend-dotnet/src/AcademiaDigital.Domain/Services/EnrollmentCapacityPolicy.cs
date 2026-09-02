using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class EnrollmentCapacityPolicy
{
    public const string MorningShift = "Mañana";
    public const string AfternoonShift = "Tarde";
    public const string EveningShift = "Noche";

    public void EnsureValidShift(string shift)
    {
        if (shift is not MorningShift and not AfternoonShift and not EveningShift)
            throw new ArgumentException(
                $"Invalid shift '{shift}'. Valid values: {MorningShift}, {AfternoonShift}, {EveningShift}.");
    }

    public void EnsureValidQuotas(int morning, int afternoon, int evening)
    {
        if (morning < 0 || afternoon < 0 || evening < 0)
            throw new ArgumentOutOfRangeException(nameof(morning), "Enrollment quotas cannot be negative.");
    }

    public void EnsureVacancy(
        EnrollmentPeriod period,
        (int Morning, int Afternoon, int Evening) enrolled,
        string shift)
    {
        EnsureValidShift(shift);

        var (quota, occupied) = shift switch
        {
            MorningShift => (period.QuotasMorning, enrolled.Morning),
            AfternoonShift => (period.QuotasAfternoon, enrolled.Afternoon),
            EveningShift => (period.QuotasEvening, enrolled.Evening),
            _ => throw new InvalidOperationException("Unsupported enrollment shift.")
        };

        if (occupied >= quota)
            throw new InvalidOperationException($"No vacancies are available for shift '{shift}'.");
    }

    public void EnsureQuotasCoverCurrentEnrollment(
        (int Morning, int Afternoon, int Evening) enrolled,
        int morning,
        int afternoon,
        int evening)
    {
        EnsureValidQuotas(morning, afternoon, evening);

        if (morning < enrolled.Morning || afternoon < enrolled.Afternoon || evening < enrolled.Evening)
            throw new InvalidOperationException("Enrollment quotas cannot be lower than current occupancy.");
    }
}
