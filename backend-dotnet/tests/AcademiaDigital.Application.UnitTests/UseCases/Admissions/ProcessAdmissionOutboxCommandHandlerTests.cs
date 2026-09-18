using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Admissions;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Admissions;

public sealed class ProcessAdmissionOutboxCommandHandlerTests
{
    [Fact]
    public async Task Handle_generates_stores_notifies_and_marks_the_message_processed()
    {
        var publicId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 22, 18, 0, 0, TimeSpan.Zero);
        var snapshot = new AdmissionAgreementSnapshot(
            publicId, "Ingreso", "Backend", "ada@example.com", "12345678", now.UtcDateTime,
            "Acepto los terminos", new Dictionary<string, string> { ["name"] = "Ada" });
        var agreement = new AdmissionAgreement
        {
            AgreementNumber = $"ADM-{publicId:N}".ToUpperInvariant(),
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            Status = AdmissionAgreementStatus.Pending,
            FileName = "agreement.pdf",
            ContentType = "application/pdf"
        };
        var message = new OutboxMessage
        {
            Type = "AdmissionAgreementConfirmed",
            DeduplicationKey = $"agreement:{publicId:N}",
            PayloadJson = JsonSerializer.Serialize(new { ApplicationPublicId = publicId }),
            Status = OutboxMessageStatus.Pending,
            AvailableAt = now.UtcDateTime,
            OccurredAt = now.UtcDateTime
        };
        var repository = Substitute.For<IAdmissionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var pdf = Substitute.For<IAdmissionAgreementPdfGenerator>();
        var storage = Substitute.For<IFileStorage>();
        var notifications = Substitute.For<IAdmissionNotificationSender>();
        repository.GetClaimableOutboxMessagesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), 20, Arg.Any<CancellationToken>())
            .Returns([message]);
        repository.FindAgreementByApplicationPublicIdAsync(publicId, true, Arg.Any<CancellationToken>())
            .Returns(agreement);
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<IReadOnlyList<OutboxMessage>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<IReadOnlyList<OutboxMessage>>>>()(call.ArgAt<CancellationToken>(1)));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        pdf.GenerateAsync(Arg.Any<AdmissionAgreementPdfModel>(), Arg.Any<CancellationToken>())
            .Returns("%PDF-1.4"u8.ToArray());
        storage.SaveAsync(
                Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), "application/pdf", "agreement.pdf",
                Arg.Any<CancellationToken>())
            .Returns("agreements/agreement.pdf");

        var handler = new ProcessAdmissionOutboxCommandHandler(
            repository, unitOfWork, pdf, storage, notifications, new FixedTimeProvider(now));
        var result = await handler.Handle(new ProcessAdmissionOutboxCommand(20), TestContext.Current.CancellationToken);

        Assert.Equal(new ProcessAdmissionOutboxResult(1, 1, 0), result);
        Assert.Equal(AdmissionAgreementStatus.Ready, agreement.Status);
        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
        Assert.Equal(1, message.Attempts);
        await notifications.Received(1).SendAgreementReadyAsync(
            Arg.Is<AdmissionAgreementNotification>(notification =>
                notification.RecipientEmail == "ada@example.com"
                && notification.DownloadPath.Contains(publicId.ToString())),
            Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
