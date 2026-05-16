namespace AcademiaDigital.Domain.Entities;

public class Materia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // Clave foránea para la materia correlativa (opcional)
    public int? CorrelativaId { get; set; }


}
