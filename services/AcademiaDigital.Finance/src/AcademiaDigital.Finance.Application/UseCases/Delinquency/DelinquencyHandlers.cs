using System.Text.Json;
using AcademiaDigital.Finance.Application.Interfaces;
using AcademiaDigital.Finance.Application.UseCases.Finance;
using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Finance.Application.UseCases.Delinquency;

public sealed record AcademicStudentSnapshotInput(
    long StudentId, long StudentCareerId, string StudentName, string Dni, string LegajoNumber,
    int CareerId, string CareerName, int? CommissionId, string? CommissionName,
    int AcademicYear, bool IsActive);

public sealed record SyncAcademicDirectoryCommand(IReadOnlyList<AcademicStudentSnapshotInput> Students);
public sealed record UpsertDelinquencyRuleCommand(
    long? Id, string Name, decimal MinimumOutstandingAmount, int MinimumDaysOverdue, bool IsActive, long ActorUserId);
public sealed record DelinquencyQuery(int? CareerId, int? CommissionId, int? AcademicYear, long? StudentId, DateOnly? AsOf);
public sealed record DelinquencyRuleDto(long Id, string Name, decimal MinimumOutstandingAmount, int MinimumDaysOverdue, bool IsActive);
public sealed record DelinquentStudentDto(
    long StudentId, long StudentCareerId, string StudentName, string Dni, string LegajoNumber,
    int CareerId, string CareerName, int? CommissionId, string? CommissionName, int AcademicYear,
    decimal OutstandingAmount, decimal OverdueAmount, int OverdueInstallments, int PartialInstallments,
    int MaximumDaysOverdue, IReadOnlyList<string> TriggeredAlerts);
public sealed record DelinquencyDashboardDto(
    DateOnly AsOf, int StudentCount, decimal OutstandingTotal, decimal OverdueTotal,
    IReadOnlyList<DelinquentStudentDto> Students);
public sealed record DelinquencyDebtDto(
    Guid PublicId, string Concept, DateOnly DueDate, decimal TotalAmount, decimal PaidAmount,
    decimal OutstandingAmount, StudentDebtStatus Status, int DaysOverdue);
public sealed record DelinquencyPaymentDto(Guid PublicId, DateTime CreatedAt, decimal Amount, PaymentStatus Status, string Method);
public sealed record DelinquencyStudentDetailDto(
    DelinquentStudentDto Summary, IReadOnlyList<DelinquencyDebtDto> Debts, IReadOnlyList<DelinquencyPaymentDto> Payments);

public sealed class SyncAcademicDirectoryCommandHandler(
    IDelinquencyRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<int> Handle(SyncAcademicDirectoryCommand command, CancellationToken ct = default)
    {
        if (command.Students.Count is 0 or > 5000)
            throw new ArgumentException("The directory sync must contain between 1 and 5000 students.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var input in command.Students)
        {
            if (input.StudentId <= 0 || input.StudentCareerId <= 0 || input.CareerId <= 0)
                throw new ArgumentException("Student, student career and career identifiers are required.");
            if (input.AcademicYear is < 2000 or > 2200)
                throw new ArgumentException("Academic year is invalid.");
            var snapshot = await repository.FindSnapshotForUpdateAsync(input.StudentCareerId, ct);
            if (snapshot is null)
            {
                snapshot = new AcademicStudentSnapshot { StudentCareerId = input.StudentCareerId };
                repository.AddSnapshot(snapshot);
            }
            snapshot.StudentId = input.StudentId;
            snapshot.StudentName = Required(input.StudentName, "Student name", 200);
            snapshot.Dni = Required(input.Dni, "DNI", 20);
            snapshot.LegajoNumber = Required(input.LegajoNumber, "Legajo", 50);
            snapshot.CareerId = input.CareerId;
            snapshot.CareerName = Required(input.CareerName, "Career name", 200);
            snapshot.CommissionId = input.CommissionId;
            snapshot.CommissionName = Optional(input.CommissionName, 100);
            snapshot.AcademicYear = input.AcademicYear;
            snapshot.IsActive = input.IsActive;
            snapshot.SyncedAtUtc = now;
        }
        await unitOfWork.SaveChangesAsync(ct);
        return command.Students.Count;
    }

    private static string Required(string value, string name, int max)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 || normalized.Length > max) throw new ArgumentException($"{name} is required and cannot exceed {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > max) throw new ArgumentException($"Value cannot exceed {max} characters.");
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}

public sealed class UpsertDelinquencyRuleCommandHandler(
    IDelinquencyRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<DelinquencyRuleDto> Handle(UpsertDelinquencyRuleCommand command, CancellationToken ct = default)
    {
        var name = command.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 150) throw new ArgumentException("Rule name is required and cannot exceed 150 characters.");
        if (command.MinimumOutstandingAmount < 0 || command.MinimumDaysOverdue < 0)
            throw new ArgumentException("Alert thresholds cannot be negative.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        DelinquencyAlertRule rule;
        if (command.Id.HasValue)
        {
            rule = await repository.FindRuleAsync(command.Id.Value, true, ct) ?? throw new KeyNotFoundException("Delinquency alert rule not found.");
            rule.UpdatedAtUtc = now;
            rule.UpdatedByUserId = command.ActorUserId;
        }
        else
        {
            rule = new DelinquencyAlertRule { CreatedAtUtc = now, CreatedByUserId = command.ActorUserId };
            repository.AddRule(rule);
        }
        rule.Name = name;
        rule.MinimumOutstandingAmount = decimal.Round(command.MinimumOutstandingAmount, 2);
        rule.MinimumDaysOverdue = command.MinimumDaysOverdue;
        rule.IsActive = command.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
        return Map(rule);
    }

    internal static DelinquencyRuleDto Map(DelinquencyAlertRule rule)
        => new(rule.Id, rule.Name, rule.MinimumOutstandingAmount, rule.MinimumDaysOverdue, rule.IsActive);
}

public sealed class GetDelinquencyRulesQueryHandler(IDelinquencyRepository repository)
{
    public async Task<IReadOnlyList<DelinquencyRuleDto>> Handle(CancellationToken ct = default)
        => (await repository.GetRulesAsync(ct)).Select(UpsertDelinquencyRuleCommandHandler.Map).ToArray();
}

public sealed class GetDelinquencyDashboardQueryHandler(IDelinquencyRepository repository, TimeProvider timeProvider)
{
    public async Task<DelinquencyDashboardDto> Handle(DelinquencyQuery request, CancellationToken ct = default)
    {
        var asOf = request.AsOf ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var filter = new DelinquencyFilter(request.CareerId, request.CommissionId, request.AcademicYear, request.StudentId);
        var debts = await repository.GetOpenDebtsAsync(filter, ct);
        var snapshots = (await repository.GetStudentSnapshotsAsync(filter, ct)).ToDictionary(item => item.StudentCareerId);
        var rules = (await repository.GetRulesAsync(ct)).Where(item => item.IsActive).ToArray();
        var students = debts
            .Where(debt => debt.DueDate < asOf && debt.TotalAmount > debt.PaidAmount)
            .GroupBy(debt => debt.StudentCareerId)
            .Select(group => MapGroup(group, snapshots.GetValueOrDefault(group.Key), rules, asOf))
            .Where(item => item is not null)
            .Cast<DelinquentStudentDto>()
            .OrderByDescending(item => item.MaximumDaysOverdue)
            .ThenByDescending(item => item.OverdueAmount)
            .ToArray();
        return new(asOf, students.Length, students.Sum(item => item.OutstandingAmount),
            students.Sum(item => item.OverdueAmount), students);
    }

    internal static DelinquentStudentDto? MapGroup(
        IEnumerable<StudentDebt> source, AcademicStudentSnapshot? snapshot,
        IReadOnlyCollection<DelinquencyAlertRule> rules, DateOnly asOf)
    {
        var debts = source.ToArray();
        if (debts.Length == 0 || snapshot is { IsActive: false }) return null;
        var first = debts[0];
        var calculation = JsonSerializer.Deserialize<DebtCalculationSnapshot>(first.CalculationSnapshotJson)
            ?? throw new InvalidOperationException("Debt calculation snapshot is invalid.");
        var overdue = debts.Where(item => item.DueDate < asOf).ToArray();
        var outstanding = debts.Sum(item => item.TotalAmount - item.PaidAmount);
        var overdueAmount = overdue.Sum(item => item.TotalAmount - item.PaidAmount);
        var maxDays = overdue.Length == 0 ? 0 : overdue.Max(item => asOf.DayNumber - item.DueDate.DayNumber);
        var alerts = rules.Where(rule => overdueAmount >= rule.MinimumOutstandingAmount && maxDays >= rule.MinimumDaysOverdue)
            .Select(rule => rule.Name).ToArray();
        var plan = first.BillingPlanItem.BillingPlan;
        return new(first.StudentId, first.StudentCareerId,
            snapshot?.StudentName ?? calculation.StudentName, snapshot?.Dni ?? calculation.Dni,
            snapshot?.LegajoNumber ?? calculation.LegajoNumber, plan.CareerId,
            snapshot?.CareerName ?? calculation.CareerName, snapshot?.CommissionId, snapshot?.CommissionName,
            plan.AcademicYear, decimal.Round(outstanding, 2), decimal.Round(overdueAmount, 2), overdue.Length,
            debts.Count(item => item.Status == StudentDebtStatus.PartiallyPaid), maxDays, alerts);
    }
}

public sealed class GetDelinquencyStudentDetailQueryHandler(
    IDelinquencyRepository repository, IPaymentRepository paymentRepository, TimeProvider timeProvider)
{
    public async Task<DelinquencyStudentDetailDto> Handle(long studentId, DateOnly? asOf, CancellationToken ct = default)
    {
        var date = asOf ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var filter = new DelinquencyFilter(null, null, null, studentId);
        var debts = await repository.GetOpenDebtsAsync(filter, ct);
        if (debts.Count == 0) throw new KeyNotFoundException("No open debts were found for the student.");
        var snapshots = (await repository.GetStudentSnapshotsAsync(filter, ct)).ToDictionary(item => item.StudentCareerId);
        var rules = (await repository.GetRulesAsync(ct)).Where(item => item.IsActive).ToArray();
        var summary = GetDelinquencyDashboardQueryHandler.MapGroup(debts, snapshots.GetValueOrDefault(debts[0].StudentCareerId), rules, date)!;
        var debtDtos = debts.Select(item => new DelinquencyDebtDto(item.PublicId, item.FinancialConcept.Name,
            item.DueDate, item.TotalAmount, item.PaidAmount, item.TotalAmount - item.PaidAmount, item.Status,
            Math.Max(0, date.DayNumber - item.DueDate.DayNumber))).ToArray();
        var payments = (await paymentRepository.GetByStudentAsync(studentId, ct)).Select(item =>
            new DelinquencyPaymentDto(item.PublicId, item.CreatedAt, item.Amount, item.Status, item.PaymentMethod.Name)).ToArray();
        return new(summary, debtDtos, payments);
    }
}
