using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Admissions;

public sealed record AdmissionFormFieldInput(
    string Key,
    string Label,
    AdmissionFieldType Type,
    bool IsRequired,
    int SortOrder);

public sealed record CreateAdmissionFormCommand(
    int CareerId,
    int? CommissionId,
    string Slug,
    string Title,
    string? Description,
    string TermsText,
    int ReservationHours,
    int? Capacity,
    IReadOnlyList<AdmissionFormFieldInput> Fields);

public sealed class CreateAdmissionFormCommandHandler(
    IAdmissionRepository repository,
    ICareerRepository careerRepository,
    ICommissionRepository commissionRepository,
    AdmissionFormPolicy policy,
    AdmissionCapacityPolicy capacityPolicy,
    AdmissionTargetPolicy targetPolicy,
    TimeProvider timeProvider)
{
    public async Task<AdmissionFormDto> Handle(CreateAdmissionFormCommand command, CancellationToken ct = default)
    {
        var slug = policy.NormalizeSlug(command.Slug);
        var fields = command.Fields.Select(field => new AdmissionFormField
        {
            Key = field.Key.Trim(),
            Label = field.Label.Trim(),
            Type = field.Type,
            IsRequired = field.IsRequired,
            SortOrder = field.SortOrder
        }).ToArray();
        policy.ValidateDefinition(command.Title, command.TermsText, command.ReservationHours, fields);
        capacityPolicy.ValidateCapacity(command.Capacity);

        var career = await careerRepository.FindByIdAsync(command.CareerId, ct)
            ?? throw new KeyNotFoundException("Career not found.");
        if (!career.IsActive)
            throw new InvalidOperationException("Admission forms cannot be created for an inactive career.");
        Commission? commission = null;
        if (command.CommissionId.HasValue)
        {
            commission = await commissionRepository.FindByIdAsync(command.CommissionId.Value, ct)
                ?? throw new KeyNotFoundException("Commission not found.");
        }
        targetPolicy.Validate(career, commission, command.Capacity);
        if (await repository.FormSlugExistsAsync(slug, ct))
            throw new AdmissionFormSlugAlreadyExistsException(slug);
        if (commission is not null && await repository.CommissionTargetExistsAsync(commission.Id, ct))
            throw new AdmissionCommissionAlreadyAssignedException(commission.Id);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var form = new AdmissionForm
        {
            CareerId = career.Id,
            CommissionId = commission?.Id,
            Slug = slug,
            Title = command.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            TermsText = command.TermsText.Trim(),
            ReservationHours = command.ReservationHours,
            Capacity = command.Capacity,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Fields = fields
        };

        var created = await repository.CreateFormAsync(form, ct);
        created.Career = career;
        created.Commission = commission;
        return GetAdmissionFormQueryHandler.Map(created);
    }
}

public sealed record GetAdmissionFormsQuery();

public sealed class GetAdmissionFormsQueryHandler(IAdmissionRepository repository)
{
    public async Task<IReadOnlyList<AdmissionFormDto>> Handle(
        GetAdmissionFormsQuery query,
        CancellationToken ct = default)
        => (await repository.GetFormsAsync(ct)).Select(GetAdmissionFormQueryHandler.Map).ToArray();
}

public sealed record SetAdmissionFormActiveCommand(int FormId, bool IsActive);

public sealed class SetAdmissionFormActiveCommandHandler(
    IAdmissionRepository repository,
    TimeProvider timeProvider)
{
    public async Task<AdmissionFormDto> Handle(
        SetAdmissionFormActiveCommand command,
        CancellationToken ct = default)
    {
        var form = await repository.FindFormByIdAsync(command.FormId, ct)
            ?? throw new KeyNotFoundException("Admission form not found.");
        form.IsActive = command.IsActive;
        form.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        return GetAdmissionFormQueryHandler.Map(await repository.UpdateFormAsync(form, ct));
    }
}

public sealed record AdmissionApplicationSummaryDto(
    Guid PublicId,
    int AdmissionFormId,
    string FormSlug,
    string FormTitle,
    string ApplicantEmail,
    string ApplicantDni,
    string Status,
    DateTime? ReservationExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AdmissionStatusHistoryDto(
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt,
    long? ChangedByUserId,
    string? Reason);

public sealed record AdmissionApplicationDetailDto(
    AdmissionApplicationSummaryDto Application,
    IReadOnlyDictionary<string, string> Fields,
    IReadOnlyList<AdmissionStatusHistoryDto> History);

public sealed record AdmissionApplicationPageDto(
    IReadOnlyList<AdmissionApplicationSummaryDto> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record GetAdmissionApplicationsQuery(
    int? AdmissionFormId,
    AdmissionApplicationStatus? Status,
    string? Search,
    int Page,
    int PageSize);

public sealed class GetAdmissionApplicationsQueryHandler(IAdmissionRepository repository)
{
    public async Task<AdmissionApplicationPageDto> Handle(
        GetAdmissionApplicationsQuery query,
        CancellationToken ct = default)
    {
        if (query.Page < 1)
            throw new ArgumentException("Page must be greater than zero.");
        if (query.PageSize is < 1 or > 100)
            throw new ArgumentException("Page size must be between 1 and 100.");

        var (items, total) = await repository.GetApplicationsAsync(
            query.AdmissionFormId,
            query.Status,
            query.Search,
            query.Page,
            query.PageSize,
            ct);
        return new AdmissionApplicationPageDto(
            items.Select(AdmissionAdminMappings.Summary).ToArray(),
            query.Page,
            query.PageSize,
            total);
    }
}

public sealed record GetAdmissionApplicationQuery(Guid PublicId);

public sealed class GetAdmissionApplicationQueryHandler(IAdmissionRepository repository)
{
    public async Task<AdmissionApplicationDetailDto> Handle(
        GetAdmissionApplicationQuery query,
        CancellationToken ct = default)
    {
        var application = await repository.FindApplicationByPublicIdAsync(query.PublicId, false, ct)
            ?? throw new KeyNotFoundException("Admission application not found.");
        return AdmissionAdminMappings.Detail(application);
    }
}

public sealed record ChangeAdmissionApplicationStatusCommand(
    Guid PublicId,
    AdmissionApplicationStatus Status,
    string? Reason,
    long ChangedByUserId);

public sealed class ChangeAdmissionApplicationStatusCommandHandler(
    IAdmissionRepository repository,
    AdmissionStatusTransitionPolicy policy,
    AdmissionDocumentPolicy documentPolicy,
    AdmissionCapacityPolicy capacityPolicy,
    AdmissionCapacityCoordinator capacityCoordinator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AdmissionApplicationDetailDto> Handle(
        ChangeAdmissionApplicationStatusCommand command,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var application = await repository.FindApplicationByPublicIdAsync(
                command.PublicId, true, transactionCt)
                ?? throw new KeyNotFoundException("Admission application not found.");
            var form = await repository.LockFormForCapacityAsync(
                application.AdmissionFormId, transactionCt)
                ?? throw new KeyNotFoundException("Admission form not found.");
            policy.EnsureCanTransition(application.Status, command.Status, command.Reason);

            if (command.Status == AdmissionApplicationStatus.Confirmed)
            {
                var today = DateOnly.FromDateTime(now);
                var requiredDocuments = await repository.GetApplicableRequiredDocumentRequirementsAsync(
                    form.CareerId, today, transactionCt);
                var submittedDocuments = await repository.GetApplicationDocumentsAsync(
                    application.Id, false, transactionCt);
                documentPolicy.EnsureRequiredDocumentsApproved(requiredDocuments, submittedDocuments);
            }

            var previous = application.Status;
            if (previous == AdmissionApplicationStatus.Waitlisted
                && command.Status == AdmissionApplicationStatus.PreEnrolled)
            {
                await capacityCoordinator.ReconcileAsync(
                    form, now, command.ChangedByUserId, application.Id, transactionCt);
                var occupied = await repository.CountCapacityOccupyingApplicationsAsync(form.Id, transactionCt);
                if (!capacityPolicy.HasAvailableSlot(form.Capacity, occupied))
                    throw new InvalidOperationException("Admission form has no available capacity.");
                if (await repository.HasEarlierWaitlistedApplicationAsync(
                    form.Id, application.CreatedAt, application.Id, transactionCt))
                    throw new InvalidOperationException("An earlier admission application is waiting for capacity.");
            }

            application.Status = command.Status;
            application.UpdatedAt = now;
            if (command.Status == AdmissionApplicationStatus.PreEnrolled)
                application.ReservationExpiresAt = now.AddHours(form.ReservationHours);
            else if (command.Status == AdmissionApplicationStatus.Waitlisted)
                application.ReservationExpiresAt = null;

            var history = new AdmissionApplicationStatusHistory
            {
                AdmissionApplicationId = application.Id,
                FromStatus = previous,
                ToStatus = command.Status,
                ChangedAt = now,
                ChangedByUserId = command.ChangedByUserId,
                Reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim()
            };
            application.StatusHistory.Add(history);

            var updated = await repository.UpdateApplicationStatusAsync(application, history, transactionCt);
            if (command.Status == AdmissionApplicationStatus.Confirmed)
            {
                var (agreement, message) = AdmissionAgreementFactory.Create(updated, now);
                await repository.CreateAgreementWithOutboxAsync(agreement, message, transactionCt);
            }
            if (AdmissionCapacityPolicy.OccupiesCapacity(previous)
                && !AdmissionCapacityPolicy.OccupiesCapacity(command.Status))
            {
                await capacityCoordinator.ReconcileAsync(
                    form,
                    now,
                    command.ChangedByUserId,
                    command.Status == AdmissionApplicationStatus.Waitlisted ? application.Id : null,
                    transactionCt);
            }
            return AdmissionAdminMappings.Detail(updated);
        }, ct);
    }
}

internal static class AdmissionAdminMappings
{
    public static AdmissionApplicationSummaryDto Summary(AdmissionApplication application)
        => new(
            application.PublicId,
            application.AdmissionFormId,
            application.AdmissionForm.Slug,
            application.AdmissionForm.Title,
            application.ApplicantEmail,
            application.ApplicantDni,
            application.Status.ToString(),
            application.ReservationExpiresAt,
            application.CreatedAt,
            application.UpdatedAt);

    public static AdmissionApplicationDetailDto Detail(AdmissionApplication application)
        => new(
            Summary(application),
            JsonSerializer.Deserialize<Dictionary<string, string>>(application.SubmittedFieldsJson) ?? [],
            application.StatusHistory
                .OrderBy(history => history.ChangedAt)
                .Select(history => new AdmissionStatusHistoryDto(
                    history.FromStatus?.ToString(),
                    history.ToStatus.ToString(),
                    history.ChangedAt,
                    history.ChangedByUserId,
                    history.Reason))
                .ToArray());
}
