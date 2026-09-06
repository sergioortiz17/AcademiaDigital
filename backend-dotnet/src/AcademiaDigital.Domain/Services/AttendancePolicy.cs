using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed record AttendanceMeasure(
    decimal EarnedUnits,
    decimal PossibleUnits,
    decimal? Percentage,
    bool IsAtRisk);

public sealed class AttendancePolicy
{
    public static readonly TimeSpan DefaultEditWindow = TimeSpan.FromHours(48);

    public DateTime EnsureCanCreateSession(
        TeachingPosition position,
        DateOnly sessionDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        AttendanceScope scope,
        int units,
        DateTime nowUtc)
    {
        if (!position.IsActive || position.CommissionId is null)
            throw new InvalidOperationException("La asistencia requiere un cargo docente activo con una comisión.");
        if (sessionDate.Year != position.AcademicYear)
            throw new ArgumentException("La fecha de la clase debe pertenecer al año académico del cargo docente.");
        if (sessionDate > DateOnly.FromDateTime(nowUtc))
            throw new ArgumentException("No se pueden crear clases de asistencia en el futuro.");
        if (units is < 1 or > 12)
            throw new ArgumentException("Las unidades de asistencia deben estar entre 1 y 12.");

        if (scope == AttendanceScope.ClassHour)
        {
            if (!startTime.HasValue || !endTime.HasValue || endTime <= startTime)
                throw new ArgumentException("La asistencia por hora de clase requiere una hora de inicio y de fin válidas.");
        }
        else if (startTime.HasValue || endTime.HasValue || units != 1)
        {
            throw new ArgumentException("La asistencia de jornada completa no acepta horarios y siempre usa una unidad.");
        }

        var occurrenceEnd = sessionDate.ToDateTime(
            scope == AttendanceScope.ClassHour ? endTime!.Value : TimeOnly.MaxValue,
            DateTimeKind.Utc);
        return occurrenceEnd.Add(DefaultEditWindow);
    }

    public void EnsureEditable(AttendanceSession session, DateTime nowUtc)
    {
        if (session.Status != AttendanceSessionStatus.Open)
            throw new InvalidOperationException("La clase de asistencia está cerrada.");
        if (!session.IsAdministrativelyReopened && nowUtc > session.EditDeadlineUtc)
            throw new InvalidOperationException("La ventana de edición de asistencia de 48 horas ha expirado.");
    }

    public void EnsureCanClose(AttendanceSession session)
    {
        if (session.Status != AttendanceSessionStatus.Open)
            throw new InvalidOperationException("La clase de asistencia ya está cerrada.");
    }

    public void EnsureCanReopen(AttendanceSession session, string reason)
    {
        if (session.Status != AttendanceSessionStatus.Closed)
            throw new InvalidOperationException("Solo una clase de asistencia cerrada puede reabrirse.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new ArgumentException("Se requiere un motivo de reapertura de al menos tres caracteres.");
    }

    public void EnsureRecordStatusCanBeLoaded(AttendanceRecordStatus status)
    {
        if (!Enum.IsDefined(status) || status == AttendanceRecordStatus.Justified)
            throw new ArgumentException("La carga masiva de asistencia acepta Presente, Tarde o Ausente; Justificado requiere una justificación auditada.");
    }

    public void EnsureCanJustify(AttendanceRecord record, string category, string reason, string? evidenceUrl)
    {
        if (record.Status is not AttendanceRecordStatus.Absent and not AttendanceRecordStatus.Late)
            throw new InvalidOperationException("Solo una ausencia o una llegada tarde pueden justificarse.");
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("La categoría de justificación es obligatoria.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new ArgumentException("El motivo de la justificación debe contener al menos tres caracteres.");
        if (!string.IsNullOrWhiteSpace(evidenceUrl)
            && !Uri.TryCreate(evidenceUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("La URL de evidencia debe ser absoluta.");
        if (!string.IsNullOrWhiteSpace(evidenceUrl)
            && !evidenceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !evidenceUrl.StartsWith("storage://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("La URL de evidencia debe usar HTTPS o una clave de almacenamiento.");
    }

    public AttendanceMeasure Calculate(
        IEnumerable<(AttendanceRecordStatus Status, int Units)> records,
        decimal? minimumAttendancePercentage)
    {
        decimal earned = 0;
        decimal possible = 0;
        foreach (var (status, units) in records)
        {
            if (status == AttendanceRecordStatus.Justified) continue;
            possible += units;
            earned += status switch
            {
                AttendanceRecordStatus.Present => units,
                AttendanceRecordStatus.Late => units * 0.5m,
                _ => 0m
            };
        }

        decimal? percentage = possible == 0 ? null : Math.Round(earned / possible * 100m, 2);
        return new AttendanceMeasure(
            earned,
            possible,
            percentage,
            percentage < minimumAttendancePercentage);
    }
}
