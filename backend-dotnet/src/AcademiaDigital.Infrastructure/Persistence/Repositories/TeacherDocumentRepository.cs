using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class TeacherDocumentRepository(AppDbContext db) : ITeacherDocumentRepository
{
    public async Task<IReadOnlyList<TeacherDocument>> GetByTeacherAsync(
        long teacherId,
        CancellationToken ct = default)
        => await db.TeacherDocuments.AsNoTracking()
            .Where(document => document.TeacherId == teacherId)
            .OrderBy(document => document.DocumentType)
            .ThenByDescending(document => document.Version)
            .ToArrayAsync(ct);

    public Task<TeacherDocument?> FindAsync(
        long teacherId,
        long documentId,
        bool trackChanges,
        CancellationToken ct = default)
    {
        IQueryable<TeacherDocument> query = db.TeacherDocuments
            .Where(document => document.TeacherId == teacherId && document.Id == documentId);
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public async Task<TeacherDocument> CreateVersionAsync(
        TeacherDocument document,
        CancellationToken ct = default)
    {
        var teacher = await db.Teachers
            .FromSqlInterpolated($"SELECT * FROM \"Teachers\" WHERE id = {document.TeacherId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Teacher not found.");
        if (!teacher.IsActive)
            throw new InvalidOperationException("Documents cannot be submitted for an inactive teacher.");

        var versions = await db.TeacherDocuments
            .Where(existing => existing.TeacherId == document.TeacherId
                && existing.DocumentType == document.DocumentType)
            .ToArrayAsync(ct);
        document.Version = versions.Length == 0 ? 1 : versions.Max(existing => existing.Version) + 1;
        foreach (var current in versions.Where(existing =>
                     existing.Status is StudentDocumentStatus.Submitted or StudentDocumentStatus.Approved))
            current.Status = StudentDocumentStatus.Expired;

        db.TeacherDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<TeacherDocument> UpdateAsync(TeacherDocument document, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
        return document;
    }
}
