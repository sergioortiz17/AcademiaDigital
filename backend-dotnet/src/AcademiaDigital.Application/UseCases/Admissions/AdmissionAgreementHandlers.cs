using System.Security.Cryptography;
using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admissions;

public sealed record AdmissionAgreementSnapshot(
    Guid ApplicationPublicId,
    string FormTitle,
    string CareerName,
    string ApplicantEmail,
    string ApplicantDni,
    DateTime ConfirmedAt,
    string TermsText,
    IReadOnlyDictionary<string, string> SubmittedFields);

public sealed record AdmissionAgreementDto(
    string AgreementNumber,
    string Status,
    string FileName,
    string ContentType,
    string? Sha256,
    DateTime CreatedAt,
    DateTime? GeneratedAt,
    string? DownloadPath,
    string? LastError);

public sealed record GetAdmissionAgreementQuery(Guid PublicId);

public sealed class GetAdmissionAgreementQueryHandler(IAdmissionRepository repository)
{
    public async Task<AdmissionAgreementDto> Handle(GetAdmissionAgreementQuery query, CancellationToken ct = default)
    {
        var agreement = await repository.FindAgreementByApplicationPublicIdAsync(query.PublicId, false, ct)
            ?? throw new KeyNotFoundException("Convenio de admisión no encontrado.");
        return Map(query.PublicId, agreement);
    }

    internal static AdmissionAgreementDto Map(Guid publicId, AdmissionAgreement agreement)
        => new(
            agreement.AgreementNumber,
            agreement.Status.ToString(),
            agreement.FileName,
            agreement.ContentType,
            agreement.Sha256,
            agreement.CreatedAt,
            agreement.GeneratedAt,
            agreement.Status == AdmissionAgreementStatus.Ready
                ? $"/api/v1/admissions/applications/{publicId}/agreement/download"
                : null,
            agreement.LastError);
}

public sealed record DownloadAdmissionAgreementQuery(Guid PublicId);

public sealed class DownloadAdmissionAgreementQueryHandler(
    IAdmissionRepository repository,
    IFileStorage fileStorage)
{
    public async Task<StoredFile> Handle(DownloadAdmissionAgreementQuery query, CancellationToken ct = default)
    {
        var agreement = await repository.FindAgreementByApplicationPublicIdAsync(query.PublicId, false, ct)
            ?? throw new KeyNotFoundException("Convenio de admisión no encontrado.");
        if (agreement.Status != AdmissionAgreementStatus.Ready || string.IsNullOrWhiteSpace(agreement.StorageKey))
            throw new InvalidOperationException("El convenio de admisión no está listo para descargar.");
        return await fileStorage.ReadAsync(
                agreement.StorageKey, agreement.ContentType, agreement.FileName, ct)
            ?? throw new KeyNotFoundException("Archivo del convenio de admisión no encontrado.");
    }
}

public sealed record ProcessAdmissionOutboxCommand(int Limit);
public sealed record ProcessAdmissionOutboxResult(int Claimed, int Processed, int Failed);

public sealed class ProcessAdmissionOutboxCommandHandler(
    IAdmissionRepository repository,
    IUnitOfWork unitOfWork,
    IAdmissionAgreementPdfGenerator pdfGenerator,
    IFileStorage fileStorage,
    IAdmissionNotificationSender notificationSender,
    TimeProvider timeProvider)
{
    public async Task<ProcessAdmissionOutboxResult> Handle(
        ProcessAdmissionOutboxCommand command,
        CancellationToken ct = default)
    {
        if (command.Limit is < 1 or > 100)
            throw new ArgumentException("El límite de procesamiento del outbox debe estar entre 1 y 100.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var messages = await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var claimable = await repository.GetClaimableOutboxMessagesAsync(
                now, now.AddMinutes(-5), command.Limit, transactionCt);
            foreach (var message in claimable) message.MarkProcessing(now);
            await unitOfWork.SaveChangesAsync(transactionCt);
            return claimable;
        }, ct);

        var processed = 0;
        var failed = 0;
        foreach (var message in messages)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<AdmissionAgreementOutboxPayload>(message.PayloadJson)
                    ?? throw new InvalidOperationException("El contenido del outbox de admisión es inválido.");
                var agreement = await repository.FindAgreementByApplicationPublicIdAsync(
                    payload.ApplicationPublicId, true, ct)
                    ?? throw new KeyNotFoundException("Convenio de admisión no encontrado para el mensaje del outbox.");
                var snapshot = JsonSerializer.Deserialize<AdmissionAgreementSnapshot>(agreement.SnapshotJson)
                    ?? throw new InvalidOperationException("La instantánea del convenio de admisión es inválida.");

                if (agreement.Status != AdmissionAgreementStatus.Ready)
                {
                    var pdf = await pdfGenerator.GenerateAsync(new AdmissionAgreementPdfModel(
                        agreement.AgreementNumber,
                        snapshot.FormTitle,
                        snapshot.CareerName,
                        snapshot.ApplicantEmail,
                        snapshot.ApplicantDni,
                        snapshot.ConfirmedAt,
                        snapshot.TermsText,
                        snapshot.SubmittedFields), ct);
                    var storageKey = await fileStorage.SaveAsync(
                        $"agreements/{snapshot.ApplicationPublicId:N}/{agreement.FileName}",
                        pdf,
                        agreement.ContentType,
                        agreement.FileName,
                        ct);
                    agreement.MarkReady(storageKey, Convert.ToHexString(SHA256.HashData(pdf)), now);
                }

                await notificationSender.SendAgreementReadyAsync(new AdmissionAgreementNotification(
                    snapshot.ApplicantEmail,
                    agreement.AgreementNumber,
                    $"/api/v1/admissions/applications/{snapshot.ApplicationPublicId}/agreement/download",
                    message.DeduplicationKey), ct);
                message.MarkProcessed(now);
                await unitOfWork.SaveChangesAsync(ct);
                processed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.MarkFailed(exception.Message, now.AddMinutes(1));
                await unitOfWork.SaveChangesAsync(ct);
                failed++;
            }
        }

        return new ProcessAdmissionOutboxResult(messages.Count, processed, failed);
    }
}

internal sealed record AdmissionAgreementOutboxPayload(Guid ApplicationPublicId);

internal static class AdmissionAgreementFactory
{
    public static (AdmissionAgreement Agreement, OutboxMessage Message) Create(
        AdmissionApplication application,
        DateTime confirmedAt)
    {
        var snapshot = new AdmissionAgreementSnapshot(
            application.PublicId,
            application.AdmissionForm.Title,
            application.AdmissionForm.Career.Name,
            application.ApplicantEmail,
            application.ApplicantDni,
            confirmedAt,
            application.AdmissionForm.TermsText,
            JsonSerializer.Deserialize<Dictionary<string, string>>(application.SubmittedFieldsJson) ?? []);
        var agreementNumber = $"ADM-{application.PublicId:N}".ToUpperInvariant();
        var agreement = new AdmissionAgreement
        {
            AdmissionApplicationId = application.Id,
            AgreementNumber = agreementNumber,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            Status = AdmissionAgreementStatus.Pending,
            FileName = $"{agreementNumber}.pdf",
            ContentType = "application/pdf",
            CreatedAt = confirmedAt
        };
        var message = new OutboxMessage
        {
            Type = "AdmissionAgreementConfirmed",
            AggregateId = application.PublicId.ToString(),
            DeduplicationKey = $"admission-agreement-confirmed:{application.PublicId:N}",
            PayloadJson = JsonSerializer.Serialize(new AdmissionAgreementOutboxPayload(application.PublicId)),
            Status = OutboxMessageStatus.Pending,
            OccurredAt = confirmedAt,
            AvailableAt = confirmedAt
        };
        return (agreement, message);
    }
}
