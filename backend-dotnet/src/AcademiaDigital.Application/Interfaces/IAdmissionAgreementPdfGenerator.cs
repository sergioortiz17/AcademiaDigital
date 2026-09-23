namespace AcademiaDigital.Application.Interfaces;

public sealed record AdmissionAgreementPdfModel(
    string AgreementNumber,
    string FormTitle,
    string CareerName,
    string ApplicantEmail,
    string ApplicantDni,
    DateTime ConfirmedAt,
    string TermsText,
    IReadOnlyDictionary<string, string> SubmittedFields);

public interface IAdmissionAgreementPdfGenerator
{
    Task<byte[]> GenerateAsync(AdmissionAgreementPdfModel model, CancellationToken ct = default);
}
