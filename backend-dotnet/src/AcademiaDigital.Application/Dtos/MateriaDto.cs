namespace AcademiaDigital.Application.Dtos;

public class MateriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? CorrelativaId { get; set; }
    public string? CorrelativaNombre { get; set; }
}
