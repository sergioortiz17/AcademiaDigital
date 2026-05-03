namespace AcademiaDigital.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
}
