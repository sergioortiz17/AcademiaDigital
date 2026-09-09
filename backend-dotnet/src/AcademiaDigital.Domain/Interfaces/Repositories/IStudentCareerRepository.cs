using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudentCareerRepository
{
    Task<IReadOnlyList<StudentCareer>> GetByStudentAsync(long studentId, CancellationToken ct = default);
    Task<StudentCareer?> FindAsync(long studentId, int careerId, bool activeOnly = true, CancellationToken ct = default);
    Task<StudentCareer> CreateAsync(StudentCareer studentCareer, CancellationToken ct = default);
    Task<bool> ExistsForCareerAsync(int careerId, CancellationToken ct = default);
}
