using AcademiaDigital.Domain.Entities;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Entities;

public sealed class AdmissionAgreementTests
{
    [Fact]
    public void MarkReady_sets_file_integrity_and_clears_error()
    {
        var agreement = new AdmissionAgreement { Status = AdmissionAgreementStatus.Failed, LastError = "previous" };
        var generatedAt = new DateTime(2026, 8, 22, 18, 0, 0, DateTimeKind.Utc);

        agreement.MarkReady("agreements/a.pdf", new string('A', 64), generatedAt);

        Assert.Equal(AdmissionAgreementStatus.Ready, agreement.Status);
        Assert.Equal("agreements/a.pdf", agreement.StorageKey);
        Assert.Equal(new string('A', 64), agreement.Sha256);
        Assert.Equal(generatedAt, agreement.GeneratedAt);
        Assert.Null(agreement.LastError);
    }

    [Fact]
    public void MarkReady_requires_storage_and_hash()
        => Assert.Throws<ArgumentException>(() => new AdmissionAgreement().MarkReady("", "", DateTime.UtcNow));

    [Fact]
    public void Outbox_lifecycle_tracks_attempt_and_completion()
    {
        var message = new OutboxMessage();
        var now = new DateTime(2026, 8, 22, 18, 0, 0, DateTimeKind.Utc);

        message.MarkProcessing(now);
        message.MarkProcessed(now.AddSeconds(1));

        Assert.Equal(1, message.Attempts);
        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
        Assert.Equal(now.AddSeconds(1), message.ProcessedAt);
    }

    [Fact]
    public void Outbox_failure_is_retryable_at_the_supplied_time()
    {
        var message = new OutboxMessage();
        var retryAt = new DateTime(2026, 8, 22, 18, 1, 0, DateTimeKind.Utc);

        message.MarkFailed("temporary", retryAt);

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.Equal(retryAt, message.AvailableAt);
        Assert.Equal("temporary", message.LastError);
    }
}
