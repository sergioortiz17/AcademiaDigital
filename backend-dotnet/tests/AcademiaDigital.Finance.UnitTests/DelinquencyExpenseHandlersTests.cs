using AcademiaDigital.Finance.Application.Interfaces;
using AcademiaDigital.Finance.Application.UseCases.Delinquency;
using AcademiaDigital.Finance.Application.UseCases.Expenses;
using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Finance.UnitTests;

public sealed class DelinquencyExpenseHandlersTests
{
    [Fact]
    public async Task SyncDirectory_UpsertsOnlyTheMinimalAcademicSnapshot()
    {
        var repository = Substitute.For<IDelinquencyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        AcademicStudentSnapshot? added = null;
        repository.FindSnapshotForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((AcademicStudentSnapshot?)null);
        repository.When(item => item.AddSnapshot(Arg.Any<AcademicStudentSnapshot>()))
            .Do(info => added = info.Arg<AcademicStudentSnapshot>());
        var handler = new SyncAcademicDirectoryCommandHandler(repository, unitOfWork, TimeProvider.System);

        var count = await handler.Handle(new([
            new(10, 99, "Ada Lovelace", "123", "A-10", 3, "Sistemas", 8, "1A", 2026, true)
        ]), TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
        Assert.NotNull(added);
        Assert.Equal("Ada Lovelace", added.StudentName);
        Assert.Equal(8, added.CommissionId);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertRule_RejectsNegativeThresholds()
    {
        var handler = new UpsertDelinquencyRuleCommandHandler(
            Substitute.For<IDelinquencyRepository>(), Substitute.For<IUnitOfWork>(), TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new(null, "Alerta", -1, 10, true, 5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RegisterExpense_RejectsAttachmentWhenMimeDoesNotMatchItsSignature()
    {
        var repository = Substitute.For<IExpenseRepository>();
        repository.FindCategoryAsync(4, false, Arg.Any<CancellationToken>()).Returns(new ExpenseCategory
        {
            Id = 4, Code = "SERV", Name = "Servicios", IsActive = true
        });
        var handler = new RegisterExpenseCommandHandler(repository, Substitute.For<IFileStorage>(),
            Substitute.For<IUnitOfWork>(), TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new(
            4, "Proveedor", new DateOnly(2026, 11, 14), 200, null, null,
            "not-a-pdf"u8.ToArray(), "application/pdf", "factura.pdf", 5), TestContext.Current.CancellationToken));
        repository.DidNotReceive().AddEntry(Arg.Any<ExpenseEntry>());
    }

    [Fact]
    public async Task ReverseExpense_PersistsOnlyForeignKeysAndReturnsTheCompensatingEntry()
    {
        var category = new ExpenseCategory { Id = 2, Code = "SERV", Name = "Servicios", IsActive = true };
        var original = new ExpenseEntry
        {
            Id = 17, PublicId = Guid.NewGuid(), Kind = ExpenseEntryKind.Expense, CategoryId = 2,
            Category = category, SupplierName = "Proveedor", ExpenseDate = new DateOnly(2026, 11, 1), Amount = 400
        };
        var repository = Substitute.For<IExpenseRepository>();
        repository.FindEntryAsync(original.PublicId, false, Arg.Any<CancellationToken>()).Returns(original);
        repository.HasReversalAsync(17, Arg.Any<CancellationToken>()).Returns(false);
        ExpenseEntry? persisted = null;
        repository.When(item => item.AddEntry(Arg.Any<ExpenseEntry>())).Do(info =>
        {
            persisted = info.Arg<ExpenseEntry>();
            Assert.Null(persisted.ReversalOfExpense);
            Assert.Null(persisted.Category);
        });
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ExpenseEntryDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ExpenseEntryDto>>>()(CancellationToken.None));
        var handler = new ReverseExpenseCommandHandler(repository, unitOfWork, TimeProvider.System);

        var result = await handler.Handle(new(original.PublicId, "Carga duplicada", 5), TestContext.Current.CancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(ExpenseEntryKind.Reversal, result.Kind);
        Assert.Equal(original.PublicId, result.ReversalOf);
        Assert.Equal(17, persisted.ReversalOfExpenseId);
        Assert.Equal(400, result.Amount);
    }
}
