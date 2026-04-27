using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IEnrollmentRepository
{
    Task<IEnumerable<Enrollment>> GetByStudentAsync(long studentId, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetBySubjectAndPeriodAsync(int subjectId, int year, int semester, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByTeachingPositionAsync(int teachingPositionId, CancellationToken ct = default);
    Task<Enrollment?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<Enrollment?> FindByStudentAndSubjectAsync(long studentId, int subjectId, int year, int semester, CancellationToken ct = default);
    Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken ct = default);
    Task<Enrollment> UpdateAsync(Enrollment enrollment, CancellationToken ct = default);
    Task DeleteAsync(Enrollment enrollment, CancellationToken ct = default);
}
