namespace AcademiaDigital.Domain.Entities;

public class CooperativeEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
}
