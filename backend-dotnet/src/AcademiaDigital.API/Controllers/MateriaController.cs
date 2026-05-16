using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.Materias;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MateriaController : ControllerBase
{
    private readonly MateriaService _materiaService;

    public MateriaController(MateriaService materiaService)
    {
        _materiaService = materiaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _materiaService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _materiaService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMateriaDto dto)
    {
        var result = await _materiaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateMateriaDto dto)
    {
        var result = await _materiaService.UpdateAsync(id, dto);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _materiaService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
