using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ITeacherDocumentRepository
{
    Task<IReadOnlyList<TeacherDocument>> GetByTeacherAsync(long teacherId, CancellationToken ct = default);
    Task<TeacherDocument?> FindAsync(long teacherId, long documentId, bool trackChanges, CancellationToken ct = default);
    Task<TeacherDocument> CreateVersionAsync(TeacherDocument document, CancellationToken ct = default);
    Task<TeacherDocument> UpdateAsync(TeacherDocument document, CancellationToken ct = default);
}
