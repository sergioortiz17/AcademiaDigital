using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IRematriculationRepository
{
    Task<StudentCareer?> LockActiveStudentCareerAsync(
        long studentId,
        int careerId,
        CancellationToken ct = default);
    Task<StudentRematriculation?> FindByCycleAsync(
        long studentCareerId,
        int academicYear,
        CancellationToken ct = default);
    Task<int?> GetLatestAcademicYearAsync(long studentCareerId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentAcademicAssignment>> GetCurrentAssignmentsAsync(
        long studentCareerId,
        CancellationToken ct = default);
    Task<StudentStudyPlan?> FindCurrentStudyPlanAsync(
        long studentCareerId,
        CancellationToken ct = default);
    Task<StudentRematriculation> CreateAsync(
        StudentRematriculation rematriculation,
        StudentAcademicAssignment assignment,
        StudentStudyPlan? newStudyPlan,
        CancellationToken ct = default);
}
