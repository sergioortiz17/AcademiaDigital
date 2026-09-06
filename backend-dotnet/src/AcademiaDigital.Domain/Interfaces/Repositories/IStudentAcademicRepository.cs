using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudentAcademicRepository
{
    Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, CancellationToken ct = default);
    Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, int careerId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<long, StudentStudyPlan>> GetCurrentStudyPlansAsync(IEnumerable<long> studentIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, StudentStudyPlan>> GetCurrentStudyPlansByCareerAsync(long studentId, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, int careerId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCourse>> GetStudyPlanCoursesAsync(int studyPlanId, CancellationToken ct = default);
    Task<IReadOnlyList<CoursePrerequisite>> GetPrerequisitesAsync(int studyPlanId, CancellationToken ct = default);
    Task<StudentStudyPlan> AssignStudyPlanAsync(StudentStudyPlan studentStudyPlan, CancellationToken ct = default);

    /// <summary>¿El alumno ya tiene una asignación académica vigente para esta membresía de carrera?
    /// Se usa para NO pisar una asignación manual existente (Parte 2) al auto-asignar comisión.</summary>
    Task<bool> HasCurrentAcademicAssignmentAsync(long studentCareerId, CancellationToken ct = default);

    /// <summary>Agrega una asignación académica (comisión) reutilizando el DbContext compartido, para
    /// que participe de la transacción en curso del alta de inscripción (sin abrir una anidada).</summary>
    Task AddAcademicAssignmentAsync(StudentAcademicAssignment assignment, CancellationToken ct = default);
}
