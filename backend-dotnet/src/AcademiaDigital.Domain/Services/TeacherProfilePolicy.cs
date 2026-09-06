using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class TeacherProfilePolicy
{
    public string NormalizeEmployeeNumber(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("El número de legajo es obligatorio.");

        return employeeNumber.Trim().ToUpperInvariant();
    }

    public void ValidateHireDate(DateTime hireDate, DateTime now)
    {
        if (hireDate.Date > now.Date)
            throw new ArgumentException("La fecha de ingreso no puede ser futura.");
    }

    public void Deactivate(Teacher teacher, long actorUserId, string? reason, DateTime now)
    {
        if (!teacher.IsActive)
            return;

        teacher.IsActive = false;
        teacher.DeactivatedAt = now;
        teacher.DeactivatedByUserId = actorUserId;
        teacher.DeactivationReason = string.IsNullOrWhiteSpace(reason)
            ? "Administrative deactivation."
            : reason.Trim();
    }
}
