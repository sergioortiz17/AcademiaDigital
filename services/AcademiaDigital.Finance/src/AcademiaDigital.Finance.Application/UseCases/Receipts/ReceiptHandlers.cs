using System.Security.Cryptography;
using System.Text.Json;
using AcademiaDigital.Finance.Application.Interfaces;
using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Domain.Exceptions;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Finance.Application.UseCases.Receipts;

public sealed record ReceiptItemSnapshot(
    Guid DebtPublicId,
    string ConceptCode,
    string ConceptName,
    decimal Amount);

public sealed record ReceiptSnapshot(
    string InstitutionName,
    Guid PaymentPublicId,
    long StudentId,
    string StudentName,
    string Dni,
    string PaymentMethodCode,
    string PaymentMethodName,
    string Currency,
    decimal Amount,
    DateTime IssuedAt,
    long OperatorUserId,
    string OperatorName,
    IReadOnlyList<ReceiptItemSnapshot> Items,
    string NonFiscalLegend);

public sealed record ReceiptItemDto(Guid DebtPublicId, string ConceptCode, string ConceptName, decimal Amount);

public sealed record ReceiptDto(
    Guid PublicId,
    string ReceiptNumber,
    Guid PaymentPublicId,
    PaymentStatus PaymentStatus,
    long StudentId,
    string StudentName,
    string Dni,
    string PaymentMethodCode,
    string PaymentMethodName,
    string Currency,
    decimal Amount,
    DateTime IssuedAt,
    long OperatorUserId,
    string OperatorName,
    IReadOnlyList<ReceiptItemDto> Items,
    ReceiptStatus Status,
    string FileName,
    string? Sha256,
    DateTime? GeneratedAt,
    string? LastError,
    string? FiscalCae,
    string? FiscalQrData,
    string? DownloadPath);

public sealed class ReceiptWorkflowService(
    IReceiptRepository repository,
    IUnitOfWork unitOfWork,
    IReceiptPdfGenerator pdfGenerator,
    IFileStorage fileStorage,
    TimeProvider timeProvider)
{
    public async Task<Receipt> ReserveAsync(
        Payment payment,
        long actorUserId,
        DateTime issuedAt,
        CancellationToken ct = default)
    {
        if (payment.Status != PaymentStatus.Confirmed)
            throw new InvalidOperationException("A receipt can only be reserved for a confirmed payment.");

        var existing = payment.Receipt ?? await repository.FindByPaymentAsync(payment.Id, true, ct);
        if (existing is not null)
        {
            payment.Receipt = existing;
            return existing;
        }

        var sequence = await repository.LockSequenceAsync(ct);
        var sequenceNumber = sequence.TakeNext();
        var receiptNumber = $"REC-{sequenceNumber:00000000}";
        var snapshot = BuildSnapshot(payment, actorUserId, issuedAt);
        var receipt = new Receipt
        {
            PublicId = Guid.NewGuid(),
            PaymentId = payment.Id,
            Payment = payment,
            SequenceNumber = sequenceNumber,
            ReceiptNumber = receiptNumber,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            Status = ReceiptStatus.Generating,
            FileName = $"{receiptNumber}.pdf",
            ContentType = "application/pdf",
            CreatedAt = issuedAt,
            IssuedByUserId = actorUserId
        };
        repository.Add(receipt);
        payment.Receipt = receipt;
        return receipt;
    }

    public async Task<ReceiptDto> EnsureGeneratedAsync(Receipt receipt, CancellationToken ct = default)
    {
        if (receipt.Status == ReceiptStatus.Ready) return ReceiptMappings.Map(receipt);
        try
        {
            var snapshot = ReceiptMappings.ReadSnapshot(receipt);
            var pdf = await pdfGenerator.GenerateAsync(new ReceiptPdfModel(
                snapshot.InstitutionName,
                receipt.ReceiptNumber,
                snapshot.IssuedAt,
                snapshot.StudentName,
                snapshot.Dni,
                snapshot.PaymentMethodName,
                snapshot.Currency,
                snapshot.Amount,
                snapshot.OperatorUserId,
                snapshot.OperatorName,
                snapshot.Items.Select(item => new ReceiptPdfItem(
                    item.ConceptCode, item.ConceptName, item.DebtPublicId, item.Amount)).ToArray(),
                snapshot.NonFiscalLegend), ct);
            var storageKey = await fileStorage.SaveAsync(
                $"receipts/{receipt.CreatedAt:yyyy}/{receipt.PublicId:N}/{receipt.FileName}",
                pdf, receipt.ContentType, receipt.FileName, ct);
            receipt.MarkReady(storageKey, Convert.ToHexString(SHA256.HashData(pdf)), timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(ct);
            return ReceiptMappings.Map(receipt);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            receipt.MarkFailed(exception.Message);
            await unitOfWork.SaveChangesAsync(ct);
            throw new InvalidOperationException(
                "Receipt generation failed and can be retried with the same number.", exception);
        }
    }

    public async Task<ReceiptDto> RetryAsync(Guid publicId, CancellationToken ct = default)
    {
        var receipt = await repository.FindByPublicIdAsync(publicId, true, ct)
            ?? throw new KeyNotFoundException("Receipt not found.");
        return await EnsureGeneratedAsync(receipt, ct);
    }

    private static ReceiptSnapshot BuildSnapshot(Payment payment, long actorUserId, DateTime issuedAt)
    {
        var items = payment.Allocations.OrderBy(item => item.StudentDebtId).Select(item =>
        {
            var concept = item.StudentDebt.FinancialConcept;
            return new ReceiptItemSnapshot(
                item.StudentDebt.PublicId,
                string.IsNullOrWhiteSpace(concept?.Code) ? $"DEBT-{item.StudentDebtId}" : concept.Code,
                string.IsNullOrWhiteSpace(concept?.Name) ? "Concepto financiero" : concept.Name,
                item.Amount);
        }).ToArray();
        if (items.Length == 0 || items.Sum(item => item.Amount) != payment.Amount)
            throw new InvalidOperationException("Receipt items must reproduce the complete payment amount.");
        return new ReceiptSnapshot(
            "Academia Digital",
            payment.PublicId,
            payment.StudentId,
            payment.StudentName,
            payment.StudentDni,
            payment.PaymentMethod.Code,
            payment.PaymentMethod.Name,
            payment.Currency,
            payment.Amount,
            issuedAt,
            actorUserId,
            $"Usuario {actorUserId}",
            items,
            "COMPROBANTE INTERNO NO FISCAL");
    }
}

// Ownership after extraction is expressed in terms of studentId: Finance no longer knows
// the user↔student mapping (Student lives in the monolito). The API layer authorises the
// caller and passes the effective studentId when the request is scoped to one student.
public sealed record GetReceiptsQuery(bool IsAdmin, long? StudentId = null);

public sealed class GetReceiptsQueryHandler(IReceiptRepository repository)
{
    public async Task<IReadOnlyList<ReceiptDto>> Handle(GetReceiptsQuery query, CancellationToken ct = default)
    {
        if (!query.StudentId.HasValue)
            throw new ArgumentException("A studentId is required to list receipts.");
        var receipts = await repository.GetByStudentAsync(query.StudentId.Value, ct);
        return receipts.Select(ReceiptMappings.Map).ToArray();
    }
}

public sealed record GetReceiptQuery(Guid PublicId, bool IsAdmin, long? StudentId);

public sealed class GetReceiptQueryHandler(IReceiptRepository repository)
{
    public async Task<ReceiptDto> Handle(GetReceiptQuery query, CancellationToken ct = default)
    {
        var receipt = await repository.FindByPublicIdAsync(query.PublicId, false, ct)
            ?? throw new KeyNotFoundException("Receipt not found.");
        EnsureOwnership(receipt, query.StudentId, query.IsAdmin);
        return ReceiptMappings.Map(receipt);
    }

    internal static void EnsureOwnership(Receipt receipt, long? studentId, bool isAdmin)
    {
        if (!isAdmin && receipt.Payment.StudentId != studentId)
            throw new ForbiddenException("The receipt belongs to another student.");
    }
}

public sealed record DownloadReceiptQuery(Guid PublicId, bool IsAdmin, long? StudentId);

public sealed class DownloadReceiptQueryHandler(IReceiptRepository repository, IFileStorage fileStorage)
{
    public async Task<StoredFile> Handle(DownloadReceiptQuery query, CancellationToken ct = default)
    {
        var receipt = await repository.FindByPublicIdAsync(query.PublicId, false, ct)
            ?? throw new KeyNotFoundException("Receipt not found.");
        GetReceiptQueryHandler.EnsureOwnership(receipt, query.StudentId, query.IsAdmin);
        if (receipt.Status != ReceiptStatus.Ready || string.IsNullOrWhiteSpace(receipt.StorageKey))
            throw new InvalidOperationException("Receipt is not ready for download.");
        var stored = await fileStorage.ReadAsync(receipt.StorageKey, receipt.ContentType, receipt.FileName, ct)
            ?? throw new KeyNotFoundException("Receipt file not found.");
        var actualHash = Convert.ToHexString(SHA256.HashData(stored.Content));
        if (!string.Equals(actualHash, receipt.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Receipt file integrity validation failed.");
        return stored;
    }
}

internal static class ReceiptMappings
{
    public static ReceiptDto Map(Receipt receipt)
    {
        var snapshot = ReadSnapshot(receipt);
        return new ReceiptDto(
            receipt.PublicId,
            receipt.ReceiptNumber,
            snapshot.PaymentPublicId,
            receipt.Payment.Status,
            snapshot.StudentId,
            snapshot.StudentName,
            snapshot.Dni,
            snapshot.PaymentMethodCode,
            snapshot.PaymentMethodName,
            snapshot.Currency,
            snapshot.Amount,
            snapshot.IssuedAt,
            snapshot.OperatorUserId,
            snapshot.OperatorName,
            snapshot.Items.Select(item => new ReceiptItemDto(
                item.DebtPublicId, item.ConceptCode, item.ConceptName, item.Amount)).ToArray(),
            receipt.Status,
            receipt.FileName,
            receipt.Sha256,
            receipt.GeneratedAt,
            receipt.LastError,
            receipt.FiscalCae,
            receipt.FiscalQrData,
            receipt.Status == ReceiptStatus.Ready ? $"/api/v1/receipts/{receipt.PublicId}/download" : null);
    }

    public static ReceiptSnapshot ReadSnapshot(Receipt receipt)
        => JsonSerializer.Deserialize<ReceiptSnapshot>(receipt.SnapshotJson)
            ?? throw new InvalidOperationException("Receipt snapshot is invalid.");
}
