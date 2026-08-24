using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Receipts;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Receipts;

public sealed class ReceiptHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reserve_creates_global_number_and_immutable_payment_snapshot()
    {
        var fixture = Fixture();
        var payment = Payment();

        var receipt = await fixture.Workflow.ReserveAsync(payment, 99, Now.UtcDateTime, TestContext.Current.CancellationToken);

        Assert.Equal("REC-00000001", receipt.ReceiptNumber);
        Assert.Equal(payment.Id, receipt.PaymentId);
        Assert.Contains("CUOTA", receipt.SnapshotJson);
        Assert.Contains("COMPROBANTE INTERNO NO FISCAL", receipt.SnapshotJson);
        fixture.Repository.Received(1).Add(receipt);
    }

    [Fact]
    public async Task Reserve_is_idempotent_per_payment()
    {
        var fixture = Fixture();
        var payment = Payment();
        var existing = Receipt(payment);
        payment.Receipt = existing;

        var result = await fixture.Workflow.ReserveAsync(payment, 99, Now.UtcDateTime, TestContext.Current.CancellationToken);

        Assert.Same(existing, result);
        await fixture.Repository.DidNotReceive().LockSequenceAsync(Arg.Any<CancellationToken>());
        fixture.Repository.DidNotReceive().Add(Arg.Any<Receipt>());
    }

    [Fact]
    public async Task Generate_persists_pdf_hash_and_reuses_reserved_number()
    {
        var fixture = Fixture();
        var receipt = await fixture.Workflow.ReserveAsync(Payment(), 99, Now.UtcDateTime, TestContext.Current.CancellationToken);

        var result = await fixture.Workflow.EnsureGeneratedAsync(receipt, TestContext.Current.CancellationToken);

        Assert.Equal(ReceiptStatus.Ready, result.Status);
        Assert.Equal("REC-00000001", result.ReceiptNumber);
        Assert.Equal(64, result.Sha256!.Length);
        Assert.Equal($"/api/v1/receipts/{receipt.PublicId}/download", result.DownloadPath);
    }

    [Fact]
    public async Task Generation_failure_keeps_number_and_can_be_retried()
    {
        var fixture = Fixture(failPdf: true);
        var receipt = await fixture.Workflow.ReserveAsync(Payment(), 99, Now.UtcDateTime, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Workflow.EnsureGeneratedAsync(
            receipt, TestContext.Current.CancellationToken));

        Assert.Equal("REC-00000001", receipt.ReceiptNumber);
        Assert.Equal(ReceiptStatus.Failed, receipt.Status);
        Assert.Contains("pdf failure", receipt.LastError);
    }

    [Fact]
    public async Task Own_history_uses_authenticated_user_and_admin_can_filter_student()
    {
        var repository = Substitute.For<IReceiptRepository>();
        repository.GetByUserAsync(7, Arg.Any<CancellationToken>()).Returns([Receipt(Payment())]);
        repository.GetByStudentAsync(30, Arg.Any<CancellationToken>()).Returns([Receipt(Payment())]);
        var handler = new GetReceiptsQueryHandler(repository);

        Assert.Single(await handler.Handle(new(7, false), TestContext.Current.CancellationToken));
        Assert.Single(await handler.Handle(new(99, true, 30), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new(7, false, 30), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Download_checks_owner_and_sha256_integrity()
    {
        var payment = Payment();
        var receipt = Receipt(payment);
        var content = "%PDF-1.4 receipt"u8.ToArray();
        receipt.Status = ReceiptStatus.Ready;
        receipt.StorageKey = "receipts/test.pdf";
        receipt.Sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        var repository = Substitute.For<IReceiptRepository>();
        repository.FindByPublicIdAsync(receipt.PublicId, false, Arg.Any<CancellationToken>()).Returns(receipt);
        var storage = Substitute.For<IFileStorage>();
        storage.ReadAsync(receipt.StorageKey, receipt.ContentType, receipt.FileName, Arg.Any<CancellationToken>())
            .Returns(new StoredFile(content, receipt.ContentType, receipt.FileName));
        var handler = new DownloadReceiptQueryHandler(repository, storage);

        var file = await handler.Handle(new(receipt.PublicId, 7, false), TestContext.Current.CancellationToken);

        Assert.Equal(content, file.Content);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new(receipt.PublicId, 8, false), TestContext.Current.CancellationToken));
    }

    private static TestFixture Fixture(bool failPdf = false)
    {
        var repository = Substitute.For<IReceiptRepository>();
        repository.LockSequenceAsync(Arg.Any<CancellationToken>()).Returns(new ReceiptSequence());
        var pdf = Substitute.For<IReceiptPdfGenerator>();
        if (failPdf)
            pdf.GenerateAsync(Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
                .Returns<Task<byte[]>>(_ => throw new InvalidOperationException("pdf failure"));
        else
            pdf.GenerateAsync(Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
                .Returns("%PDF-1.4 receipt"u8.ToArray());
        var storage = Substitute.For<IFileStorage>();
        storage.SaveAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("receipts/test.pdf");
        var unitOfWork = new ImmediateUnitOfWork();
        return new(repository, new ReceiptWorkflowService(
            repository, unitOfWork, pdf, storage, new FixedTimeProvider(Now)));
    }

    private static Payment Payment()
    {
        var student = new Student
        {
            Id = 30,
            UserId = 7,
            User = new User { Id = 7, Username = "Ada", LastName = "Lovelace", Dni = "12345678" }
        };
        var debt = new StudentDebt
        {
            Id = 10,
            PublicId = Guid.NewGuid(),
            StudentId = 30,
            TotalAmount = 100,
            PaidAmount = 100,
            Status = StudentDebtStatus.Paid,
            Currency = "ARS",
            FinancialConcept = new FinancialConcept { Id = 1, Code = "CUOTA", Name = "Cuota mensual" }
        };
        var payment = new Payment
        {
            Id = 20,
            PublicId = Guid.NewGuid(),
            StudentId = 30,
            Student = student,
            PaymentMethodId = 1,
            PaymentMethod = new PaymentMethod { Id = 1, Code = "CASH", Name = "Efectivo", Kind = PaymentMethodKind.Cash },
            Currency = "ARS",
            Amount = 100,
            Status = PaymentStatus.Confirmed,
            CreatedAt = Now.UtcDateTime,
            ConfirmedAt = Now.UtcDateTime,
            ConfirmedByUserId = 99
        };
        payment.Allocations = [new PaymentAllocation
        {
            PaymentId = payment.Id,
            StudentDebtId = debt.Id,
            StudentDebt = debt,
            Amount = 100
        }];
        return payment;
    }

    private static Receipt Receipt(Payment payment) => new()
    {
        Id = 1,
        PublicId = Guid.NewGuid(),
        PaymentId = payment.Id,
        Payment = payment,
        SequenceNumber = 1,
        ReceiptNumber = "REC-00000001",
        SnapshotJson = System.Text.Json.JsonSerializer.Serialize(new ReceiptSnapshot(
            "Academia Digital", payment.PublicId, payment.StudentId, "Ada Lovelace", "12345678",
            "CASH", "Efectivo", "ARS", payment.Amount, Now.UtcDateTime, 99, "Usuario 99",
            [new ReceiptItemSnapshot(payment.Allocations.Single().StudentDebt.PublicId, "CUOTA", "Cuota mensual", 100)],
            "COMPROBANTE INTERNO NO FISCAL")),
        Status = ReceiptStatus.Generating,
        FileName = "REC-00000001.pdf",
        ContentType = "application/pdf",
        CreatedAt = Now.UtcDateTime,
        IssuedByUserId = 99
    };

    private sealed record TestFixture(IReceiptRepository Repository, ReceiptWorkflowService Workflow);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }
}
