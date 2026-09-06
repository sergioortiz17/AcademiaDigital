using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class TeachingAssignmentPolicy
{
    public void ValidatePositionDefinition(
        int academicYear,
        int semester,
        int maxStudents,
        Course course,
        Commission commission)
    {
        if (academicYear is < 2000 or > 2100)
            throw new ArgumentException("El año académico debe estar entre 2000 y 2100.");
        if (semester is not (1 or 2))
            throw new ArgumentException("El cuatrimestre debe ser 1 o 2.");
        if (maxStudents is < 1 or > 1000)
            throw new ArgumentException("El máximo de estudiantes debe estar entre 1 y 1000.");
        if (!course.IsActive)
            throw new InvalidOperationException("La materia está inactiva.");
        if (!commission.IsActive)
            throw new InvalidOperationException("La comisión está inactiva.");
        if (commission.CareerId != course.CareerId)
            throw new ArgumentException("La materia y la comisión deben pertenecer a la misma carrera.");
        if (commission.AcademicYear != academicYear)
            throw new ArgumentException("La comisión y el cargo docente deben usar el mismo año académico.");
    }

    public void EnsurePositionCanChange(TeachingPosition position, bool hasAssignmentHistory)
    {
        if (!position.IsActive)
            throw new InvalidOperationException("El cargo docente está inactivo.");
        if (!position.IsVacant || position.TeacherId.HasValue)
            throw new InvalidOperationException("Un cargo docente asignado no puede modificarse.");
        if (hasAssignmentHistory)
            throw new InvalidOperationException("Un cargo docente con historial de asignaciones no puede cambiar su definición académica.");
    }

    public void EnsureCanDeactivate(TeachingPosition position)
    {
        if (!position.IsActive) return;
        if (!position.IsVacant || position.TeacherId.HasValue)
            throw new InvalidOperationException("Finalice la asignación docente actual antes de desactivar el cargo.");
    }

    public void EnsureCanAssign(TeachingPosition position, Teacher teacher, DateOnly startedOn)
    {
        if (!position.IsActive)
            throw new InvalidOperationException("El cargo docente está inactivo.");
        if (!position.IsVacant || position.TeacherId.HasValue)
            throw new InvalidOperationException("El cargo docente ya está asignado.");
        if (!teacher.IsActive || !teacher.User.IsActive)
            throw new InvalidOperationException("El docente está inactivo.");
        if (startedOn.Year != position.AcademicYear)
            throw new ArgumentException("La fecha de inicio de la asignación debe pertenecer al año académico del cargo docente.");
    }

    public void EnsureCanEnd(TeacherAssignment assignment, DateOnly endedOn, string reason)
    {
        if (!assignment.IsCurrent || assignment.EndedOn.HasValue)
            throw new InvalidOperationException("La asignación docente ya está cerrada.");
        if (endedOn < assignment.StartedOn)
            throw new ArgumentException("La fecha de fin de la asignación no puede ser anterior a su fecha de inicio.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Se requiere un motivo de finalización.");
    }
}
