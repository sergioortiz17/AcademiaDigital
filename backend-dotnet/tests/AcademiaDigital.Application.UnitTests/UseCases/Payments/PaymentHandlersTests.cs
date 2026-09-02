using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Payments;
using AcademiaDigital.Application.UseCases.Receipts;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Payments;

public sealed class PaymentHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_validates_dni_and_builds_immutable_draft_allocations()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var student = Student();
        var debt = Debt(10, 100m);
        repository.FindActiveMethodAsync(1, Arg.Any<CancellationToken>()).Returns(Method(PaymentMethodKind.Cash));
        repository.FindStudentByDniAsync("12345678", Arg.Any<CancellationToken>()).Returns(student);
        repository.GetDebtsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([debt]);
        var handler = Handler<CreatePaymentCommandHandler>(repository);

        var result = await handler.Handle(new(
            "12.345.678", 1, 40m, null, "Caja",
            [new CreatePaymentAllocationCommand(debt.PublicId, 40m)], 99), TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatus.Draft, result.Status);
        Assert.Equal(40m, Assert.Single(result.Allocations).Amount);
        repository.Received(1).AddPayment(Arg.Is<Payment>(payment =>
            payment.StudentId == student.Id && payment.CreatedByUserId == 99 && payment.Allocations.Count == 1));
    }

    [Fact]
    public async Task Confirm_cash_payment_applies_multiple_partial_allocations_atomically()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var payment = Payment(PaymentMethodKind.Cash, PaymentStatus.Draft, [(10, 40m, 100m), (11, 20m, 80m)]);
        repository.FindForUpdateAsync(payment.PublicId, Arg.Any<CancellationToken>()).Returns(payment);
        repository.LockDebtsForPaymentAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(payment.Allocations.Select(item => item.StudentDebt).ToArray());
        var handler = Handler<ConfirmPaymentCommandHandler>(repository);

        var result = await handler.Handle(new(payment.PublicId, "payment-key-cash-001", 99), TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatus.Confirmed, result.Status);
        Assert.Equal(60m, result.Amount);
        Assert.All(result.Allocations, item => Assert.Equal(StudentDebtStatus.PartiallyPaid, item.DebtStatus));
        Assert.Equal("payment-key-cash-001", payment.ConfirmationIdempotencyKey);
        Assert.Equal(99, payment.ConfirmedByUserId);
        Assert.NotNull(result.Receipt);
        Assert.Equal("REC-00000001", result.Receipt.ReceiptNumber);
        Assert.Equal(ReceiptStatus.Ready, result.Receipt.Status);
    }

    [Fact]
    public async Task Confirm_with_same_key_returns_existing_payment()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var payment = Payment(PaymentMethodKind.Cash, PaymentStatus.Confirmed, [(10, 100m, 100m)]);
        payment.ConfirmationIdempotencyKey = "payment-key-repeat";
        repository.FindByConfirmationKeyForUpdateAsync(payment.ConfirmationIdempotencyKey, Arg.Any<CancellationToken>()).Returns(payment);
        var handler = Handler<ConfirmPaymentCommandHandler>(repository);

        var result = await handler.Handle(new(payment.PublicId, payment.ConfirmationIdempotencyKey, 99), TestContext.Current.CancellationToken);

        Assert.Equal(payment.PublicId, result.PublicId);
        await repository.DidNotReceive().FindForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirmation_key_cannot_be_reused_for_another_payment()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var payment = Payment(PaymentMethodKind.Cash, PaymentStatus.Confirmed, [(10, 100m, 100m)]);
        repository.FindByConfirmationKeyForUpdateAsync("payment-key-conflict", Arg.Any<CancellationToken>()).Returns(payment);
        var handler = Handler<ConfirmPaymentCommandHandler>(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new(Guid.NewGuid(), "payment-key-conflict", 99), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Transfer_confirmation_waits_for_manual_reconciliation()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var payment = Payment(PaymentMethodKind.BankTransfer, PaymentStatus.Draft, [(10, 50m, 100m)]);
        repository.FindForUpdateAsync(payment.PublicId, Arg.Any<CancellationToken>()).Returns(payment);
        var handler = Handler<ConfirmPaymentCommandHandler>(repository);

        var result = await handler.Handle(new(payment.PublicId, "payment-key-transfer", 99), TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatus.PendingReconciliation, result.Status);
        Assert.Equal(0m, payment.Allocations.Single().StudentDebt.PaidAmount);
        await repository.DidNotReceive().LockDebtsForPaymentAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approved_transfer_applies_allocations_and_audits_operator()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var payment = Payment(PaymentMethodKind.BankTransfer, PaymentStatus.PendingReconciliation, [(10, 100m, 100m)]);
        repository.FindForUpdateAsync(payment.PublicId, Arg.Any<CancellationToken>()).Returns(payment);
        repository.LockDebtsForPaymentAsync(payment.Id, Arg.Any<CancellationToken>()).Returns([payment.Allocations.Single().StudentDebt]);
        var handler = Handler<ReconcilePaymentCommandHandler>(repository);

        var result = await handler.Handle(new(
            payment.PublicId, PaymentReconciliationDecision.Approve, "Acreditado", 55), TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatus.Confirmed, result.Status);
        Assert.Equal(StudentDebtStatus.Paid, result.Allocations.Single().DebtStatus);
        var reconciliation = Assert.Single(result.Reconciliations);
        Assert.Equal(55, reconciliation.CreatedByUserId);
    }

    [Fact]
    public async Task Reconciliation_revalidates_and_rejects_overpayment()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var payment = Payment(PaymentMethodKind.BankTransfer, PaymentStatus.PendingReconciliation, [(10, 60m, 100m)]);
        payment.Allocations.Single().StudentDebt.PaidAmount = 50m;
        payment.Allocations.Single().StudentDebt.Status = StudentDebtStatus.PartiallyPaid;
        repository.FindForUpdateAsync(payment.PublicId, Arg.Any<CancellationToken>()).Returns(payment);
        repository.LockDebtsForPaymentAsync(payment.Id, Arg.Any<CancellationToken>()).Returns([payment.Allocations.Single().StudentDebt]);
        var handler = Handler<ReconcilePaymentCommandHandler>(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new(
            payment.PublicId, PaymentReconciliationDecision.Approve, null, 55), TestContext.Current.CancellationToken));

        Assert.Equal(PaymentStatus.PendingReconciliation, payment.Status);
        Assert.Empty(payment.Reconciliations);
    }

    [Fact]
    public async Task Reversal_is_append_only_and_restores_debt_balance()
    {
        var repository = Substitute.For<IPaymentRepository>();
        var payment = Payment(PaymentMethodKind.Cash, PaymentStatus.Confirmed, [(10, 70m, 100m)]);
        var debt = payment.Allocations.Single().StudentDebt;
        debt.PaidAmount = 70m;
        debt.Status = StudentDebtStatus.PartiallyPaid;
        repository.FindForUpdateAsync(payment.PublicId, Arg.Any<CancellationToken>()).Returns(payment);
        repository.LockDebtsForPaymentAsync(payment.Id, Arg.Any<CancellationToken>()).Returns([debt]);
        var handler = Handler<ReversePaymentCommandHandler>(repository);

        var result = await handler.Handle(new(payment.PublicId, "Error de imputación", 77), TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatus.Reversed, result.Status);
        Assert.Equal(0m, result.Allocations.Single().DebtPaid);
        Assert.Equal(StudentDebtStatus.Pending, result.Allocations.Single().DebtStatus);
        Assert.Equal(77, Assert.Single(result.Reversals).CreatedByUserId);
    }

    [Fact]
    public async Task Own_history_uses_authenticated_user_identity()
    {
        var repository = Substitute.For<IPaymentRepository>();
        repository.GetByUserAsync(7, Arg.Any<CancellationToken>()).Returns([Payment(PaymentMethodKind.Cash, PaymentStatus.Draft, [(10, 10m, 100m)])]);
        var handler = new GetPaymentsQueryHandler(repository);

        var result = await handler.Handle(7, null, TestContext.Current.CancellationToken);

        Assert.Single(result);
        await repository.Received(1).GetByUserAsync(7, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().GetByStudentAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    private static T Handler<T>(IPaymentRepository repository) where T : class
    {
        var policy = new PaymentPolicy();
        var unitOfWork = new ImmediateUnitOfWork();
        var time = new FixedTimeProvider(Now);
        var receiptRepository = Substitute.For<IReceiptRepository>();
        receiptRepository.LockSequenceAsync(Arg.Any<CancellationToken>()).Returns(new ReceiptSequence());
        var pdf = Substitute.For<IReceiptPdfGenerator>();
        pdf.GenerateAsync(Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns("%PDF-1.4 receipt"u8.ToArray());
        var storage = Substitute.For<IFileStorage>();
        storage.SaveAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("receipts/test.pdf");
        var receiptWorkflow = new ReceiptWorkflowService(receiptRepository, unitOfWork, pdf, storage, time);
        return typeof(T).Name switch
        {
            nameof(CreatePaymentCommandHandler) => (T)(object)new CreatePaymentCommandHandler(repository, policy, unitOfWork, time),
            nameof(ConfirmPaymentCommandHandler) => (T)(object)new ConfirmPaymentCommandHandler(repository, policy, unitOfWork, time, receiptWorkflow),
            nameof(ReconcilePaymentCommandHandler) => (T)(object)new ReconcilePaymentCommandHandler(repository, policy, unitOfWork, time, receiptWorkflow),
            nameof(ReversePaymentCommandHandler) => (T)(object)new ReversePaymentCommandHandler(repository, policy, unitOfWork, time),
            _ => throw new InvalidOperationException()
        };
    }

    private static PaymentMethod Method(PaymentMethodKind kind) => new()
    {
        Id = kind == PaymentMethodKind.BankTransfer ? 2 : 1,
        Code = kind == PaymentMethodKind.BankTransfer ? "BANK_TRANSFER" : "CASH",
        Name = kind.ToString(), Kind = kind, IsActive = true
    };

    private static Student Student() => new()
    {
        Id = 30,
        UserId = 7,
        User = new User { Id = 7, Username = "Ada", LastName = "Lovelace", Dni = "12345678" }
    };

    private static StudentDebt Debt(long id, decimal total) => new()
    {
        Id = id, PublicId = Guid.NewGuid(), StudentId = 30, TotalAmount = total,
        PaidAmount = 0m, Status = StudentDebtStatus.Pending, Currency = "ARS",
        FinancialConcept = new FinancialConcept { Id = (int)id, Code = $"CON-{id}", Name = $"Concepto {id}" }
    };

    private static Payment Payment(PaymentMethodKind kind, PaymentStatus status, IReadOnlyCollection<(long Id, decimal Amount, decimal Total)> allocations)
    {
        var student = Student();
        var payment = new Payment
        {
            Id = 20, PublicId = Guid.NewGuid(), StudentId = student.Id, Student = student,
            PaymentMethodId = Method(kind).Id, PaymentMethod = Method(kind), Currency = "ARS",
            Amount = allocations.Sum(item => item.Amount), Status = status, CreatedAt = Now.UtcDateTime, CreatedByUserId = 99
        };
        payment.Allocations = allocations.Select(item => new PaymentAllocation
        {
            PaymentId = payment.Id, StudentDebtId = item.Id, StudentDebt = Debt(item.Id, item.Total), Amount = item.Amount
        }).ToArray();
        return payment;
    }

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
