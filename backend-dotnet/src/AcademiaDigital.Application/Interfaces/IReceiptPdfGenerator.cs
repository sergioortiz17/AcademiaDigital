namespace AcademiaDigital.Application.Interfaces;

public sealed record ReceiptPdfItem(
    string ConceptCode,
    string ConceptName,
    Guid DebtPublicId,
    decimal Amount);

public sealed record ReceiptPdfModel(
    string InstitutionName,
    string ReceiptNumber,
    DateTime IssuedAt,
    string StudentName,
    string Dni,
    string PaymentMethod,
    string Currency,
    decimal Amount,
    long OperatorUserId,
    string OperatorName,
    IReadOnlyList<ReceiptPdfItem> Items,
    string NonFiscalLegend);

public interface IReceiptPdfGenerator
{
    Task<byte[]> GenerateAsync(ReceiptPdfModel model, CancellationToken ct = default);
}
