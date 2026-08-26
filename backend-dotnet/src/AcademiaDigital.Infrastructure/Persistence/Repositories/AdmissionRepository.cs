using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class AdmissionRepository(AppDbContext db) : IAdmissionRepository
{
    public Task<AdmissionForm?> FindActiveFormBySlugAsync(string slug, CancellationToken ct = default)
        => db.AdmissionForms
            .AsNoTracking()
            .Include(form => form.Career)
            .Include(form => form.Commission)
            .Include(form => form.Fields.OrderBy(field => field.SortOrder))
            .FirstOrDefaultAsync(form => form.Slug == slug
                && form.IsActive
                && (form.CommissionId == null || form.Commission!.IsActive), ct);

    public async Task<IReadOnlyList<AdmissionForm>> GetFormsAsync(CancellationToken ct = default)
        => await db.AdmissionForms
            .AsNoTracking()
            .Include(form => form.Career)
            .Include(form => form.Commission)
            .Include(form => form.Fields.OrderBy(field => field.SortOrder))
            .OrderBy(form => form.Title)
            .ToArrayAsync(ct);

    public Task<AdmissionForm?> FindFormByIdAsync(int id, CancellationToken ct = default)
        => db.AdmissionForms
            .Include(form => form.Career)
            .Include(form => form.Commission)
            .Include(form => form.Fields.OrderBy(field => field.SortOrder))
            .FirstOrDefaultAsync(form => form.Id == id, ct);

    public Task<AdmissionForm?> LockFormForCapacityAsync(int id, CancellationToken ct = default)
        => db.AdmissionForms
            .FromSqlInterpolated($"SELECT * FROM [AdmissionForms] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {id}")
            .SingleOrDefaultAsync(ct);

    public Task<bool> FormSlugExistsAsync(string slug, CancellationToken ct = default)
        => db.AdmissionForms.AsNoTracking().AnyAsync(form => form.Slug == slug, ct);

    public Task<bool> CommissionTargetExistsAsync(int commissionId, CancellationToken ct = default)
        => db.AdmissionForms.AsNoTracking().AnyAsync(form => form.CommissionId == commissionId, ct);

    public async Task<AdmissionForm> CreateFormAsync(AdmissionForm form, CancellationToken ct = default)
    {
        db.AdmissionForms.Add(form);
        try
        {
            await db.SaveChangesAsync(ct);
            return form;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            if (form.CommissionId.HasValue
                && exception.InnerException.Message.Contains("commission_id", StringComparison.OrdinalIgnoreCase))
                throw new AdmissionCommissionAlreadyAssignedException(form.CommissionId.Value);
            throw new AdmissionFormSlugAlreadyExistsException(form.Slug);
        }
    }

    public async Task<AdmissionForm> UpdateFormAsync(AdmissionForm form, CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return form;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The admission form changed. Reload it and retry.");
        }
    }

    public Task<bool> ApplicationExistsAsync(
        int admissionFormId,
        string applicantEmail,
        string applicantDni,
        CancellationToken ct = default)
        => db.AdmissionApplications.AsNoTracking().AnyAsync(
            application => application.AdmissionFormId == admissionFormId
                && (application.ApplicantEmail == applicantEmail || application.ApplicantDni == applicantDni),
            ct);

    public async Task<AdmissionApplication> CreateApplicationAsync(
        AdmissionApplication application,
        CancellationToken ct = default)
    {
        db.AdmissionApplications.Add(application);
        try
        {
            await db.SaveChangesAsync(ct);
            return application;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new AdmissionApplicationAlreadyExistsException();
        }
    }

    public Task<int> CountCapacityOccupyingApplicationsAsync(
        int admissionFormId,
        CancellationToken ct = default)
        => db.AdmissionApplications.CountAsync(application =>
            application.AdmissionFormId == admissionFormId
            && (application.Status == AdmissionApplicationStatus.PreEnrolled
                || application.Status == AdmissionApplicationStatus.Enrolled
                || application.Status == AdmissionApplicationStatus.Confirmed),
            ct);

    public async Task<IReadOnlyList<AdmissionApplication>> GetExpiredReservationsAsync(
        int admissionFormId,
        DateTime expiresAtOrBefore,
        CancellationToken ct = default)
        => await db.AdmissionApplications
            .Include(application => application.AdmissionForm)
            .Include(application => application.StatusHistory)
            .Where(application =>
                application.AdmissionFormId == admissionFormId
                && (application.Status == AdmissionApplicationStatus.PreEnrolled
                    || application.Status == AdmissionApplicationStatus.Enrolled)
                && application.ReservationExpiresAt != null
                && application.ReservationExpiresAt <= expiresAtOrBefore)
            .OrderBy(application => application.ReservationExpiresAt)
            .ThenBy(application => application.Id)
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<AdmissionApplication>> GetWaitlistedApplicationsAsync(
        int admissionFormId,
        int limit,
        long? excludedApplicationId,
        CancellationToken ct = default)
        => await db.AdmissionApplications
            .Include(application => application.AdmissionForm)
            .Include(application => application.StatusHistory)
            .Where(application =>
                application.AdmissionFormId == admissionFormId
                && application.Status == AdmissionApplicationStatus.Waitlisted
                && (!excludedApplicationId.HasValue || application.Id != excludedApplicationId.Value))
            .OrderBy(application => application.CreatedAt)
            .ThenBy(application => application.Id)
            .Take(limit)
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<int>> GetFormIdsWithExpiredReservationsAsync(
        DateTime expiresAtOrBefore,
        CancellationToken ct = default)
        => await db.AdmissionApplications
            .AsNoTracking()
            .Where(application =>
                (application.Status == AdmissionApplicationStatus.PreEnrolled
                    || application.Status == AdmissionApplicationStatus.Enrolled)
                && application.ReservationExpiresAt != null
                && application.ReservationExpiresAt <= expiresAtOrBefore)
            .Select(application => application.AdmissionFormId)
            .Distinct()
            .OrderBy(id => id)
            .ToArrayAsync(ct);

    public Task<bool> HasEarlierWaitlistedApplicationAsync(
        int admissionFormId,
        DateTime createdAt,
        long applicationId,
        CancellationToken ct = default)
        => db.AdmissionApplications.AsNoTracking().AnyAsync(application =>
            application.AdmissionFormId == admissionFormId
            && application.Status == AdmissionApplicationStatus.Waitlisted
            && (application.CreatedAt < createdAt
                || (application.CreatedAt == createdAt && application.Id < applicationId)),
            ct);

    public async Task<(IReadOnlyList<AdmissionApplication> Items, int Total)> GetApplicationsAsync(
        int? admissionFormId,
        AdmissionApplicationStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.AdmissionApplications
            .AsNoTracking()
            .Include(application => application.AdmissionForm)
            .AsQueryable();

        if (admissionFormId.HasValue)
            query = query.Where(application => application.AdmissionFormId == admissionFormId.Value);
        if (status.HasValue)
            query = query.Where(application => application.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(application =>
                application.ApplicantEmail.ToLower().Contains(normalizedSearch)
                || application.ApplicantDni.ToLower().Contains(normalizedSearch));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(application => application.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);
        return (items, total);
    }

    public Task<AdmissionApplication?> FindApplicationByPublicIdAsync(
        Guid publicId,
        bool trackChanges,
        CancellationToken ct = default)
    {
        IQueryable<AdmissionApplication> query = db.AdmissionApplications
            .Include(application => application.AdmissionForm)
                .ThenInclude(form => form.Career)
            .Include(application => application.StatusHistory);
        if (!trackChanges)
            query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(application => application.PublicId == publicId, ct);
    }

    public async Task<AdmissionApplication> UpdateApplicationStatusAsync(
        AdmissionApplication application,
        AdmissionApplicationStatusHistory history,
        CancellationToken ct = default)
    {
        if (db.Entry(history).State == EntityState.Detached)
            db.AdmissionApplicationStatusHistory.Add(history);
        try
        {
            await db.SaveChangesAsync(ct);
            return application;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AdmissionApplicationConcurrencyException();
        }
    }

    public Task<DocumentRequirement?> FindDocumentRequirementAsync(int id, CancellationToken ct = default)
        => db.DocumentRequirements.AsNoTracking().FirstOrDefaultAsync(requirement => requirement.Id == id, ct);

    public async Task<IReadOnlyList<DocumentRequirement>> GetApplicableRequiredDocumentRequirementsAsync(
        int careerId,
        DateOnly date,
        CancellationToken ct = default)
        => await db.DocumentRequirements.AsNoTracking()
            .Where(requirement => requirement.IsRequired
                && requirement.IsActive
                && (requirement.CareerId == null || requirement.CareerId == careerId)
                && (requirement.ValidFrom == null || requirement.ValidFrom <= date)
                && (requirement.ValidTo == null || requirement.ValidTo >= date))
            .OrderBy(requirement => requirement.Code)
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<AdmissionApplicationDocument>> GetApplicationDocumentsAsync(
        long admissionApplicationId,
        bool trackChanges,
        CancellationToken ct = default)
    {
        IQueryable<AdmissionApplicationDocument> query = db.AdmissionApplicationDocuments
            .Include(document => document.DocumentRequirement)
            .Where(document => document.AdmissionApplicationId == admissionApplicationId);
        if (!trackChanges)
            query = query.AsNoTracking();
        return await query.OrderByDescending(document => document.SubmittedAt).ThenByDescending(document => document.Id)
            .ToArrayAsync(ct);
    }

    public Task<AdmissionApplicationDocument?> FindApplicationDocumentAsync(
        Guid applicationPublicId,
        long documentId,
        bool trackChanges,
        CancellationToken ct = default)
    {
        IQueryable<AdmissionApplicationDocument> query = db.AdmissionApplicationDocuments
            .Include(document => document.DocumentRequirement)
            .Where(document => document.Id == documentId
                && document.AdmissionApplication.PublicId == applicationPublicId);
        if (!trackChanges)
            query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public async Task<AdmissionApplicationDocument> CreateApplicationDocumentAsync(
        AdmissionApplicationDocument document,
        CancellationToken ct = default)
    {
        var currentDocuments = await db.AdmissionApplicationDocuments
            .Where(existing => existing.AdmissionApplicationId == document.AdmissionApplicationId
                && existing.DocumentRequirementId == document.DocumentRequirementId
                && (existing.Status == StudentDocumentStatus.Submitted
                    || existing.Status == StudentDocumentStatus.Approved))
            .ToArrayAsync(ct);
        foreach (var currentDocument in currentDocuments)
            currentDocument.Status = StudentDocumentStatus.Expired;

        db.AdmissionApplicationDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<AdmissionApplicationDocument> UpdateApplicationDocumentAsync(
        AdmissionApplicationDocument document,
        CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<AdmissionAgreement> CreateAgreementWithOutboxAsync(
        AdmissionAgreement agreement,
        OutboxMessage message,
        CancellationToken ct = default)
    {
        db.AdmissionAgreements.Add(agreement);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return agreement;
    }

    public Task<AdmissionAgreement?> FindAgreementByApplicationPublicIdAsync(
        Guid publicId,
        bool trackChanges,
        CancellationToken ct = default)
    {
        IQueryable<AdmissionAgreement> query = db.AdmissionAgreements
            .Include(agreement => agreement.AdmissionApplication)
                .ThenInclude(application => application.AdmissionForm)
                    .ThenInclude(form => form.Career)
            .Where(agreement => agreement.AdmissionApplication.PublicId == publicId);
        if (!trackChanges)
            query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetClaimableOutboxMessagesAsync(
        DateTime availableAtOrBefore,
        DateTime processingStartedBefore,
        int limit,
        CancellationToken ct = default)
        => await db.OutboxMessages
            .Where(message => message.Type == "AdmissionAgreementConfirmed"
                && message.AvailableAt <= availableAtOrBefore
                && (message.Status == OutboxMessageStatus.Pending
                    || message.Status == OutboxMessageStatus.Failed
                    || (message.Status == OutboxMessageStatus.Processing
                        && message.ProcessingStartedAt < processingStartedBefore)))
            .OrderBy(message => message.OccurredAt)
            .ThenBy(message => message.Id)
            .Take(limit)
            .ToArrayAsync(ct);
}
