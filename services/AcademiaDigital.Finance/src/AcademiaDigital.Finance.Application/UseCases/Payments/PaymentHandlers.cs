using AcademiaDigital.Finance.Application.UseCases.Receipts;
using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Application.Interfaces;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;
using AcademiaDigital.Finance.Domain.Services;

namespace AcademiaDigital.Finance.Application.UseCases.Payments;

public sealed record PaymentMethodDto(int Id, string Code, string Name, PaymentMethodKind Kind, bool RequiresReconciliation);
public sealed record PaymentAllocationDto(Guid DebtPublicId, decimal Amount, decimal DebtTotal, decimal DebtPaid, decimal DebtOutstanding, StudentDebtStatus DebtStatus);
public sealed record PaymentReconciliationDto(PaymentReconciliationDecision Decision, string? Note, DateTime CreatedAt, long CreatedByUserId);
public sealed record PaymentReversalDto(Guid PublicId, decimal Amount, string Reason, DateTime CreatedAt, long CreatedByUserId);
public sealed record PaymentDto(
    Guid PublicId,
    long StudentId,
    string StudentName,
    string StudentDni,
    PaymentMethodDto Method,
    string Currency,
    decimal Amount,
    PaymentStatus Status,
    string? ExternalReference,
    string? Notes,
    DateTime CreatedAt,
    long CreatedByUserId,
    DateTime? ConfirmationRequestedAt,
    long? ConfirmationRequestedByUserId,
    DateTime? ConfirmedAt,
    long? ConfirmedByUserId,
    IReadOnlyList<PaymentAllocationDto> Allocations,
    IReadOnlyList<PaymentReconciliationDto> Reconciliations,
    IReadOnlyList<PaymentReversalDto> Reversals,
    ReceiptDto? Receipt);

public sealed record CreatePaymentAllocationCommand(Guid DebtPublicId, decimal Amount);

// The caller resolves the student (DNI → studentId + display data) before calling Finance,
// because after extraction Finance no longer owns the Student table. StudentDni is still
// validated/normalised, and StudentName/StudentDni are stored as an immutable snapshot.
public sealed record CreatePaymentCommand(
    long StudentId,
    string StudentName,
    string StudentDni,
    int PaymentMethodId,
    decimal Amount,
    string? ExternalReference,
    string? Notes,
    IReadOnlyList<CreatePaymentAllocationCommand> Allocations,
    long ActorUserId);
public sealed record ConfirmPaymentCommand(Guid PaymentPublicId, string IdempotencyKey, long ActorUserId);
public sealed record ReconcilePaymentCommand(Guid PaymentPublicId, PaymentReconciliationDecision Decision, string? Note, long ActorUserId);
public sealed record ReversePaymentCommand(Guid PaymentPublicId, string Reason, long ActorUserId);

public sealed class GetPaymentMethodsQueryHandler(IPaymentRepository repository)
{
    public async Task<IReadOnlyList<PaymentMethodDto>> Handle(CancellationToken ct = default)
        => (await repository.GetActiveMethodsAsync(ct)).Select(PaymentMappings.Map).ToArray();
}

public sealed class CreatePaymentCommandHandler(
    IPaymentRepository repository,
    PaymentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<PaymentDto> Handle(CreatePaymentCommand command, CancellationToken ct = default)
    {
        var dni = policy.NormalizeDni(command.StudentDni);
        if (command.StudentId <= 0) throw new ArgumentException("A valid studentId is required.");
        var method = await repository.FindActiveMethodAsync(command.PaymentMethodId, ct)
            ?? throw new KeyNotFoundException("Active payment method not found.");
        var allocationInput = command.Allocations.Select(item => (item.DebtPublicId, item.Amount)).ToArray();
        policy.ValidateDraft(command.Amount, method.Kind, command.ExternalReference, command.Notes, allocationInput);
        var debts = await repository.GetDebtsByPublicIdsAsync(allocationInput.Select(item => item.DebtPublicId).ToArray(), ct);
        if (debts.Count != allocationInput.Length) throw new KeyNotFoundException("One or more debts were not found.");
        if (debts.Any(debt => debt.StudentId != command.StudentId))
            throw new InvalidOperationException("Every allocated debt must belong to the identified student.");
        var debtByPublicId = debts.ToDictionary(debt => debt.PublicId);
        foreach (var allocation in allocationInput)
        {
            var debt = debtByPublicId[allocation.DebtPublicId];
            if (debt.Status == StudentDebtStatus.Cancelled)
                throw new InvalidOperationException("Cancelled debts cannot receive payments.");
            if (allocation.Amount > debt.TotalAmount - debt.PaidAmount)
                throw new InvalidOperationException("Payment allocation exceeds the debt outstanding amount.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var payment = new Payment
        {
            PublicId = Guid.NewGuid(), StudentId = command.StudentId,
            StudentName = string.IsNullOrWhiteSpace(command.StudentName) ? $"Alumno {command.StudentId}" : command.StudentName.Trim(),
            StudentDni = dni,
            PaymentMethodId = method.Id, PaymentMethod = method, Currency = "ARS", Amount = command.Amount,
            Status = PaymentStatus.Draft, ExternalReference = command.ExternalReference?.Trim(), Notes = command.Notes?.Trim(),
            CreatedAt = now, CreatedByUserId = command.ActorUserId,
            Allocations = allocationInput.Select(item => new PaymentAllocation
            {
                StudentDebtId = debtByPublicId[item.DebtPublicId].Id,
                StudentDebt = debtByPublicId[item.DebtPublicId],
                Amount = item.Amount
            }).ToArray()
        };
        repository.AddPayment(payment);
        await unitOfWork.SaveChangesAsync(ct);
        return PaymentMappings.Map(payment);
    }
}

public sealed class ConfirmPaymentCommandHandler(
    IPaymentRepository repository,
    PaymentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ReceiptWorkflowService receiptWorkflow)
{
    public async Task<PaymentDto> Handle(ConfirmPaymentCommand command, CancellationToken ct = default)
    {
        var key = policy.ValidateIdempotencyKey(command.IdempotencyKey);
        var prepared = await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var existing = await repository.FindByConfirmationKeyForUpdateAsync(key, transactionCt);
            if (existing is not null)
            {
                if (existing.PublicId != command.PaymentPublicId)
                    throw new InvalidOperationException("Idempotency-Key was already used for another payment.");
                var existingReceipt = existing.Status == PaymentStatus.Confirmed
                    ? await receiptWorkflow.ReserveAsync(
                        existing,
                        existing.ConfirmedByUserId ?? command.ActorUserId,
                        existing.ConfirmedAt ?? timeProvider.GetUtcNow().UtcDateTime,
                        transactionCt)
                    : null;
                if (existingReceipt is not null) await unitOfWork.SaveChangesAsync(transactionCt);
                return (Payment: existing, Receipt: existingReceipt);
            }

            var payment = await repository.FindForUpdateAsync(command.PaymentPublicId, transactionCt)
                ?? throw new KeyNotFoundException("Payment not found.");
            if (payment.Status != PaymentStatus.Draft)
                throw new InvalidOperationException("Only draft payments can be confirmed.");
            var now = timeProvider.GetUtcNow().UtcDateTime;
            payment.ConfirmationIdempotencyKey = key;
            payment.ConfirmationRequestedAt = now;
            payment.ConfirmationRequestedByUserId = command.ActorUserId;

            if (payment.PaymentMethod.Kind == PaymentMethodKind.BankTransfer)
            {
                payment.Status = PaymentStatus.PendingReconciliation;
            }
            else
            {
                var debts = await repository.LockDebtsForPaymentAsync(payment.Id, transactionCt);
                ApplyAllocations(payment, debts, policy);
                payment.Status = PaymentStatus.Confirmed;
                payment.ConfirmedAt = now;
                payment.ConfirmedByUserId = command.ActorUserId;
            }
            var receipt = payment.Status == PaymentStatus.Confirmed
                ? await receiptWorkflow.ReserveAsync(payment, command.ActorUserId, now, transactionCt)
                : null;
            await unitOfWork.SaveChangesAsync(transactionCt);
            return (Payment: payment, Receipt: receipt);
        }, ct);
        if (prepared.Receipt is not null)
            await receiptWorkflow.EnsureGeneratedAsync(prepared.Receipt, ct);
        return PaymentMappings.Map(prepared.Payment);
    }

    internal static void ApplyAllocations(Payment payment, IReadOnlyCollection<StudentDebt> debts, PaymentPolicy policy)
    {
        if (debts.Count != payment.Allocations.Count)
            throw new InvalidOperationException("Payment allocations reference unavailable debts.");
        var debtById = debts.ToDictionary(debt => debt.Id);
        foreach (var allocation in payment.Allocations.OrderBy(item => item.StudentDebtId))
        {
            if (!debtById.TryGetValue(allocation.StudentDebtId, out var debt) || debt.StudentId != payment.StudentId)
                throw new InvalidOperationException("Payment allocations must belong to the payment student.");
            policy.ApplyAllocation(debt, allocation.Amount);
            allocation.StudentDebt = debt;
        }
    }
}

public sealed class ReconcilePaymentCommandHandler(
    IPaymentRepository repository,
    PaymentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ReceiptWorkflowService receiptWorkflow)
{
    public async Task<PaymentDto> Handle(ReconcilePaymentCommand command, CancellationToken ct = default)
    {
        policy.ValidateReconciliation(command.Decision, command.Note);
        var prepared = await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var payment = await repository.FindForUpdateAsync(command.PaymentPublicId, transactionCt)
                ?? throw new KeyNotFoundException("Payment not found.");
            var previous = payment.Reconciliations.OrderByDescending(item => item.Id).FirstOrDefault();
            if (previous is not null && previous.Decision == command.Decision)
            {
                var existingReceipt = payment.Status == PaymentStatus.Confirmed
                    ? await receiptWorkflow.ReserveAsync(
                        payment,
                        payment.ConfirmedByUserId ?? command.ActorUserId,
                        payment.ConfirmedAt ?? timeProvider.GetUtcNow().UtcDateTime,
                        transactionCt)
                    : null;
                if (existingReceipt is not null) await unitOfWork.SaveChangesAsync(transactionCt);
                return (Payment: payment, Receipt: existingReceipt);
            }
            if (payment.PaymentMethod.Kind != PaymentMethodKind.BankTransfer)
                throw new InvalidOperationException("Only bank transfers require reconciliation.");
            if (payment.Status != PaymentStatus.PendingReconciliation)
                throw new InvalidOperationException("Only pending transfers can be reconciled.");

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (command.Decision == PaymentReconciliationDecision.Approve)
            {
                var debts = await repository.LockDebtsForPaymentAsync(payment.Id, transactionCt);
                ConfirmPaymentCommandHandler.ApplyAllocations(payment, debts, policy);
                payment.Status = PaymentStatus.Confirmed;
                payment.ConfirmedAt = now;
                payment.ConfirmedByUserId = command.ActorUserId;
            }
            else
            {
                payment.Status = PaymentStatus.Rejected;
            }
            payment.Reconciliations.Add(new PaymentReconciliation
            {
                Decision = command.Decision,
                Note = command.Note?.Trim(),
                CreatedAt = now,
                CreatedByUserId = command.ActorUserId
            });
            var receipt = payment.Status == PaymentStatus.Confirmed
                ? await receiptWorkflow.ReserveAsync(payment, command.ActorUserId, now, transactionCt)
                : null;
            await unitOfWork.SaveChangesAsync(transactionCt);
            return (Payment: payment, Receipt: receipt);
        }, ct);
        if (prepared.Receipt is not null)
            await receiptWorkflow.EnsureGeneratedAsync(prepared.Receipt, ct);
        return PaymentMappings.Map(prepared.Payment);
    }
}

public sealed class ReversePaymentCommandHandler(
    IPaymentRepository repository,
    PaymentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<PaymentDto> Handle(ReversePaymentCommand command, CancellationToken ct = default)
    {
        var reason = policy.ValidateReversalReason(command.Reason);
        return unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var payment = await repository.FindForUpdateAsync(command.PaymentPublicId, transactionCt)
                ?? throw new KeyNotFoundException("Payment not found.");
            if (payment.Status == PaymentStatus.Reversed && payment.Reversals.Count != 0) return PaymentMappings.Map(payment);
            if (payment.Status != PaymentStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed payments can be reversed.");
            var debts = await repository.LockDebtsForPaymentAsync(payment.Id, transactionCt);
            if (debts.Count != payment.Allocations.Count)
                throw new InvalidOperationException("Payment allocations reference unavailable debts.");
            var debtById = debts.ToDictionary(debt => debt.Id);
            foreach (var allocation in payment.Allocations.OrderBy(item => item.StudentDebtId))
            {
                if (!debtById.TryGetValue(allocation.StudentDebtId, out var debt))
                    throw new InvalidOperationException("Payment allocation debt was not found.");
                policy.ReverseAllocation(debt, allocation.Amount);
                allocation.StudentDebt = debt;
            }
            payment.Status = PaymentStatus.Reversed;
            payment.Reversals.Add(new PaymentReversal
            {
                PublicId = Guid.NewGuid(), Amount = payment.Amount, Reason = reason,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime, CreatedByUserId = command.ActorUserId
            });
            await unitOfWork.SaveChangesAsync(transactionCt);
            return PaymentMappings.Map(payment);
        }, ct);
    }
}

public sealed class GetPaymentsQueryHandler(IPaymentRepository repository)
{
    public async Task<IReadOnlyList<PaymentDto>> Handle(long studentId, CancellationToken ct = default)
        => (await repository.GetByStudentAsync(studentId, ct)).Select(PaymentMappings.Map).ToArray();
}

internal static class PaymentMappings
{
    public static PaymentMethodDto Map(PaymentMethod method)
        => new(method.Id, method.Code, method.Name, method.Kind, method.Kind == PaymentMethodKind.BankTransfer);

    public static PaymentDto Map(Payment payment)
        => new(
            payment.PublicId,
            payment.StudentId,
            payment.StudentName,
            payment.StudentDni,
            Map(payment.PaymentMethod),
            payment.Currency,
            payment.Amount,
            payment.Status,
            payment.ExternalReference,
            payment.Notes,
            payment.CreatedAt,
            payment.CreatedByUserId,
            payment.ConfirmationRequestedAt,
            payment.ConfirmationRequestedByUserId,
            payment.ConfirmedAt,
            payment.ConfirmedByUserId,
            payment.Allocations.OrderBy(item => item.StudentDebtId).Select(item => new PaymentAllocationDto(
                item.StudentDebt.PublicId,
                item.Amount,
                item.StudentDebt.TotalAmount,
                item.StudentDebt.PaidAmount,
                item.StudentDebt.TotalAmount - item.StudentDebt.PaidAmount,
                item.StudentDebt.Status)).ToArray(),
            payment.Reconciliations.OrderBy(item => item.CreatedAt).Select(item => new PaymentReconciliationDto(
                item.Decision, item.Note, item.CreatedAt, item.CreatedByUserId)).ToArray(),
            payment.Reversals.OrderBy(item => item.CreatedAt).Select(item => new PaymentReversalDto(
                item.PublicId, item.Amount, item.Reason, item.CreatedAt, item.CreatedByUserId)).ToArray(),
            payment.Receipt is null ? null : ReceiptMappings.Map(payment.Receipt));
}
