namespace AcademiaDigital.Application.Interfaces;

public sealed record AdmissionAgreementNotification(
    string RecipientEmail,
    string AgreementNumber,
    string DownloadPath,
    string IdempotencyKey);

public interface IAdmissionNotificationSender
{
    Task SendAgreementReadyAsync(
        AdmissionAgreementNotification notification,
        CancellationToken ct = default);
}
