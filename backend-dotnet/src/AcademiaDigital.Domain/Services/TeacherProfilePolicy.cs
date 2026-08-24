using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class TeacherProfilePolicy
{
    public string NormalizeEmployeeNumber(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("Employee number is required.");

        return employeeNumber.Trim().ToUpperInvariant();
    }

    public void ValidateHireDate(DateTime hireDate, DateTime now)
    {
        if (hireDate.Date > now.Date)
            throw new ArgumentException("Hire date cannot be in the future.");
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
