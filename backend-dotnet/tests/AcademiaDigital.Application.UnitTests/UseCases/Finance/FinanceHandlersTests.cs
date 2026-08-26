using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Finance;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Finance;

public sealed class FinanceHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Generation_applies_surcharge_then_best_eligible_benefit_and_snapshots_result()
    {
        var repository = Substitute.For<IFinanceRepository>();
        repository.FindPlanForGenerationAsync(10, Arg.Any<CancellationToken>()).Returns(Plan());
        repository.GetGenerationTargetsAsync(2, 2026, new DateOnly(2026, 8, 24), Arg.Any<CancellationToken>())
            .Returns([Target([3])]);
        repository.GetGenerationRatesAsync(2, 2026, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([Rate()]);
        repository.GetGenerationBenefitsAsync(2, new DateOnly(2026, 8, 24), Arg.Any<CancellationToken>())
            .Returns([
                Benefit(1, "GENERAL10", FinancialBenefitKind.Discount, 10m),
                Benefit(2, "BECA30", FinancialBenefitKind.Scholarship, 30m, 3)
            ]);
        var handler = new GenerateStudentDebtsCommandHandler(
            repository, new FinancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new(10, "finance-m9-key-001", 99), TestContext.Current.CancellationToken);

        var debt = Assert.Single(result.Debts);
        Assert.Equal(100m, debt.SurchargeAmount);
        Assert.Equal(330m, debt.DiscountAmount);
        Assert.Equal(770m, debt.TotalAmount);
        Assert.Equal("BECA30", debt.AppliedBenefitCode);
        Assert.Equal(770m, result.GeneratedTotal);
        repository.Received(1).AddBatch(Arg.Is<DebtGenerationBatch>(batch =>
            batch.GeneratedDebtCount == 1 && batch.Debts.Single().CalculationSnapshotJson.Contains("BECA30")));
    }

    [Fact]
    public async Task Repeated_generation_key_returns_existing_batch_without_new_debts()
    {
        var repository = Substitute.For<IFinanceRepository>();
        var existing = new DebtGenerationBatch
        {
            Id = 44, PublicId = Guid.NewGuid(), IdempotencyKey = "finance-m9-key-002", BillingPlanId = 10,
            GeneratedAt = Now.UtcDateTime, GeneratedDebtCount = 1, GeneratedTotal = 770m
        };
        repository.FindBatchForUpdateAsync(existing.IdempotencyKey, Arg.Any<CancellationToken>()).Returns(existing);
        repository.GetDebtsByBatchAsync(44, Arg.Any<CancellationToken>()).Returns([Debt()]);
        var handler = new GenerateStudentDebtsCommandHandler(
            repository, new FinancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new(10, existing.IdempotencyKey, 99), TestContext.Current.CancellationToken);

        Assert.Equal(existing.PublicId, result.BatchPublicId);
        repository.DidNotReceive().AddBatch(Arg.Any<DebtGenerationBatch>());
        await repository.DidNotReceive().FindPlanForGenerationAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reusing_generation_key_for_another_plan_is_rejected()
    {
        var repository = Substitute.For<IFinanceRepository>();
        repository.FindBatchForUpdateAsync("finance-m9-key-003", Arg.Any<CancellationToken>())
            .Returns(new DebtGenerationBatch { BillingPlanId = 9, IdempotencyKey = "finance-m9-key-003" });
        var handler = new GenerateStudentDebtsCommandHandler(
            repository, new FinancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new(10, "finance-m9-key-003", 99), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Missing_rate_aborts_the_whole_generation()
    {
        var repository = Substitute.For<IFinanceRepository>();
        repository.FindPlanForGenerationAsync(10, Arg.Any<CancellationToken>()).Returns(Plan());
        repository.GetGenerationTargetsAsync(2, 2026, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([Target([])]);
        repository.GetGenerationRatesAsync(2, 2026, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns([]);
        repository.GetGenerationBenefitsAsync(2, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GenerateStudentDebtsCommandHandler(
            repository, new FinancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new(10, "finance-m9-key-004", 99), TestContext.Current.CancellationToken));
        repository.DidNotReceive().AddBatch(Arg.Any<DebtGenerationBatch>());
    }

    [Fact]
    public async Task Different_key_cannot_generate_the_same_plan_twice()
    {
        var repository = Substitute.For<IFinanceRepository>();
        repository.FindPlanForGenerationAsync(10, Arg.Any<CancellationToken>()).Returns(Plan());
        repository.HasDebtsForPlanAsync(10, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new GenerateStudentDebtsCommandHandler(
            repository, new FinancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new(10, "finance-m9-key-005", 99), TestContext.Current.CancellationToken));

        repository.DidNotReceive().AddBatch(Arg.Any<DebtGenerationBatch>());
    }

    [Fact]
    public async Task Student_debt_query_uses_authenticated_user_identity()
    {
        var repository = Substitute.For<IFinanceRepository>();
        repository.GetDebtsByUserAsync(7, Arg.Any<CancellationToken>()).Returns([Debt()]);
        var handler = new GetStudentDebtsQueryHandler(repository);

        var result = await handler.Handle(7, null, TestContext.Current.CancellationToken);

        Assert.Single(result);
        await repository.Received(1).GetDebtsByUserAsync(7, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().GetDebtsByStudentAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rate_update_does_not_recalculate_existing_debt_snapshot()
    {
        var repository = Substitute.For<IFinanceRepository>();
        var rate = Rate();
        repository.FindConceptAsync(5, false, Arg.Any<CancellationToken>()).Returns(new FinancialConcept { Id = 5, IsActive = true });
        repository.CareerExistsAsync(2, Arg.Any<CancellationToken>()).Returns(true);
        repository.FindRateAsync(8, true, Arg.Any<CancellationToken>()).Returns(rate);
        var debt = Debt();
        var originalSnapshot = debt.CalculationSnapshotJson;
        var handler = new UpsertFinancialRateCommandHandler(
            repository, new FinancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await handler.Handle(new(8, 5, 2, 2026, null, 2000m, 20m, true), TestContext.Current.CancellationToken);

        Assert.Equal(2000m, rate.Amount);
        Assert.Equal(originalSnapshot, debt.CalculationSnapshotJson);
        Assert.Equal(1000m, debt.BaseAmount);
    }

    private static BillingPlan Plan()
    {
        var concept = new FinancialConcept { Id = 5, Code = "CUOTA", Name = "Cuota mensual", IsActive = true };
        return new BillingPlan
        {
            Id = 10, CareerId = 2, AcademicYear = 2026, Currency = "ARS", IsActive = true,
            Items = [new BillingPlanItem { Id = 20, FinancialConceptId = 5, FinancialConcept = concept, InstallmentNumber = 1, DueDate = new DateOnly(2026, 8, 20) }]
        };
    }

    private static FinanceStudentTarget Target(IReadOnlyCollection<int> scholarships)
        => new(30, 7, 40, StudentStatus.Regular, "Ada Lovelace", "12345678", "LEG-30", "Sistemas", scholarships);

    private static FinancialRate Rate() => new()
    {
        Id = 8, FinancialConceptId = 5, CareerId = 2, AcademicYear = 2026,
        Amount = 1000m, SurchargePercentage = 10m, IsActive = true
    };

    private static FinancialBenefit Benefit(long id, string code, FinancialBenefitKind kind, decimal percentage, int? scholarshipId = null)
        => new() { Id = id, Code = code, Name = code, Kind = kind, Percentage = percentage, ScholarshipId = scholarshipId, IsActive = true };

    private static StudentDebt Debt()
    {
        var snapshot = new DebtCalculationSnapshot(
            "Ada Lovelace", "12345678", "LEG-30", "Sistemas", "CUOTA", "Cuota mensual", 1,
            new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 24), 8, 1000m, 10m,
            2, "BECA30", "Beca 30", FinancialBenefitKind.Scholarship, 30m, 100m, 330m, 770m);
        return new StudentDebt
        {
            PublicId = Guid.NewGuid(), StudentId = 30, StudentCareerId = 40, DueDate = new DateOnly(2026, 8, 20),
            Currency = "ARS", BaseAmount = 1000m, SurchargeAmount = 100m, DiscountAmount = 330m,
            TotalAmount = 770m, CalculationSnapshotJson = System.Text.Json.JsonSerializer.Serialize(snapshot), CreatedAt = Now.UtcDateTime
        };
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
