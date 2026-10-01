using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class ExamTablePolicy
{
    public void EnsureCanCreate(
        int academicYear,
        int callNumber,
        DateTime examDateUtc,
        DateTime registrationDeadlineUtc,
        string location,
        IReadOnlyCollection<ExamTribunalMember> tribunal,
        DateTime nowUtc)
    {
        if (academicYear != examDateUtc.Year)
            throw new ArgumentException("La fecha del examen debe pertenecer al año académico seleccionado.");
        if (callNumber < 1 || callNumber > 10)
            throw new ArgumentException("El número de llamado debe estar entre uno y diez.");
        if (examDateUtc < nowUtc)
            throw new ArgumentException("Una mesa de examen no puede programarse en el pasado.");
        if (registrationDeadlineUtc <= nowUtc || registrationDeadlineUtc > examDateUtc)
            throw new ArgumentException("La fecha límite de inscripción debe ser futura y no posterior a la fecha del examen.");
        if (string.IsNullOrWhiteSpace(location) || location.Trim().Length > 200)
            throw new ArgumentException("Se requiere un lugar de hasta 200 caracteres.");
        if (tribunal.Count < 2 || tribunal.Count > 5)
            throw new ArgumentException("El tribunal requiere entre dos y cinco docentes.");
        if (tribunal.Select(item => item.TeacherId).Distinct().Count() != tribunal.Count)
            throw new ArgumentException("Un docente no puede aparecer dos veces en el tribunal.");
        if (tribunal.Count(item => item.Role == ExamTribunalRole.President) != 1
            || !tribunal.Any(item => item.Role == ExamTribunalRole.Vocal))
            throw new ArgumentException("El tribunal requiere exactamente un presidente y al menos un vocal.");
    }

    public void EnsureCanRegister(ExamTable table, Enrollment enrollment, DateTime nowUtc)
    {
        if (table.Status != ExamTableStatus.Open || nowUtc > table.RegistrationDeadlineUtc)
            throw new InvalidOperationException("La inscripción a esta mesa de examen está cerrada.");
        if (enrollment.CourseId != table.CourseId)
            throw new ArgumentException("La inscripción no pertenece a la materia de la mesa de examen.");
        if (enrollment.Status != EnrollmentStatus.Regularized)
            throw new InvalidOperationException("Solo una inscripción regularizada puede inscribirse a un examen final.");
    }

    public void EnsureCanStartGrading(ExamTable table)
    {
        if (table.Status != ExamTableStatus.Open)
            throw new InvalidOperationException("Solo una mesa de examen abierta puede comenzar la calificación.");
        if (table.Registrations.Count == 0)
            throw new InvalidOperationException("Una mesa de examen sin inscripciones no puede comenzar la calificación.");
    }

    public void EnsureCanRecordResults(ExamTable table)
    {
        if (table.Status != ExamTableStatus.Grading)
            throw new InvalidOperationException("Los resultados solo pueden registrarse mientras la mesa de examen está en calificación.");
    }

    public void EnsureResultIsValid(ExamResultOutcome outcome, decimal? grade, decimal minimumPassingGrade)
    {
        if (minimumPassingGrade < 1m || minimumPassingGrade > 10m)
            throw new InvalidOperationException("La nota mínima de aprobación del examen final debe estar entre uno y diez.");
        if (outcome == ExamResultOutcome.Absent && grade.HasValue)
            throw new ArgumentException("Un resultado de ausente no puede tener nota.");
        if (outcome != ExamResultOutcome.Absent && (!grade.HasValue || grade < 0m || grade > 10m))
            throw new ArgumentException("Un resultado de examen presente requiere una nota entre cero y diez.");
        if (outcome == ExamResultOutcome.Passed && grade < minimumPassingGrade)
            throw new ArgumentException("Un resultado aprobado debe alcanzar la nota mínima de aprobación del examen final.");
        if (outcome == ExamResultOutcome.Failed && grade >= minimumPassingGrade)
            throw new ArgumentException("Un resultado desaprobado debe estar por debajo de la nota mínima de aprobación del examen final.");
    }

    public void EnsureCanPublish(ExamTable table)
    {
        if (table.Status != ExamTableStatus.Grading)
            throw new InvalidOperationException("Solo una mesa de examen en calificación puede publicarse.");
        if (table.Registrations.Count == 0
            || table.Registrations.Any(registration => registration.GradeRevisions.All(revision => !revision.IsCurrent)))
            throw new InvalidOperationException("Cada inscripción requiere un resultado antes de la publicación.");
    }

    public void EnsureCanReopen(ExamTable table, string reason)
    {
        if (table.Status != ExamTableStatus.Published)
            throw new InvalidOperationException("Solo una mesa de examen publicada puede reabrirse.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3 || reason.Trim().Length > 1000)
            throw new ArgumentException("Se requiere un motivo de reapertura de entre 3 y 1000 caracteres.");
    }
}
