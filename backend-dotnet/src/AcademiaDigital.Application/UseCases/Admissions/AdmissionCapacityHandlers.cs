using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Admissions;

public sealed record AdmissionCapacityReconciliationResult(int Expired, int Promoted);

public sealed class AdmissionCapacityCoordinator(IAdmissionRepository repository)
{
    public async Task<AdmissionCapacityReconciliationResult> ReconcileAsync(
        AdmissionForm form,
        DateTime now,
        long? changedByUserId,
        long? excludedWaitlistedApplicationId,
        CancellationToken ct = default)
    {
        var expired = await repository.GetExpiredReservationsAsync(form.Id, now, ct);
        foreach (var application in expired)
        {
            await ApplySystemTransitionAsync(
                application,
                AdmissionApplicationStatus.Expired,
                now,
                changedByUserId,
                "Reservation expired during capacity reconciliation.",
                ct);
        }

        var occupied = await repository.CountCapacityOccupyingApplicationsAsync(form.Id, ct);
        var available = form.Capacity is null
            ? 100_000
            : Math.Max(0, form.Capacity.Value - occupied);
        if (available == 0)
            return new AdmissionCapacityReconciliationResult(expired.Count, 0);

        var waitlisted = await repository.GetWaitlistedApplicationsAsync(
            form.Id, available, excludedWaitlistedApplicationId, ct);
        foreach (var application in waitlisted)
        {
            application.ReservationExpiresAt = now.AddHours(form.ReservationHours);
            await ApplySystemTransitionAsync(
                application,
                AdmissionApplicationStatus.PreEnrolled,
                now,
                changedByUserId,
                "Promoted from the FIFO waitlist after capacity became available.",
                ct);
        }

        return new AdmissionCapacityReconciliationResult(expired.Count, waitlisted.Count);
    }

    private async Task ApplySystemTransitionAsync(
        AdmissionApplication application,
        AdmissionApplicationStatus target,
        DateTime now,
        long? changedByUserId,
        string reason,
        CancellationToken ct)
    {
        var previous = application.Status;
        application.Status = target;
        application.UpdatedAt = now;
        var history = new AdmissionApplicationStatusHistory
        {
            AdmissionApplicationId = application.Id,
            FromStatus = previous,
            ToStatus = target,
            ChangedAt = now,
            ChangedByUserId = changedByUserId,
            Reason = reason
        };
        application.StatusHistory.Add(history);
        await repository.UpdateApplicationStatusAsync(application, history, ct);
    }
}

public sealed record SetAdmissionFormCapacityCommand(int FormId, int? Capacity, long ChangedByUserId);

public sealed class SetAdmissionFormCapacityCommandHandler(
    IAdmissionRepository repository,
    AdmissionCapacityPolicy policy,
    AdmissionTargetPolicy targetPolicy,
    AdmissionCapacityCoordinator coordinator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AdmissionFormDto> Handle(
        SetAdmissionFormCapacityCommand command,
        CancellationToken ct = default)
    {
        policy.ValidateCapacity(command.Capacity);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            _ = await repository.LockFormForCapacityAsync(command.FormId, transactionCt)
                ?? throw new KeyNotFoundException("Admission form not found.");
            var form = await repository.FindFormByIdAsync(command.FormId, transactionCt)
                ?? throw new KeyNotFoundException("Admission form not found.");
            targetPolicy.ValidateCapacity(form.CommissionId, command.Capacity);
            var occupied = await repository.CountCapacityOccupyingApplicationsAsync(form.Id, transactionCt);
            if (command.Capacity.HasValue && command.Capacity.Value < occupied)
                throw new InvalidOperationException("Admission form capacity cannot be lower than its occupied capacity.");

            form.Capacity = command.Capacity;
            form.UpdatedAt = now;
            await repository.UpdateFormAsync(form, transactionCt);
            await coordinator.ReconcileAsync(form, now, command.ChangedByUserId, null, transactionCt);
            return GetAdmissionFormQueryHandler.Map(form);
        }, ct);
    }
}

public sealed record ProcessAdmissionExpirationsCommand(int? FormId, long ChangedByUserId);
public sealed record ProcessAdmissionExpirationsDto(int FormsProcessed, int Expired, int Promoted);

public sealed class ProcessAdmissionExpirationsCommandHandler(
    IAdmissionRepository repository,
    AdmissionCapacityCoordinator coordinator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProcessAdmissionExpirationsDto> Handle(
        ProcessAdmissionExpirationsCommand command,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var formIds = command.FormId.HasValue
            ? new[] { command.FormId.Value }
            : await repository.GetFormIdsWithExpiredReservationsAsync(now, ct);

        return await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var expired = 0;
            var promoted = 0;
            foreach (var formId in formIds.Order())
            {
                var form = await repository.LockFormForCapacityAsync(formId, transactionCt)
                    ?? throw new KeyNotFoundException("Admission form not found.");
                var result = await coordinator.ReconcileAsync(
                    form, now, command.ChangedByUserId, null, transactionCt);
                expired += result.Expired;
                promoted += result.Promoted;
            }

            return new ProcessAdmissionExpirationsDto(formIds.Count, expired, promoted);
        }, ct);
    }
}
