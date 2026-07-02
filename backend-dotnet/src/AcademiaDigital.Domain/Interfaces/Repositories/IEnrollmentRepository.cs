using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

// Flat row returned by the optimized period-students query
public sealed record EnrollmentStudentRow(
    long StudentId,
    string FullName,
    string Dni,
    string? Shift,
    System.DateTime EnrollmentDate,
    string CourseName);

public interface IEnrollmentRepository
{
    Task<IEnumerable<Enrollment>> GetByStudentAsync(long studentId, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByCourseAndPeriodAsync(int courseId, int year, int semester, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByTeachingPositionAsync(int teachingPositionId, CancellationToken ct = default);
    Task<Enrollment?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<Enrollment?> FindByStudentAndCourseAsync(long studentId, int courseId, int year, int semester, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByEnrollmentPeriodAsync(int periodId, CancellationToken ct = default);
    // Optimized projection: one SQL query, groups by student in-memory with a dictionary
    Task<IReadOnlyList<EnrollmentStudentRow>> GetStudentRowsByPeriodAsync(int periodId, CancellationToken ct = default);
    Task DeleteByStudentAndPeriodAsync(long studentId, int periodId, CancellationToken ct = default);
    Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken ct = default);
    Task<Enrollment> UpdateAsync(Enrollment enrollment, CancellationToken ct = default);
    Task DeleteAsync(Enrollment enrollment, CancellationToken ct = default);
}
