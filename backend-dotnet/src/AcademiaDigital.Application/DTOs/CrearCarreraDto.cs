namespace AcademiaDigital.Application.Dtos;

public class CrearCarreraDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionAnios { get; set; }
}