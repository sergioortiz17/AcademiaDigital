using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Carreras;

public class CarreraService
{
    private readonly ICarreraRepository _carreraRepository;

    public CarreraService(ICarreraRepository carreraRepository)
    {
        _carreraRepository = carreraRepository;
    }

    public async Task<IEnumerable<CarreraDto>> GetAllAsync()
    {
        var carreras = await _carreraRepository.GetAllAsync();
        return carreras.Select(c => new CarreraDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Codigo = c.Codigo,
            Descripcion = c.Descripcion,
            DuracionAnios = c.DuracionAnios,
            EstaActiva = c.EstaActiva
        });
    }

    public async Task<CarreraDto?> GetByIdAsync(int id)
    {
        var carrera = await _carreraRepository.GetByIdAsync(id);
        if (carrera is null) return null;

        return new CarreraDto
        {
            Id = carrera.Id,
            Nombre = carrera.Nombre,
            Codigo = carrera.Codigo,
            Descripcion = carrera.Descripcion,
            DuracionAnios = carrera.DuracionAnios,
            EstaActiva = carrera.EstaActiva
        };
    }

    public async Task<CarreraDto> CreateAsync(CrearCarreraDto dto)
    {
        var carrera = new Carrera
        {
            Nombre = dto.Nombre,
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            DuracionAnios = dto.DuracionAnios
            // Nota: Si 'EstaActiva' viene en el DTO, agrégalo aquí. 
            // Normalmente, al crear, se inicializa por defecto en true en la entidad.
        };

        await _carreraRepository.AddAsync(carrera);
        await _carreraRepository.SaveChangesAsync();

        return new CarreraDto
        {
            Id = carrera.Id,
            Nombre = carrera.Nombre,
            Codigo = carrera.Codigo,
            Descripcion = carrera.Descripcion,
            DuracionAnios = carrera.DuracionAnios,
            EstaActiva = carrera.EstaActiva
        };
    }

    public async Task<bool> UpdateAsync(int id, CrearCarreraDto dto)
    {
        var carrera = await _carreraRepository.GetByIdAsync(id);
        if (carrera is null) return false;

        carrera.Nombre = dto.Nombre;
        carrera.Codigo = dto.Codigo;
        carrera.Descripcion = dto.Descripcion;
        carrera.DuracionAnios = dto.DuracionAnios;

        _carreraRepository.Update(carrera);
        await _carreraRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var carrera = await _carreraRepository.GetByIdAsync(id);
        if (carrera is null) return false;

        _carreraRepository.Delete(carrera);
        await _carreraRepository.SaveChangesAsync();
        return true;
    }
}