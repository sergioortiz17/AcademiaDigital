using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.Carreras;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarreraController : ControllerBase
{
    private readonly CarreraService _carreraService;

    public CarreraController(CarreraService carreraService)
    {
        _carreraService = carreraService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _carreraService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _carreraService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CrearCarreraDto dto)
    {
        var result = await _carreraService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CrearCarreraDto dto)
    {
        var result = await _carreraService.UpdateAsync(id, dto);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _carreraService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}