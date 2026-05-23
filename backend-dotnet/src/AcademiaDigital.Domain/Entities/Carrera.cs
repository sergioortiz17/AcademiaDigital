namespace AcademiaDigital.Domain.Entities;

public class Carrera
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty; //creo que cada carrera tiene su código único
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionAnios { get; set; }
    public bool EstaActiva { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow; //Cuando se creó la carrera
}