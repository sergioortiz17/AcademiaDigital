using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Careers;

public class CareerService(ICareerRepository careerRepository)
{
    public async Task<IEnumerable<CareerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var careers = await careerRepository.GetAllAsync(ct);
        return careers.Select(Map);
    }

    public async Task<CareerDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(id, ct);
        return career is null ? null : Map(career);
    }

    public async Task<CareerDto> CreateAsync(CreateCareerDto dto, CancellationToken ct = default)
    {
        var career = new Career
        {
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            Description = dto.Description?.Trim(),
            TotalCredits = dto.TotalCredits,
            DurationYears = dto.DurationYears
        };

        var created = await careerRepository.CreateAsync(career, ct);
        return Map(created);
    }

    public async Task<bool> UpdateAsync(int id, CreateCareerDto dto, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(id, ct);
        if (career is null) return false;

        career.Name = dto.Name.Trim();
        career.Code = dto.Code.Trim();
        career.Description = dto.Description?.Trim();
        career.TotalCredits = dto.TotalCredits;
        career.DurationYears = dto.DurationYears;

        await careerRepository.UpdateAsync(career, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(id, ct);
        if (career is null) return false;

        await careerRepository.DeleteAsync(career, ct);
        return true;
    }

    private static CareerDto Map(Career career) => new()
    {
        Id = career.Id,
        Name = career.Name,
        Code = career.Code,
        Description = career.Description,
        TotalCredits = career.TotalCredits,
        DurationYears = career.DurationYears,
        IsActive = career.IsActive,
        CreatedAt = career.CreatedAt
    };
}
