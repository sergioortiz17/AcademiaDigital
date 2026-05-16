using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Materias;

public class MateriaService
{
    private readonly IMateriaRepository _materiaRepository;

    public MateriaService(IMateriaRepository materiaRepository)
    {
        _materiaRepository = materiaRepository;
    }

    public async Task<IEnumerable<MateriaDto>> GetAllAsync()
    {
        var materias = await _materiaRepository.GetAllAsync();
        return materias.Select(m => new MateriaDto
        {
            Id = m.Id,
            Nombre = m.Nombre,
            CorrelativaId = m.CorrelativaId,
            CorrelativaNombre = m.Correlativa?.Nombre
        });
    }

    public async Task<MateriaDto?> GetByIdAsync(int id)
    {
        var materia = await _materiaRepository.GetByIdAsync(id);
        if (materia is null) return null;

        return new MateriaDto
        {
            Id = materia.Id,
            Nombre = materia.Nombre,
            CorrelativaId = materia.CorrelativaId,
            CorrelativaNombre = materia.Correlativa?.Nombre
        };
    }

    public async Task<MateriaDto> CreateAsync(CreateMateriaDto dto)
    {
        var materia = new Materia
        {
            Nombre = dto.Nombre,
            CorrelativaId = dto.CorrelativaId
        };

        await _materiaRepository.AddAsync(materia);
        await _materiaRepository.SaveChangesAsync();

        return new MateriaDto
        {
            Id = materia.Id,
            Nombre = materia.Nombre,
            CorrelativaId = materia.CorrelativaId,
            CorrelativaNombre = materia.Correlativa?.Nombre
        };
    }

    public async Task<bool> UpdateAsync(int id, CreateMateriaDto dto)
    {
        var materia = await _materiaRepository.GetByIdAsync(id);
        if (materia is null) return false;

        materia.Nombre = dto.Nombre;
        materia.CorrelativaId = dto.CorrelativaId;

        _materiaRepository.Update(materia);
        await _materiaRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var materia = await _materiaRepository.GetByIdAsync(id);
        if (materia is null) return false;

        _materiaRepository.Delete(materia);
        await _materiaRepository.SaveChangesAsync();
        return true;
    }
}
