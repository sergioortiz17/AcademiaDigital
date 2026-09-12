using System.Text;
using System.Text.Json;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class LocalAdmissionNotificationSender(IFileStorage fileStorage, TimeProvider timeProvider) : IAdmissionNotificationSender
{
    public async Task SendAgreementReadyAsync(
        AdmissionAgreementNotification notification,
        CancellationToken ct = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "AdmissionAgreementReady",
            recipient = notification.RecipientEmail,
            agreementNumber = notification.AgreementNumber,
            downloadPath = notification.DownloadPath,
            createdAt = timeProvider.GetUtcNow()
        }, new JsonSerializerOptions { WriteIndented = true });
        var safeKey = Convert.ToHexString(Encoding.UTF8.GetBytes(notification.IdempotencyKey));
        await fileStorage.SaveAsync(
            $"notifications/{safeKey}.json",
            payload,
            "application/json",
            $"{safeKey}.json",
            ct);
    }
}
