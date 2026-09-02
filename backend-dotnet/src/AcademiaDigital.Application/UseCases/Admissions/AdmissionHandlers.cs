using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Admissions;

public sealed record AdmissionFormFieldDto(
    string Key,
    string Label,
    string Type,
    bool IsRequired,
    int SortOrder);

public sealed record AdmissionFormDto(
    int Id,
    string Slug,
    string Title,
    string? Description,
    string TermsText,
    int CareerId,
    string CareerName,
    int? CommissionId,
    string? CommissionCode,
    string? CommissionName,
    int? AcademicYear,
    int? YearNumber,
    string? Shift,
    int ReservationHours,
    int? Capacity,
    bool IsActive,
    IReadOnlyList<AdmissionFormFieldDto> Fields);

public sealed record AdmissionApplicationDto(
    Guid PublicId,
    string Status,
    DateTime? ReservationExpiresAt,
    DateTime CreatedAt);

public sealed record GetAdmissionFormQuery(string Slug);

public sealed class GetAdmissionFormQueryHandler(
    IAdmissionRepository repository,
    AdmissionFormPolicy formPolicy)
{
    public async Task<AdmissionFormDto> Handle(GetAdmissionFormQuery query, CancellationToken ct = default)
    {
        var form = await repository.FindActiveFormBySlugAsync(formPolicy.NormalizeSlug(query.Slug), ct)
            ?? throw new KeyNotFoundException("Admission form not found.");

        return Map(form);
    }

    internal static AdmissionFormDto Map(AdmissionForm form)
        => new(
            form.Id,
            form.Slug,
            form.Title,
            form.Description,
            form.TermsText,
            form.CareerId,
            form.Career.Name,
            form.CommissionId,
            form.Commission?.Code,
            form.Commission?.Name,
            form.Commission?.AcademicYear,
            form.Commission?.YearNumber,
            form.Commission?.Shift,
            form.ReservationHours,
            form.Capacity,
            form.IsActive,
            form.Fields
                .OrderBy(field => field.SortOrder)
                .Select(field => new AdmissionFormFieldDto(
                    field.Key,
                    field.Label,
                    field.Type.ToString(),
                    field.IsRequired,
                    field.SortOrder))
                .ToArray());
}

public sealed record CreateAdmissionApplicationCommand(
    string FormSlug,
    bool AcceptedTerms,
    IReadOnlyDictionary<string, string?> Fields,
    string? ChallengeToken = null,
    string? RemoteIpAddress = null);

public sealed class CreateAdmissionApplicationCommandHandler(
    IAdmissionRepository repository,
    AdmissionApplicationPolicy policy,
    AdmissionFormPolicy formPolicy,
    AdmissionCapacityPolicy capacityPolicy,
    AdmissionCapacityCoordinator capacityCoordinator,
    IAdmissionChallengeVerifier challengeVerifier,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AdmissionApplicationDto> Handle(
        CreateAdmissionApplicationCommand command,
        CancellationToken ct = default)
    {
        if (!await challengeVerifier.VerifyAsync(
                command.ChallengeToken,
                command.RemoteIpAddress,
                ct))
            throw new AdmissionChallengeRejectedException();

        var form = await repository.FindActiveFormBySlugAsync(
            formPolicy.NormalizeSlug(command.FormSlug), ct)
            ?? throw new KeyNotFoundException("Admission form not found.");

        var normalizedFields = policy.ValidateAndNormalize(form, command.Fields, command.AcceptedTerms);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var lockedForm = await repository.LockFormForCapacityAsync(form.Id, transactionCt)
                ?? throw new KeyNotFoundException("Admission form not found.");
            if (!lockedForm.IsActive)
                throw new KeyNotFoundException("Admission form not found.");

            var email = normalizedFields["email"];
            var dni = normalizedFields["dni"];
            if (await repository.ApplicationExistsAsync(lockedForm.Id, email, dni, transactionCt))
                throw new AdmissionApplicationAlreadyExistsException();

            await capacityCoordinator.ReconcileAsync(lockedForm, now, null, null, transactionCt);
            var occupied = await repository.CountCapacityOccupyingApplicationsAsync(lockedForm.Id, transactionCt);
            var hasSlot = capacityPolicy.HasAvailableSlot(lockedForm.Capacity, occupied);
            var initialStatus = hasSlot
                ? AdmissionApplicationStatus.PreEnrolled
                : AdmissionApplicationStatus.Waitlisted;
            var application = new AdmissionApplication
            {
                PublicId = Guid.NewGuid(),
                AdmissionFormId = lockedForm.Id,
                ApplicantEmail = email,
                ApplicantDni = dni,
                SubmittedFieldsJson = JsonSerializer.Serialize(
                    normalizedFields.OrderBy(field => field.Key)
                        .ToDictionary(field => field.Key, field => field.Value, StringComparer.OrdinalIgnoreCase)),
                Status = initialStatus,
                TermsAcceptedAt = now,
                ReservationExpiresAt = hasSlot ? now.AddHours(lockedForm.ReservationHours) : null,
                CreatedAt = now,
                UpdatedAt = now
            };
            application.StatusHistory.Add(new AdmissionApplicationStatusHistory
            {
                FromStatus = null,
                ToStatus = initialStatus,
                ChangedAt = now,
                Reason = hasSlot
                    ? "Public application submitted with a reserved slot."
                    : "Public application submitted to the FIFO waitlist."
            });

            var created = await repository.CreateApplicationAsync(application, transactionCt);
            return new AdmissionApplicationDto(
                created.PublicId,
                created.Status.ToString(),
                created.ReservationExpiresAt,
                created.CreatedAt);
        }, ct);
    }
}
