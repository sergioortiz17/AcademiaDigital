using AcademiaDigital.Domain.Entities;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Entities;

public sealed class ReceiptTests
{
    [Fact]
    public void Sequence_is_monotonic_and_checked()
    {
        var sequence = new ReceiptSequence { LastValue = 41 };

        Assert.Equal(42, sequence.TakeNext());
        Assert.Equal(42, sequence.LastValue);
    }

    [Fact]
    public void Sequence_rejects_numeric_overflow()
    {
        var sequence = new ReceiptSequence { LastValue = long.MaxValue };

        Assert.Throws<OverflowException>(() => sequence.TakeNext());
    }

    [Fact]
    public void Ready_receipt_requires_valid_sha256_and_clears_failure()
    {
        var receipt = new Receipt { Status = ReceiptStatus.Failed, LastError = "temporary" };
        var now = new DateTime(2026, 8, 24, 20, 0, 0, DateTimeKind.Utc);

        receipt.MarkReady("receipts/receipt.pdf", new string('a', 64), now);

        Assert.Equal(ReceiptStatus.Ready, receipt.Status);
        Assert.Equal(new string('A', 64), receipt.Sha256);
        Assert.Equal(now, receipt.GeneratedAt);
        Assert.Null(receipt.LastError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("xyz")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Ready_receipt_rejects_invalid_sha256(string sha256)
    {
        var receipt = new Receipt();

        Assert.Throws<ArgumentException>(() => receipt.MarkReady("receipt.pdf", sha256, DateTime.UtcNow));
    }

    [Fact]
    public void Failed_receipt_is_retriable_and_truncates_diagnostic()
    {
        var receipt = new Receipt
        {
            Status = ReceiptStatus.Ready,
            StorageKey = "old.pdf",
            Sha256 = new string('A', 64),
            GeneratedAt = DateTime.UtcNow
        };

        receipt.MarkFailed(new string('x', 1200));

        Assert.Equal(ReceiptStatus.Failed, receipt.Status);
        Assert.Equal(1000, receipt.LastError!.Length);
        Assert.Null(receipt.StorageKey);
        Assert.Null(receipt.Sha256);
        Assert.Null(receipt.GeneratedAt);
    }
}
