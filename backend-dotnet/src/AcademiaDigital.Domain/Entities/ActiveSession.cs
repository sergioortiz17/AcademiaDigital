namespace AcademiaDigital.Domain.Entities;

public class ActiveSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired(int expirationDays = 7)
        => CreatedAt.AddDays(expirationDays) < DateTime.UtcNow;
}
