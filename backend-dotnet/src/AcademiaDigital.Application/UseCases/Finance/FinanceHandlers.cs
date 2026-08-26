using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Finance;

public sealed record FinancialConceptDto(int Id, string Code, string Name, string? Description, bool IsActive);
public sealed record FinancialRateDto(long Id, int ConceptId, int CareerId, int AcademicYear, StudentStatus? StudentCondition, decimal Amount, decimal SurchargePercentage, bool IsActive);
public sealed record FinancialBenefitDto(long Id, string Code, string Name, FinancialBenefitKind Kind, int? ScholarshipId, int? CareerId, StudentStatus? StudentCondition, decimal Percentage, DateOnly? ValidFrom, DateOnly? ValidTo, bool IsActive);
public sealed record BillingPlanItemDto(long Id, int ConceptId, int InstallmentNumber, DateOnly DueDate);
public sealed record BillingPlanDto(long Id, string Name, int CareerId, int AcademicYear, string Currency, bool IsActive, IReadOnlyList<BillingPlanItemDto> Items);
public sealed record StudentDebtDto(Guid PublicId, long StudentId, long StudentCareerId, string StudentName, string Dni, string LegajoNumber, string CareerName, string ConceptCode, string ConceptName, int InstallmentNumber, DateOnly DueDate, string Currency, decimal BaseAmount, decimal SurchargeAmount, decimal DiscountAmount, decimal TotalAmount, decimal PaidAmount, decimal OutstandingAmount, StudentDebtStatus Status, string? AppliedBenefitCode, string? AppliedBenefitName, DateTime CreatedAt);
public sealed record DebtGenerationResultDto(Guid BatchPublicId, string IdempotencyKey, long BillingPlanId, int GeneratedDebtCount, decimal GeneratedTotal, DateTime GeneratedAt, IReadOnlyList<StudentDebtDto> Debts);

public sealed record CreateFinancialConceptCommand(string Code, string Name, string? Description);
public sealed record UpdateFinancialConceptCommand(int Id, string Code, string Name, string? Description, bool IsActive);
public sealed record UpsertFinancialRateCommand(long? Id, int ConceptId, int CareerId, int AcademicYear, StudentStatus? StudentCondition, decimal Amount, decimal SurchargePercentage, bool IsActive);
public sealed record CreateFinancialBenefitCommand(string Code, string Name, FinancialBenefitKind Kind, int? ScholarshipId, int? CareerId, StudentStatus? StudentCondition, decimal Percentage, DateOnly? ValidFrom, DateOnly? ValidTo);
public sealed record CreateBillingPlanItemCommand(int ConceptId, int InstallmentNumber, DateOnly DueDate);
public sealed record CreateBillingPlanCommand(string Name, int CareerId, int AcademicYear, IReadOnlyList<CreateBillingPlanItemCommand> Items, long ActorUserId);
public sealed record GenerateStudentDebtsCommand(long BillingPlanId, string IdempotencyKey, long ActorUserId);

public sealed class GetFinancialConceptsQueryHandler(IFinanceRepository repository)
{
    public async Task<IReadOnlyList<FinancialConceptDto>> Handle(CancellationToken ct = default)
        => (await repository.GetConceptsAsync(ct)).Select(FinanceMappings.Map).ToArray();
}

public sealed class CreateFinancialConceptCommandHandler(IFinanceRepository repository, FinancePolicy policy, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<FinancialConceptDto> Handle(CreateFinancialConceptCommand command, CancellationToken ct = default)
    {
        var code = policy.NormalizeCode(command.Code);
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 150)
            throw new ArgumentException("Concept name is required and cannot exceed 150 characters.");
        if (await repository.ConceptCodeExistsAsync(code, null, ct))
            throw new InvalidOperationException("Financial concept code already exists.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var concept = new FinancialConcept { Code = code, Name = command.Name.Trim(), Description = Clean(command.Description), CreatedAt = now, UpdatedAt = now };
        repository.AddConcept(concept);
        await unitOfWork.SaveChangesAsync(ct);
        return FinanceMappings.Map(concept);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UpdateFinancialConceptCommandHandler(IFinanceRepository repository, FinancePolicy policy, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<FinancialConceptDto> Handle(UpdateFinancialConceptCommand command, CancellationToken ct = default)
    {
        var concept = await repository.FindConceptAsync(command.Id, true, ct) ?? throw new KeyNotFoundException("Financial concept not found.");
        var code = policy.NormalizeCode(command.Code);
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 150)
            throw new ArgumentException("Concept name is required and cannot exceed 150 characters.");
        if (await repository.ConceptCodeExistsAsync(code, command.Id, ct))
            throw new InvalidOperationException("Financial concept code already exists.");
        concept.Code = code;
        concept.Name = command.Name.Trim();
        concept.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        concept.IsActive = command.IsActive;
        concept.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync(ct);
        return FinanceMappings.Map(concept);
    }
}

public sealed class GetFinancialRatesQueryHandler(IFinanceRepository repository)
{
    public async Task<IReadOnlyList<FinancialRateDto>> Handle(int? careerId, int? academicYear, CancellationToken ct = default)
        => (await repository.GetRatesAsync(careerId, academicYear, ct)).Select(FinanceMappings.Map).ToArray();
}

public sealed class UpsertFinancialRateCommandHandler(IFinanceRepository repository, FinancePolicy policy, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<FinancialRateDto> Handle(UpsertFinancialRateCommand command, CancellationToken ct = default)
    {
        policy.ValidateRate(command.Amount, command.SurchargePercentage, command.AcademicYear);
        policy.ValidateStudentCondition(command.StudentCondition);
        if (await repository.FindConceptAsync(command.ConceptId, false, ct) is not { IsActive: true })
            throw new KeyNotFoundException("Active financial concept not found.");
        if (!await repository.CareerExistsAsync(command.CareerId, ct))
            throw new KeyNotFoundException("Career not found.");
        if (await repository.RateExistsAsync(command.ConceptId, command.CareerId, command.AcademicYear, command.StudentCondition, command.Id, ct))
            throw new InvalidOperationException("A rate already exists for the same concept, career, year and condition.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        FinancialRate rate;
        if (command.Id.HasValue)
        {
            rate = await repository.FindRateAsync(command.Id.Value, true, ct) ?? throw new KeyNotFoundException("Financial rate not found.");
        }
        else
        {
            rate = new FinancialRate { CreatedAt = now };
            repository.AddRate(rate);
        }
        rate.FinancialConceptId = command.ConceptId;
        rate.CareerId = command.CareerId;
        rate.AcademicYear = command.AcademicYear;
        rate.StudentCondition = command.StudentCondition;
        rate.Amount = decimal.Round(command.Amount, 2, MidpointRounding.AwayFromZero);
        rate.SurchargePercentage = decimal.Round(command.SurchargePercentage, 2, MidpointRounding.AwayFromZero);
        rate.IsActive = command.IsActive;
        rate.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(ct);
        return FinanceMappings.Map(rate);
    }
}

public sealed class GetFinancialBenefitsQueryHandler(IFinanceRepository repository)
{
    public async Task<IReadOnlyList<FinancialBenefitDto>> Handle(CancellationToken ct = default)
        => (await repository.GetBenefitsAsync(ct)).Select(FinanceMappings.Map).ToArray();
}

public sealed class CreateFinancialBenefitCommandHandler(IFinanceRepository repository, FinancePolicy policy, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<FinancialBenefitDto> Handle(CreateFinancialBenefitCommand command, CancellationToken ct = default)
    {
        var code = policy.NormalizeCode(command.Code);
        policy.ValidateBenefit(command.Kind, command.ScholarshipId, command.Percentage, command.ValidFrom, command.ValidTo);
        policy.ValidateStudentCondition(command.StudentCondition);
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 150)
            throw new ArgumentException("Benefit name is required and cannot exceed 150 characters.");
        if (await repository.BenefitCodeExistsAsync(code, ct)) throw new InvalidOperationException("Financial benefit code already exists.");
        if (command.CareerId.HasValue && !await repository.CareerExistsAsync(command.CareerId.Value, ct)) throw new KeyNotFoundException("Career not found.");
        if (command.ScholarshipId.HasValue && !await repository.ScholarshipExistsAsync(command.ScholarshipId.Value, ct)) throw new KeyNotFoundException("Scholarship not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var benefit = new FinancialBenefit
        {
            Code = code, Name = command.Name.Trim(), Kind = command.Kind, ScholarshipId = command.ScholarshipId,
            CareerId = command.CareerId, StudentCondition = command.StudentCondition,
            Percentage = decimal.Round(command.Percentage, 2, MidpointRounding.AwayFromZero),
            ValidFrom = command.ValidFrom, ValidTo = command.ValidTo, CreatedAt = now, UpdatedAt = now
        };
        repository.AddBenefit(benefit);
        await unitOfWork.SaveChangesAsync(ct);
        return FinanceMappings.Map(benefit);
    }
}

public sealed class GetBillingPlansQueryHandler(IFinanceRepository repository)
{
    public async Task<IReadOnlyList<BillingPlanDto>> Handle(int? careerId, int? academicYear, CancellationToken ct = default)
        => (await repository.GetPlansAsync(careerId, academicYear, ct)).Select(FinanceMappings.Map).ToArray();
}

public sealed class CreateBillingPlanCommandHandler(IFinanceRepository repository, FinancePolicy policy, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<BillingPlanDto> Handle(CreateBillingPlanCommand command, CancellationToken ct = default)
    {
        var items = command.Items.Select(item => (item.ConceptId, item.InstallmentNumber, item.DueDate)).ToArray();
        policy.ValidatePlan(command.Name, command.AcademicYear, items);
        if (!await repository.CareerExistsAsync(command.CareerId, ct)) throw new KeyNotFoundException("Career not found.");
        if (await repository.PlanNameExistsAsync(command.Name.Trim(), command.CareerId, command.AcademicYear, ct))
            throw new InvalidOperationException("A billing plan with the same name already exists for this career and year.");
        foreach (var conceptId in items.Select(item => item.ConceptId).Distinct())
            if (await repository.FindConceptAsync(conceptId, false, ct) is not { IsActive: true })
                throw new KeyNotFoundException($"Active financial concept {conceptId} not found.");
        var plan = new BillingPlan
        {
            Name = command.Name.Trim(), CareerId = command.CareerId, AcademicYear = command.AcademicYear,
            Currency = "ARS", CreatedAt = timeProvider.GetUtcNow().UtcDateTime, CreatedByUserId = command.ActorUserId,
            Items = command.Items.Select(item => new BillingPlanItem
            {
                FinancialConceptId = item.ConceptId, InstallmentNumber = item.InstallmentNumber, DueDate = item.DueDate
            }).ToList()
        };
        repository.AddPlan(plan);
        await unitOfWork.SaveChangesAsync(ct);
        return FinanceMappings.Map(plan);
    }
}

public sealed record DebtCalculationSnapshot(
    string StudentName,
    string Dni,
    string LegajoNumber,
    string CareerName,
    string ConceptCode,
    string ConceptName,
    int InstallmentNumber,
    DateOnly DueDate,
    DateOnly CalculationDate,
    long RateId,
    decimal RateAmount,
    decimal SurchargePercentage,
    long? BenefitId,
    string? BenefitCode,
    string? BenefitName,
    FinancialBenefitKind? BenefitKind,
    decimal? BenefitPercentage,
    decimal SurchargeAmount,
    decimal DiscountAmount,
    decimal TotalAmount);

public sealed class GenerateStudentDebtsCommandHandler(IFinanceRepository repository, FinancePolicy policy, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public Task<DebtGenerationResultDto> Handle(GenerateStudentDebtsCommand command, CancellationToken ct = default)
    {
        var key = (command.IdempotencyKey ?? string.Empty).Trim();
        if (key.Length is < 8 or > 100) throw new ArgumentException("Idempotency-Key must contain 8 to 100 characters.");
        return unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var existing = await repository.FindBatchForUpdateAsync(key, transactionCt);
            if (existing is not null)
            {
                if (existing.BillingPlanId != command.BillingPlanId) throw new InvalidOperationException("Idempotency-Key was already used for another billing plan.");
                return FinanceMappings.Map(existing, await repository.GetDebtsByBatchAsync(existing.Id, transactionCt));
            }

            var plan = await repository.FindPlanForGenerationAsync(command.BillingPlanId, transactionCt)
                ?? throw new KeyNotFoundException("Active billing plan not found.");
            if (await repository.HasDebtsForPlanAsync(plan.Id, transactionCt))
                throw new InvalidOperationException("Debts were already generated for this billing plan with another Idempotency-Key.");
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var calculationDate = DateOnly.FromDateTime(now);
            var targets = await repository.GetGenerationTargetsAsync(plan.CareerId, plan.AcademicYear, calculationDate, transactionCt);
            if (targets.Count == 0) throw new InvalidOperationException("The billing plan has no active student careers to generate.");
            var conceptIds = plan.Items.Select(item => item.FinancialConceptId).Distinct().ToArray();
            var rates = await repository.GetGenerationRatesAsync(plan.CareerId, plan.AcademicYear, conceptIds, transactionCt);
            var benefits = await repository.GetGenerationBenefitsAsync(plan.CareerId, calculationDate, transactionCt);
            var batch = new DebtGenerationBatch
            {
                PublicId = Guid.NewGuid(), IdempotencyKey = key, BillingPlanId = plan.Id,
                GeneratedAt = now, GeneratedByUserId = command.ActorUserId
            };

            foreach (var target in targets)
            foreach (var item in plan.Items.OrderBy(item => item.DueDate).ThenBy(item => item.InstallmentNumber))
            {
                var rate = rates.FirstOrDefault(candidate => candidate.FinancialConceptId == item.FinancialConceptId && candidate.StudentCondition == target.Condition)
                    ?? rates.FirstOrDefault(candidate => candidate.FinancialConceptId == item.FinancialConceptId && candidate.StudentCondition is null)
                    ?? throw new InvalidOperationException($"No active rate exists for concept {item.FinancialConceptId}, career {plan.CareerId}, year {plan.AcademicYear} and condition {target.Condition}.");
                var applicableBenefits = benefits
                    .Where(benefit => (!benefit.StudentCondition.HasValue || benefit.StudentCondition == target.Condition)
                        && (!benefit.ScholarshipId.HasValue || target.GrantedScholarshipIds.Contains(benefit.ScholarshipId.Value)))
                    .Select(benefit => new FinancialBenefitCandidate(benefit.Id, benefit.Code, benefit.Name, benefit.Kind, benefit.Percentage))
                    .ToArray();
                var calculation = policy.Calculate(rate.Amount, item.DueDate, calculationDate, rate.SurchargePercentage, applicableBenefits);
                var snapshot = new DebtCalculationSnapshot(
                    target.StudentName, target.Dni, target.LegajoNumber, target.CareerName,
                    item.FinancialConcept.Code, item.FinancialConcept.Name, item.InstallmentNumber,
                    item.DueDate, calculationDate, rate.Id, rate.Amount, rate.SurchargePercentage,
                    calculation.AppliedBenefit?.Id, calculation.AppliedBenefit?.Code, calculation.AppliedBenefit?.Name,
                    calculation.AppliedBenefit?.Kind, calculation.AppliedBenefit?.Percentage,
                    calculation.SurchargeAmount, calculation.DiscountAmount, calculation.TotalAmount);
                batch.Debts.Add(new StudentDebt
                {
                    PublicId = Guid.NewGuid(), BillingPlanItemId = item.Id, StudentId = target.StudentId,
                    StudentCareerId = target.StudentCareerId, FinancialConceptId = item.FinancialConceptId,
                    Currency = "ARS", DueDate = item.DueDate, BaseAmount = calculation.BaseAmount,
                    SurchargeAmount = calculation.SurchargeAmount, DiscountAmount = calculation.DiscountAmount,
                    TotalAmount = calculation.TotalAmount, PaidAmount = 0m, Status = StudentDebtStatus.Pending,
                    FinancialRateId = rate.Id, AppliedBenefitId = calculation.AppliedBenefit?.Id,
                    CalculationSnapshotJson = JsonSerializer.Serialize(snapshot), CreatedAt = now
                });
            }
            batch.GeneratedDebtCount = batch.Debts.Count;
            batch.GeneratedTotal = decimal.Round(batch.Debts.Sum(debt => debt.TotalAmount), 2, MidpointRounding.AwayFromZero);
            repository.AddBatch(batch);
            await unitOfWork.SaveChangesAsync(transactionCt);
            return FinanceMappings.Map(batch, batch.Debts.ToArray());
        }, ct);
    }
}

public sealed class GetStudentDebtsQueryHandler(IFinanceRepository repository)
{
    public async Task<IReadOnlyList<StudentDebtDto>> Handle(long? userId, long? studentId, CancellationToken ct = default)
    {
        var debts = studentId.HasValue
            ? await repository.GetDebtsByStudentAsync(studentId.Value, ct)
            : await repository.GetDebtsByUserAsync(userId ?? throw new ArgumentException("User is required."), ct);
        return debts.Select(FinanceMappings.Map).ToArray();
    }
}

internal static class FinanceMappings
{
    public static FinancialConceptDto Map(FinancialConcept item) => new(item.Id, item.Code, item.Name, item.Description, item.IsActive);
    public static FinancialRateDto Map(FinancialRate item) => new(item.Id, item.FinancialConceptId, item.CareerId, item.AcademicYear, item.StudentCondition, item.Amount, item.SurchargePercentage, item.IsActive);
    public static FinancialBenefitDto Map(FinancialBenefit item) => new(item.Id, item.Code, item.Name, item.Kind, item.ScholarshipId, item.CareerId, item.StudentCondition, item.Percentage, item.ValidFrom, item.ValidTo, item.IsActive);
    public static BillingPlanDto Map(BillingPlan item) => new(item.Id, item.Name, item.CareerId, item.AcademicYear, item.Currency, item.IsActive,
        item.Items.OrderBy(planItem => planItem.DueDate).ThenBy(planItem => planItem.InstallmentNumber)
            .Select(planItem => new BillingPlanItemDto(planItem.Id, planItem.FinancialConceptId, planItem.InstallmentNumber, planItem.DueDate)).ToArray());

    public static StudentDebtDto Map(StudentDebt debt)
    {
        var snapshot = JsonSerializer.Deserialize<DebtCalculationSnapshot>(debt.CalculationSnapshotJson)
            ?? throw new InvalidOperationException("Debt calculation snapshot is invalid.");
        return new(debt.PublicId, debt.StudentId, debt.StudentCareerId, snapshot.StudentName, snapshot.Dni,
            snapshot.LegajoNumber, snapshot.CareerName, snapshot.ConceptCode, snapshot.ConceptName,
            snapshot.InstallmentNumber, debt.DueDate, debt.Currency, debt.BaseAmount, debt.SurchargeAmount,
            debt.DiscountAmount, debt.TotalAmount, debt.PaidAmount, debt.TotalAmount - debt.PaidAmount,
            debt.Status, snapshot.BenefitCode, snapshot.BenefitName, debt.CreatedAt);
    }

    public static DebtGenerationResultDto Map(DebtGenerationBatch batch, IReadOnlyCollection<StudentDebt> debts)
        => new(batch.PublicId, batch.IdempotencyKey, batch.BillingPlanId, batch.GeneratedDebtCount,
            batch.GeneratedTotal, batch.GeneratedAt, debts.Select(Map).ToArray());
}
