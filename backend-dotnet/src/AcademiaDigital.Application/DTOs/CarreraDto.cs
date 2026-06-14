namespace AcademiaDigital.Application.Dtos;

public class CarreraDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionAnios { get; set; }
    public bool EstaActiva { get; set; }
}